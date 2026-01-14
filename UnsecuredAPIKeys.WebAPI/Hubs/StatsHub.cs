using Microsoft.AspNetCore.SignalR;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI.Hubs
{
    public class StatsHub(ILogger<StatsHub> logger, IActiveUserService activeUserService)
        : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
            var ipAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
            
            logger.LogInformation("Client connected: {ConnectionId} from {IpAddress} with UserAgent: {UserAgent}", 
                Context.ConnectionId, ipAddress, userAgent);
            
            await activeUserService.UserConnectedAsync(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                logger.LogWarning("Client disconnected with error: {ConnectionId}, Error: {Error}", 
                    Context.ConnectionId, exception.Message);
            }
            else
            {
                logger.LogInformation("Client disconnected cleanly: {ConnectionId}", Context.ConnectionId);
            }
            
            await activeUserService.UserDisconnectedAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // Add a ping method for connection health monitoring
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }

        // Handle pong responses to update last seen time
        public async Task Pong()
        {
            // Update last seen time for this connection
            await activeUserService.UpdateLastSeenAsync(Context.ConnectionId);
        }

        // Method to get current active user count
        public async Task GetActiveUserCount()
        {
            await Clients.Caller.SendAsync("ActiveUserCountUpdated", activeUserService.ActiveUserCount);
        }

        // Method to manually trigger connection validation (useful for debugging)
        public async Task ValidateConnections()
        {
            await activeUserService.ValidateConnectionsAsync();
        }
    }
}
