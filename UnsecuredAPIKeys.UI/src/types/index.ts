import { SVGProps } from "react";

export type IconSvgProps = SVGProps<SVGSVGElement> & {
  size?: number;
};

export interface ApiKeyReference {
  repoURL: string;
  repoOwner: string;
  fileURL: string;
  filePath: string;
  codeContext?: string;
  lineNumber: number;
  // Additional fields that might come from API
  repoName?: string;
  fileName?: string;
  branch?: string;
  fileSHA?: string;
  provider?: string;
}

export interface ApiKey {
  id: number; // Added missing id field
  apiType: string;
  apiKey: string;
  timesDisplayed: number;
  status: "Valid" | "Invalid" | "Unverified" | "NoLongerWorking" | "Removed" | "FlaggedForRemoval" | "Error" | "ValidNoCredits"; // All possible API status values
  lastValidUTC: string; // ISO date string
  lastCheckedUTC?: string; // ISO date string - when the key was last checked
  firstFoundUTC: string; // ISO date string
  lastFoundUTC?: string; // ISO date string - when the key was last found
  searchProvider?: number; // Search provider ID
  references: ApiKeyReference[];
}

export interface KeyStats {
  totalNumberOfKeys: number;
  numberOfValidKeys: number;
  newKeysFoundToday: number;
  mostRecentFind: string; // Or could be a date string depending on format
}

export interface ApiKeyType {
  apiType: string;
  keyCount: number;
  apiTypeId: number;
}

// Rate limiting types
export interface RateLimitInfo {
  limit: number;
  requestsRemaining: number;
  requestsCount: number;
  timeWindow: string;
  resetAt: string;
}

export interface RateLimitResponse {
  success: boolean;
  message: string;
  statusCode: number;
  rateLimit: RateLimitInfo;
  fallbackApiKey?: ApiKey;
}

export interface ApiResponse<T> {
  data?: T;
  rateLimit?: RateLimitInfo;
  error?: string;
  isRateLimited?: boolean;
  fallbackApiKey?: ApiKey;
  cancelled?: boolean;
}

export interface GitHubUserInfo {
  username: string;
  avatarUrl: string;
  userId?: number;
  displayName?: string;
  issueCount: number;
}

export interface SnitchLeaderboardEntry {
  rank: number;
  displayName: string;
  totalIssuesSubmitted: number;
  openIssuesSubmitted: number;
  closedIssuesSubmitted: number;
  totalRepositoriesAffected: number;
  favoriteApiType?: string;
  snitchScore: number;
  firstSubmissionAt: string;
  lastSubmissionAt: string;
  consecutiveDaysActive: number;
  gitHubUsers?: GitHubUserInfo[]; // Make optional since API might not return this
}

export interface LeaderboardStats {
  totalSnitches: number;
  totalIssuesSubmitted: number;
  totalIssuesVerified: number;
  openIssues: number;
  closedIssues: number;
  successRate: number;
}

export interface LeaderboardEntry {
  githubUsername: string;
  totalSubmissions: number;
  lastSubmissionDate: string;
}

// Google Analytics Types - declarations moved to avoid conflicts

export interface GtagConfig {
  page_title?: string;
  page_location?: string;
  custom_map?: { [key: string]: string };
}

export interface GtagEvent {
  action: string;
  category?: string;
  label?: string;
  value?: number;
}
