using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using UnsecuredAPIKeys.WebAPI.Hubs;

namespace UnsecuredAPIKeys.WebAPI.Services
{
    public interface IActiveUserService
    {
        int ActiveUserCount { get; }
        Task UserConnectedAsync(string connectionId);
        Task UserDisconnectedAsync(string connectionId);
        Task BroadcastUserCountAsync();
        Task ValidateConnectionsAsync();
        Task UpdateLastSeenAsync(string connectionId);
    }

    public class ActiveUserService : IActiveUserService
    {
        private readonly IHubContext<StatsHub> _hubContext;
        private readonly ILogger<ActiveUserService> _logger;
        private readonly ConcurrentDictionary<string, ConnectionInfo> _activeConnections;
        private readonly Timer _cleanupTimer;
        private readonly Timer _validationTimer;

        public int ActiveUserCount => _activeConnections.Count;

        public ActiveUserService(IHubContext<StatsHub> hubContext, ILogger<ActiveUserService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            _activeConnections = new ConcurrentDictionary<string, ConnectionInfo>();
            
            // Cleanup stale connections every 15 seconds  
            _cleanupTimer = new Timer(CleanupStaleConnections, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
            
            // Validate connections every 30 seconds for development
            _validationTimer = new Timer(async _ => await ValidateConnectionsAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task UserConnectedAsync(string connectionId)
        {
            _activeConnections.TryAdd(connectionId, new ConnectionInfo
            {
                ConnectedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            
            _logger.LogInformation("User connected: {ConnectionId}. Active users: {ActiveCount}", 
                connectionId, ActiveUserCount);
            
            await BroadcastUserCountAsync();
        }

        public async Task UserDisconnectedAsync(string connectionId)
        {
            if (_activeConnections.TryRemove(connectionId, out var connectionInfo))
            {
                var duration = DateTime.UtcNow - connectionInfo.ConnectedAt;
                _logger.LogInformation("User disconnected: {ConnectionId}. Duration: {Duration}. Active users: {ActiveCount}", 
                    connectionId, duration, ActiveUserCount);
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent connection: {ConnectionId}", connectionId);
            }
            
            await BroadcastUserCountAsync();
        }

        public async Task BroadcastUserCountAsync()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ActiveUserCountUpdated", ActiveUserCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast user count update");
            }
        }

        public Task UpdateLastSeenAsync(string connectionId)
        {
            if (_activeConnections.TryGetValue(connectionId, out var connectionInfo))
            {
                connectionInfo.LastSeen = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public async Task ValidateConnectionsAsync()
        {
            try
            {
                var connectionsToValidate = _activeConnections.ToList();
                var removedCount = 0;

                _logger.LogInformation("Starting connection validation. Total connections: {TotalConnections}", connectionsToValidate.Count);

                foreach (var kvp in connectionsToValidate)
                {
                    var connectionId = kvp.Key;
                    var connectionInfo = kvp.Value;

                    try
                    {
                        // Try to send a ping to validate the connection
                        await _hubContext.Clients.Client(connectionId).SendAsync("ValidationPing");
                        
                        // Update last seen time
                        if (_activeConnections.TryGetValue(connectionId, out var existingInfo))
                        {
                            existingInfo.LastSeen = DateTime.UtcNow;
                        }
                        
                        _logger.LogDebug("Connection {ConnectionId} is alive", connectionId);
                    }
                    catch (Exception ex)
                    {
                        // Connection is dead, remove it
                        if (_activeConnections.TryRemove(connectionId, out _))
                        {
                            removedCount++;
                            _logger.LogInformation("Removed dead connection during validation: {ConnectionId}. Error: {Error}", connectionId, ex.Message);
                        }
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Connection validation completed. Removed {RemovedCount} dead connections. Active users: {ActiveCount}", 
                        removedCount, ActiveUserCount);
                    await BroadcastUserCountAsync();
                }
                else
                {
                    _logger.LogInformation("Connection validation completed. No dead connections found. Active users: {ActiveCount}", ActiveUserCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection validation");
            }
        }

        private async void CleanupStaleConnections(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Remove connections older than 10 minutes without activity
                var connectionsToRemove = new List<string>();

                foreach (var kvp in _activeConnections)
                {
                    if (kvp.Value.LastSeen < cutoffTime)
                    {
                        connectionsToRemove.Add(kvp.Key);
                    }
                }

                if (connectionsToRemove.Count > 0)
                {
                    var removedCount = 0;
                    foreach (var connectionId in connectionsToRemove)
                    {
                        if (_activeConnections.TryRemove(connectionId, out _))
                        {
                            removedCount++;
                        }
                    }

                    if (removedCount > 0)
                    {
                        _logger.LogInformation("Cleanup removed {RemovedCount} stale connections. Active users: {ActiveCount}", 
                            removedCount, ActiveUserCount);
                        await BroadcastUserCountAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stale connection cleanup");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _validationTimer?.Dispose();
        }
    }

    public class ConnectionInfo
    {
        public DateTime ConnectedAt { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
