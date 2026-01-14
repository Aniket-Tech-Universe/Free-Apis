using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace UnsecuredAPIKeys.Bots.Verifier.Services
{
    /// <summary>
    /// Tracks API keys that repeatedly error during verification
    /// </summary>
    public class ErrorTrackingService
    {
        private readonly ILogger<ErrorTrackingService> _logger;
        private readonly ConcurrentDictionary<long, int> _errorCounts = new();
        private readonly int _maxErrorsBeforeInvalid;

        public ErrorTrackingService(ILogger<ErrorTrackingService> logger, int maxErrorsBeforeInvalid = 5)
        {
            _logger = logger;
            _maxErrorsBeforeInvalid = maxErrorsBeforeInvalid;
        }

        /// <summary>
        /// Increment error count for a key
        /// </summary>
        /// <returns>True if key should be marked as invalid due to too many errors</returns>
        public bool IncrementErrorCount(long keyId)
        {
            var errorCount = _errorCounts.AddOrUpdate(keyId, 1, (key, oldValue) => oldValue + 1);
            
            _logger.LogDebug("Key {KeyId} error count: {ErrorCount}/{MaxErrors}", 
                keyId, errorCount, _maxErrorsBeforeInvalid);

            if (errorCount >= _maxErrorsBeforeInvalid)
            {
                _logger.LogWarning("Key {KeyId} has reached max error threshold ({MaxErrors}). Should be marked as invalid.", 
                    keyId, _maxErrorsBeforeInvalid);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset error count for a key (e.g., when it validates successfully)
        /// </summary>
        public void ResetErrorCount(long keyId)
        {
            if (_errorCounts.TryRemove(keyId, out var previousCount))
            {
                _logger.LogDebug("Reset error count for key {KeyId} (was {PreviousCount})", 
                    keyId, previousCount);
            }
        }

        /// <summary>
        /// Get current error count for a key
        /// </summary>
        public int GetErrorCount(long keyId)
        {
            return _errorCounts.GetOrAdd(keyId, 0);
        }

        /// <summary>
        /// Clear all error counts (e.g., between verification cycles)
        /// </summary>
        public void ClearAll()
        {
            _errorCounts.Clear();
            _logger.LogInformation("Cleared all error counts");
        }
    }
}
