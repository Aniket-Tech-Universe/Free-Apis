using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("API")]
    public class PipelineController : ControllerBase
    {
        private readonly ILogger<PipelineController> _logger;
        private readonly IConfiguration _configuration;

        // Path to your executables (Adjust based on your deployment)
        private const string ScraperPath = @"..\UnsecuredAPIKeys.Bots.Scraper\bin\Debug\net9.0\UnsecuredAPIKeys.Bots.Scraper.exe";
        private const string VerifierPath = @"..\UnsecuredAPIKeys.Bots.Verifier\bin\Debug\net9.0\UnsecuredAPIKeys.Bots.Verifier.exe";

        public PipelineController(ILogger<PipelineController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("RunPipeline")]
        public async Task<IActionResult> RunPipeline()
        {
            _logger.LogInformation("Pipeline Triggered manually via API.");

            try
            {
                // 1. Start Scraper
                var scraperStartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(ScraperPath),
                    UseShellExecute = true, // Open new window
                    CreateNoWindow = false
                };

                _logger.LogInformation("Starting Scraper from: {Path}", scraperStartInfo.FileName);
                Process.Start(scraperStartInfo);

                // 2. Start Verifier (Delayed slightly or parallel)
                // In a real pipeline, we might wait, but for "Run Now" buttons, parallel is often fine 
                // as the Verifier will just pick up whatever is in the DB.
                // However, let's wait 5 seconds to let Scraper initialize.
                await Task.Delay(5000);

                var verifierStartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(VerifierPath),
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                _logger.LogInformation("Starting Verifier from: {Path}", verifierStartInfo.FileName);
                Process.Start(verifierStartInfo);

                return Ok(new { message = "Pipeline Started! (Scraper & Verifier launched)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start pipeline.");
                return StatusCode(500, new { error = "Failed to start pipeline: " + ex.Message });
            }
        }

        [HttpGet("GetPipelineStatus")]
        public IActionResult GetPipelineStatus()
        {
            // Simple check: are processes running?
            var scraperRunning = Process.GetProcessesByName("UnsecuredAPIKeys.Bots.Scraper").Any();
            var verifierRunning = Process.GetProcessesByName("UnsecuredAPIKeys.Bots.Verifier").Any();

            var status = "Idle";
            if (scraperRunning && verifierRunning) status = "Running (Both)";
            else if (scraperRunning) status = "Running (Scraper)";
            else if (verifierRunning) status = "Running (Verifier)";

            // Mocking the GitHub Action response format for the frontend compatibility
            return Ok(new 
            { 
                workflow_runs = new[] 
                { 
                    new 
                    { 
                        status = status == "Idle" ? "completed" : "in_progress", 
                        conclusion = status == "Idle" ? "success" : null,
                        updated_at = DateTime.UtcNow.ToString("o")
                    } 
                } 
            });
        }
    }
}
