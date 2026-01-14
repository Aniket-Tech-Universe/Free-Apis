using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using System.Diagnostics;
using UnsecuredAPIKeys.Data;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController(
        ILogger<StatusController> logger,
        DBContext dbContext)
        : ControllerBase
    {
        [HttpGet("GetStatus")]
        public async Task<IActionResult> GetStatus([FromQuery] bool includeLogs = false, [FromQuery] int logLines = 50)
        {
            try
            {
                var dbStatus = await dbContext.APIKeys.AnyAsync();
                var servicesStatus = await GetServicesStatusAsync(includeLogs, logLines);

                var status = new
                {
                    ApiStatus = "UP",
                    DatabaseConnection = dbStatus,
                    Services = servicesStatus
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting status");
                return StatusCode(500, new { error = "Error getting status", details = ex.Message });
            }
        }

        private async Task<SystemServicesStatus> GetServicesStatusAsync(bool includeLogs = false, int logLines = 50)
        {
            var status = new SystemServicesStatus();

            try
            {
                status.ScraperService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("SCRAPER_SERVICE_NAME") ?? "api-scraper", includeLogs, logLines);
                status.VerifierService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("VERIFIER_SERVICE_NAME") ?? "api-verifier", includeLogs, logLines);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service status");
                throw;
            }

            return status;
        }

        private async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, bool includeLogs = false, int logLines = 50)
        {
            var status = new ServiceStatus
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

                // If logs are requested, get them
                if (includeLogs)
                {
                    status.RecentLogs = await GetServiceLogsAsync(serviceName, logLines);

                    // If no logs were found, try a simpler approach
                    if (status.RecentLogs.Count == 0)
                    {
                        status.RecentLogs = await GetServiceLogsSimpleAsync(serviceName, logLines);
                    }
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

        private async Task<List<LogEntry>> GetServiceLogsAsync(string serviceName, int lines)
        {
            var logs = new List<LogEntry>();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"journalctl -u {serviceName}.service --no-pager -n {lines} --output=json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var errorOutput = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    logger.LogWarning("Error from journalctl: {Error}", errorOutput);
                }

                using var reader = process.StandardOutput;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var logEntry = System.Text.Json.JsonSerializer.Deserialize<LogEntry>(line);
                            if (logEntry != null)
                            {
                                logs.Add(logEntry);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Failed to parse log entry: {Line}, Error: {Error}", line, ex.Message);
                        // Add as raw message instead
                        logs.Add(new LogEntry { MESSAGE = $"[Parse Error] {line}" });
                    }
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    logger.LogWarning("journalctl exited with code {ExitCode}", process.ExitCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting logs for service {ServiceName}", serviceName);
            }

            return logs;
        }

        // Fallback method that gets logs in a simpler format
        private async Task<List<LogEntry>> GetServiceLogsSimpleAsync(string serviceName, int lines)
        {
            var logs = new List<LogEntry>();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"journalctl -u {serviceName}.service --no-pager -n {lines}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(output))
                {
                    var logLines = output.Split('\n');
                    foreach (var line in logLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logs.Add(new LogEntry { MESSAGE = line.Trim() });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting simple logs for service {ServiceName}", serviceName);
            }

            return logs;
        }

        public class LogEntry
        {
            // Standard journalctl JSON fields
            public string? __REALTIME_TIMESTAMP { get; set; }
            public string? PRIORITY { get; set; }
            public string? MESSAGE { get; set; }
            public string? SYSLOG_IDENTIFIER { get; set; }
            public string? _SYSTEMD_UNIT { get; set; }

            // Helper properties for display
            public string FormattedTimestamp
            {
                get
                {
                    if (string.IsNullOrEmpty(__REALTIME_TIMESTAMP)) return string.Empty;

                    // Convert microseconds since epoch to DateTime
                    if (long.TryParse(__REALTIME_TIMESTAMP, out long timestamp))
                    {
                        try
                        {
                            // journalctl timestamps are in microseconds
                            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp / 1000)
                                .DateTime.ToUniversalTime();
                            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch
                        {
                            return __REALTIME_TIMESTAMP;
                        }
                    }
                    return __REALTIME_TIMESTAMP;
                }
            }
        }

        public class ServiceStatus
        {
            public string ServiceName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string LastRunTime { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<LogEntry> RecentLogs { get; set; } = [];
            public string LogError { get; set; } = string.Empty;

            public int? NumberOfInstances { get; set; } // Cloud Instances?

            public string MonitoringStatus => $"{ServiceName} IsActive: {IsActive}";
        }

        public class SystemServicesStatus
        {
            public ServiceStatus ScraperService { get; set; } = new();
            public ServiceStatus VerifierService { get; set; } = new();
            public DateTime StatusCheckedAt { get; set; } = DateTime.UtcNow;

            public bool AllServicesRunning => IsServiceRecentlyActive(ScraperService) && IsServiceRecentlyActive(VerifierService);

            private bool IsServiceRecentlyActive(ServiceStatus service)
            {
                // If the service is running, it's considered active
                if (service.IsActive)
                    return true;

                // If not running, check if last run was within the last 1 minute
                if (DateTime.TryParse(service.LastRunTime, out var lastRun))
                {
                    var diff = DateTime.UtcNow - lastRun.ToUniversalTime();
                    return diff.TotalMinutes >= 0 && diff.TotalMinutes <= 1;
                }
                return false;
            }
        }
    }
}
