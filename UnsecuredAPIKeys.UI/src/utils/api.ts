import * as Sentry from "@sentry/react";

import { ApiResponse, RateLimitResponse } from "@/types";

// API base URL from environment variables
const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:7227";

// Debug logging in development
if (process.env.NODE_ENV === 'development') {
  console.log('API_BASE_URL:', API_BASE_URL);
}

// Create a map to store AbortControllers for request cancellation
const requestControllers = new Map<string, AbortController>();

/**
 * Generic API fetch function that handles rate limiting and request cancellation
 */
export async function fetchWithRateLimit<T>(
  endpoint: string,
  options?: RequestInit & { requestId?: string },
): Promise<ApiResponse<T>> {
  const requestId = options?.requestId || endpoint;
  
  // Cancel any existing request with the same ID
  const existingController = requestControllers.get(requestId);
  if (existingController) {
    existingController.abort();
  }

  // Create new AbortController for this request
  const controller = new AbortController();
  requestControllers.set(requestId, controller);

  // Prepare headers with Discord ID if available
  const headers = new Headers(options?.headers);
  
  // Automatically include Discord ID for enhanced rate limits
  if (typeof window !== 'undefined') {
    const discordId = localStorage.getItem('discordId');
    if (discordId) {
      headers.set('X-Discord-Id', discordId);
    }
  }

  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
      signal: controller.signal,
    });

    // Get rate limit info from headers if available
    const rateLimitHeader = {
      limit: Number(response.headers.get("X-RateLimit-Limit") || 0),
      requestsRemaining: Number(
        response.headers.get("X-RateLimit-Remaining") || 0,
      ),
      resetAt:
        response.headers.get("X-RateLimit-Reset") || new Date().toISOString(),
    };

    // Handle rate limiting (429 status)
    if (response.status === 429) {
      const errorData = (await response.json()) as RateLimitResponse;

      return {
        isRateLimited: true,
        rateLimit: errorData.rateLimit,
        error: "Rate limit exceeded. Please try again later.",
        fallbackApiKey: errorData.fallbackApiKey,
      };
    }

    // Handle other errors
    if (!response.ok) {
      // Don't expose internal error details to users
      const userFriendlyError = response.status >= 500
        ? "Server error. Please try again later."
        : response.status === 404
        ? "Resource not found."
        : "Request failed. Please try again.";
        
      return {
        error: userFriendlyError,
        rateLimit: rateLimitHeader.limit
          ? {
              limit: rateLimitHeader.limit,
              requestsRemaining: rateLimitHeader.requestsRemaining,
              requestsCount:
                rateLimitHeader.limit - rateLimitHeader.requestsRemaining,
              resetAt: rateLimitHeader.resetAt,
              timeWindow: "01:00:00", // Default 1 hour
            }
          : undefined,
      };
    }

    // Handle successful response
    const data = await response.json();

    return {
      data: data as T,
      rateLimit: rateLimitHeader.limit
        ? {
            limit: rateLimitHeader.limit,
            requestsRemaining: rateLimitHeader.requestsRemaining,
            requestsCount:
              rateLimitHeader.limit - rateLimitHeader.requestsRemaining,
            resetAt: rateLimitHeader.resetAt,
            timeWindow: "01:00:00", // Default 1 hour
          }
        : undefined,
    };
  } catch (err) {
    // Handle aborted requests gracefully
    if (err instanceof Error && err.name === 'AbortError') {
      return {
        error: "Request was cancelled",
        cancelled: true,
      };
    }

    console.error("API request failed:", err);

    // Capture the error in Sentry
    if (err instanceof Error) {
      Sentry.captureException(err, {
        tags: {
          endpoint,
          method: options?.method || "GET",
        },
        extra: {
          requestOptions: options,
        },
      });
    } else {
      Sentry.captureMessage(`API request failed: ${JSON.stringify(err)}`, {
        level: "error",
        tags: {
          endpoint,
          method: options?.method || "GET",
        },
      });
    }

    return {
      error: "Network error. Please check your connection and try again.",
    };
  } finally {
    // Clean up the controller
    requestControllers.delete(requestId);
  }
}

/**
 * Cancel all pending requests or specific request by ID
 */
export function cancelRequests(requestId?: string) {
  if (requestId) {
    const controller = requestControllers.get(requestId);
    if (controller) {
      controller.abort();
      requestControllers.delete(requestId);
    }
  } else {
    // Cancel all requests
    requestControllers.forEach(controller => controller.abort());
    requestControllers.clear();
  }
}

/**
 * Fetch function for paginated endpoints
 */
export async function fetchPaginated<T>(
  endpoint: string,
  page: number = 1,
  pageSize: number = 50,
  options?: RequestInit & { requestId?: string },
): Promise<ApiResponse<{ items: T[]; totalCount: number; totalPages: number }>> {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });
  
  const fullEndpoint = `${endpoint}?${params.toString()}`;
  const response = await fetchWithRateLimit<any>(fullEndpoint, options);
  
  if (response.data) {
    return {
      ...response,
      data: {
        items: response.data.items || response.data,
        totalCount: response.data.totalCount || 0,
        totalPages: response.data.totalPages || 1,
      },
    };
  }
  
  return response as any;
}
