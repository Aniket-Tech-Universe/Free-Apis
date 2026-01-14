import { RateLimitInfo as RateLimitInfoType, ApiKey } from "@/types";

interface RateLimitInfoProps {
  rateLimitInfo?: RateLimitInfoType;
  isRateLimited?: boolean;
  fallbackApiKey?: ApiKey;
}

export default function RateLimitInfo({
  rateLimitInfo,
  isRateLimited,
  fallbackApiKey,
}: RateLimitInfoProps) {
  if (!rateLimitInfo) return null;

  // Calculate time until reset
  const formatTimeRemaining = () => {
    // Parse the UTC reset time (the API returns UTC time with Z suffix)
    const resetTime = new Date(rateLimitInfo.resetAt);
    // Get current time (Date constructor returns local time, but getTime() converts to UTC milliseconds)
    const now = new Date();
    
    // Calculate difference in milliseconds (both getTime() calls return UTC milliseconds since epoch)
    const diffMs = resetTime.getTime() - now.getTime();

    // If time has passed or is very close, show a user-friendly message
    if (diffMs <= 1000) return "a few seconds"; // Allow 1 second buffer

    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMinutes / 60);
    const remainingMinutes = diffMinutes % 60;

    if (diffHours > 0) {
      return `${diffHours}h ${remainingMinutes}m`;
    }

    if (diffMinutes > 0) {
      return `${diffMinutes}m`;
    }

    return `${diffSeconds}s`;
  };

  return (
    <div
      className={`text-xs p-2 rounded-md ${isRateLimited ? "bg-danger-50 text-danger" : "bg-gray-50 border border-gray-200"}`}
    >
      {isRateLimited ? (
        <div className="flex flex-col">
          <span className="font-bold">Rate limit exceeded</span>
          <span>Try again in {formatTimeRemaining()}</span>
          {fallbackApiKey && (
            <span className="mt-1 text-success">
              But don&apos;t worry, we&apos;ve got a fallback key for you!
            </span>
          )}
        </div>
      ) : (
        <div className="flex items-center gap-2">
          <svg
            className="w-3.5 h-3.5 text-gray-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
            />
          </svg>
          <span>
            <span className="font-medium">
              {rateLimitInfo.requestsRemaining}
            </span>{" "}
            of {rateLimitInfo.limit} requests remaining
          </span>
          <span className="text-gray-400">•</span>
          <span className="text-gray-500">
            Reset in {formatTimeRemaining()}
          </span>
        </div>
      )}
    </div>
  );
}
