using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.WebAPI.Models
{
    public class RateLimitResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 429; // Too Many Requests
        public RateLimitInfo RateLimit { get; set; } = new();
        public APIKey? FallbackApiKey { get; set; }
        
        public class RateLimitInfo
        {
            public int Limit { get; set; }
            public int RequestsRemaining { get; set; }
            public int RequestsCount { get; set; }
            public TimeSpan TimeWindow { get; set; }
            public DateTime ResetAt { get; set; }
        }
    }
}
