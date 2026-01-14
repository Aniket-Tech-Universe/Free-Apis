using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Web;
using UnsecuredAPIKeys.Data;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("api/paypal")]
    public class PayPalIPNController : ControllerBase
    {
        private readonly DBContext dbContext;
        private readonly ILogger<PayPalIPNController> logger;
        private readonly IConfiguration configuration;
        private const string PAYPAL_VERIFICATION_URL = "https://ipnpb.paypal.com/cgi-bin/webscr";
        private const string PAYPAL_SANDBOX_VERIFICATION_URL = "https://ipnpb.sandbox.paypal.com/cgi-bin/webscr";

        public PayPalIPNController(DBContext dbContext, ILogger<PayPalIPNController> logger, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost("ipn")]
        public async Task<IActionResult> ProcessIPN()
        {
            try
            {
                // Read the raw IPN message from PayPal
                using var reader = new StreamReader(Request.Body, Encoding.ASCII);
                var ipnMessage = await reader.ReadToEndAsync();

                logger.LogInformation("Received PayPal IPN message");

                // Verify the IPN message with PayPal
                var isVerified = await VerifyIPN(ipnMessage);
                
                if (!isVerified)
                {
                    logger.LogWarning("PayPal IPN verification failed for message: {Message}", ipnMessage);
                    return BadRequest("IPN verification failed");
                }

                // Parse the IPN message
                var ipnData = HttpUtility.ParseQueryString(ipnMessage);
                
                // Extract key fields
                var txnId = ipnData["txn_id"];
                var paymentStatus = ipnData["payment_status"];
                var payerEmail = ipnData["payer_email"];
                var firstName = ipnData["first_name"];
                var lastName = ipnData["last_name"];
                var payerId = ipnData["payer_id"];
                var mcGross = ipnData["mc_gross"];
                var mcCurrency = ipnData["mc_currency"];
                var itemName = ipnData["item_name"];
                var itemNumber = ipnData["item_number"];
                var custom = ipnData["custom"]; // This could contain tracking ID
                
                logger.LogInformation("Processing PayPal IPN for transaction {TransactionId} from {PayerEmail}", 
                    txnId, payerEmail);

                // Handle different transaction types
                var txnType = ipnData["txn_type"];
                
                // Check for refunds/reversals first (they might not have txn_type)
                if (paymentStatus == "Refunded" || paymentStatus == "Reversed")
                {
                    // Handle refunds/reversals as new records for audit trail
                    var parentTxnId = ipnData["parent_txn_id"]; // Original transaction that was refunded
                    var reasonCode = ipnData["reason_code"];
                    
                    // Create a new record for the refund/reversal
                    var refundRecord = new Data.Models.DonationTracking
                    {
                        ClickedAt = DateTime.UtcNow,
                        ClickLocation = "refund_ipn",
                        UserIP = "IPN",
                        UserAgent = "PayPal IPN Refund",
                        ConfirmedDonation = true,
                        DonationAmount = decimal.TryParse(mcGross, out var refundAmount) ? -Math.Abs(refundAmount) : 0, // Negative amount
                        PayPalTransactionId = txnId,
                        DonationConfirmedAt = DateTime.UtcNow,
                        DonorEmail = payerEmail,
                        DonorFirstName = firstName,
                        DonorLastName = lastName,
                        PayPalPayerId = payerId,
                        PaymentStatus = paymentStatus,
                        IPNVerified = true,
                        IPNRawMessage = ipnMessage,
                        Notes = $"REFUND/REVERSAL: Original transaction {parentTxnId}. Reason: {reasonCode ?? "Not specified"}. Status: {paymentStatus}"
                    };
                    
                    dbContext.DonationTrackings.Add(refundRecord);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation("Processed refund/reversal of {Amount} {Currency} for transaction {ParentTxn}. Reason: {Reason}", 
                        mcGross, mcCurrency, parentTxnId, reasonCode);
                }
                else if ((txnType == "web_accept" || txnType == "donate") && paymentStatus == "Completed")
                {
                    // This is a completed donation
                        // Find existing donation tracking record by transaction ID
                        var donation = await dbContext.DonationTrackings
                            .FirstOrDefaultAsync(d => d.PayPalTransactionId == txnId);
                        
                        if (donation == null)
                        {
                            // Look for recent unconfirmed donations that might match
                            // This is a fallback if the client-side tracking didn't capture the transaction ID
                            donation = await dbContext.DonationTrackings
                                .Where(d => !d.ConfirmedDonation && 
                                           d.ClickedAt >= DateTime.UtcNow.AddHours(-2))
                                .OrderByDescending(d => d.ClickedAt)
                                .FirstOrDefaultAsync();
                            
                            if (donation == null)
                            {
                                // Create a new tracking record if none exists
                                donation = new Data.Models.DonationTracking
                                {
                                    ClickedAt = DateTime.UtcNow,
                                    ClickLocation = "ipn_direct",
                                    UserIP = "IPN",
                                    UserAgent = "PayPal IPN"
                                };
                                dbContext.DonationTrackings.Add(donation);
                            }
                        }
                        
                        // Update donation with IPN data
                        donation.ConfirmedDonation = true;
                        donation.DonationAmount = decimal.TryParse(mcGross, out var amount) ? amount : 0;
                        donation.PayPalTransactionId = txnId;
                        donation.DonationConfirmedAt = DateTime.UtcNow;
                        donation.DonorEmail = payerEmail;
                        donation.DonorFirstName = firstName;
                        donation.DonorLastName = lastName;
                        donation.PayPalPayerId = payerId;
                        donation.PaymentStatus = paymentStatus;
                        donation.IPNVerified = true;
                        donation.IPNRawMessage = ipnMessage;
                        donation.Notes = $"IPN Verified. Currency: {mcCurrency}. Item: {itemName}";
                        
                        await dbContext.SaveChangesAsync();
                        
                        logger.LogInformation("Successfully processed donation of {Amount} {Currency} from {Email}", 
                            mcGross, mcCurrency, payerEmail);
                }
                else
                {
                    logger.LogWarning("Received IPN for unhandled transaction type: {Type} with status: {Status}", 
                        txnType ?? "null", paymentStatus);
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing PayPal IPN");
                return StatusCode(500);
            }
        }

        private async Task<bool> VerifyIPN(string ipnMessage)
        {
            try
            {
                // Prepend PayPal verification command
                var verificationRequest = "cmd=_notify-validate&" + ipnMessage;
                
                // Determine which PayPal URL to use (sandbox vs production)
                var isProduction = configuration.GetValue<bool>("PayPal:IsProduction", true);
                var verificationUrl = isProduction ? PAYPAL_VERIFICATION_URL : PAYPAL_SANDBOX_VERIFICATION_URL;
                
                using var client = new HttpClient();
                var content = new StringContent(verificationRequest, Encoding.ASCII, "application/x-www-form-urlencoded");
                
                var response = await client.PostAsync(verificationUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    return responseText.Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying IPN with PayPal");
                return false;
            }
        }

    }
}
