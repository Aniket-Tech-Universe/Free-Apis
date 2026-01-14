using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UnsecuredAPIKeys.WebAPI.Commons;

public class AllowedReferrersAttribute : TypeFilterAttribute
{
    private static readonly List<string> _allowedReferrers = [
        "localhost:5173", // Allow local dev (UI)
        "localhost:7227", // Allow local dev (API / Scalar)
        "localhost:3000",
        "localhost:3001",
        "localhost:3000",
        "localhost:3001",
        "localhost:5173"
    ];

    public AllowedReferrersAttribute(params string[] additionalReferrers)
        : base(typeof(AllowedReferrersFilter))
    {
        Arguments = [_allowedReferrers.Concat(additionalReferrers).ToArray()];
    }

    private class AllowedReferrersFilter(string[] allowedReferrers) : IActionFilter
    {
        private readonly string[] _filterAllowedReferrers = allowedReferrers;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var referrer = context.HttpContext.Request.Headers["Referer"];
            if (!string.IsNullOrEmpty(referrer))
            {
                var referrerUri = new Uri(referrer!); // Added null-forgiving operator
                var referrerDomain = referrerUri.Authority.ToLower();

                if (!_filterAllowedReferrers.Any(allowedReferrer => referrerDomain.EndsWith(allowedReferrer, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new StatusCodeResult(403); // Forbidden
                }
            }
            else
            {
                if (!(context.HttpContext.Request.Host.Value?.Contains("localhost") ?? false)) context.Result = new StatusCodeResult(403); // Forbidden
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not needed in this case
        }
    }
}
