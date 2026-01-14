using System.Diagnostics;
using UnsecuredAPIKeys.WebAPI.Controllers;

namespace UnsecuredAPIKeys.WebAPI
{
    public interface ISystemdService
    {
        Task<StatusController.SystemServicesStatus> GetServicesStatusAsync();
    }

    public class SystemdService(ILogger<SystemdService> logger) : ISystemdService
    {
        public async Task<StatusController.SystemServicesStatus> GetServicesStatusAsync()
        {
            var status = new StatusController.SystemServicesStatus();

            try
            {
                status.ScraperService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("SCRAPER_SERVICE_NAME") ?? "api-scraper");
                status.VerifierService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("VERIFIER_SERVICE_NAME") ?? "api-verifier");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service status");
                throw;
            }

            return status;
        }

        private async Task<StatusController.ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            var status = new StatusController.ServiceStatus
            {
                ServiceName = serviceName
            };

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"systemctl status {serviceName}.service --no-pager",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Parse the output
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Active:"))
                    {
                        status.Status = line.Trim();
                        status.IsActive = line.Contains("active (running)");
                    }
                    else if (line.Contains("Description:"))
                    {
                        status.Description = line.Replace("Description:", "").Trim();
                    }
                }

                // Get the last run time from journal
                startInfo.Arguments = $"journalctl -u {serviceName}.service --no-pager -n 1 --output=short-iso";
                using var journalProcess = new Process { StartInfo = startInfo };
                journalProcess.Start();
                var journalOutput = await journalProcess.StandardOutput.ReadToEndAsync();
                await journalProcess.WaitForExitAsync();

                if (!string.IsNullOrEmpty(journalOutput))
                {
                    status.LastRunTime = journalOutput.Split(' ')[0];
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting status for service {ServiceName}", serviceName);
                status.Status = $"Error: {ex.Message}";
                status.IsActive = false;
            }

            return status;
        }
    }
}
