import { useState, useEffect } from "react";
import Head from "next/head";
import { Button } from "@heroui/button";
import { Card, CardBody } from "@heroui/card";
import { Snippet } from "@heroui/snippet";
import { Divider } from "@heroui/divider";
import NextLink from "next/link";
import Image from "next/image";

import AnimatedNumber from "@/components/AnimatedNumber";
import { SnitchLeaderboardEntry } from "@/types";
import { title, subtitle } from "@/components/primitives";
import DefaultLayout from "@/layouts/default";
import { fetchWithRateLimit } from "@/utils/api";

export default function LeaderboardPage() {
  const [leaderboard, setLeaderboard] = useState<SnitchLeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    const fetchLeaderboard = async () => {
      const response = await fetchWithRateLimit<SnitchLeaderboardEntry[]>(
        "/API/GetSnitchLeaderboard",
      );

      if (response.data) {
        setLeaderboard(response.data);
        setLoading(false);
      } else {
        setLoading(false);
        setError(true);
      }

      if (response.error) {
        console.error("Failed to fetch leaderboard:", response.error);
      }
    };

    fetchLeaderboard();
  }, []);

  const formatTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    const intervals = {
      year: 31536000,
      month: 2592000,
      week: 604800,
      day: 86400,
      hour: 3600,
      minute: 60,
      second: 1,
    };

    if (seconds < 5) return "just now";

    for (const [unit, secondsInUnit] of Object.entries(intervals)) {
      const interval = Math.floor(seconds / secondsInUnit);

      if (interval >= 1) {
        return interval === 1 ? `1 ${unit} ago` : `${interval} ${unit}s ago`;
      }
    }

    return "just now";
  };

  const getRankEmoji = (rank: number) => {
    switch (rank) {
      case 1:
        return "🥇";
      case 2:
        return "🥈";
      case 3:
        return "🥉";
      default:
        return `#${rank}`;
    }
  };

  const getRankColor = (rank: number) => {
    switch (rank) {
      case 1:
        return "text-yellow-500";
      case 2:
        return "text-gray-400";
      case 3:
        return "text-orange-600";
      default:
        return "text-gray-600";
    }
  };

  return (
    <>
      <Head>
        <title>Leaderboard - Unsecured API Keys</title>
        <meta
          name="description"
          content="See who are the top contributors helping to secure the web by reporting exposed API keys. Hall of Fame for security heroes and biggest snitches."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="leaderboard, security heroes, API key reports, GitHub security, cybersecurity contributors, code security" />
        <link rel="canonical" href="https://unsecuredapikeys.com/leaderboard/" />
        
        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Unsecured API Keys" />
        <meta property="og:title" content="Leaderboard - Unsecured API Keys" />
        <meta property="og:description" content="See who are the top contributors helping to secure the web by reporting exposed API keys. Hall of Fame for security heroes." />
        <meta property="og:url" content="https://unsecuredapikeys.com/leaderboard/" />
        <meta property="og:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:image:alt" content="Leaderboard - Security Heroes at Unsecured API Keys" />
        <meta property="og:locale" content="en_US" />
        
        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Leaderboard - Unsecured API Keys" />
        <meta name="twitter:description" content="See who are the top contributors helping to secure the web by reporting exposed API keys." />
        <meta name="twitter:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta name="twitter:image:alt" content="Leaderboard - Security Heroes at Unsecured API Keys" />
        
        {/* Structured Data - Leaderboard */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              "@context": "https://schema.org",
              "@type": "WebPage",
              "name": "Security Heroes Leaderboard",
              "url": "https://unsecuredapikeys.com/leaderboard",
              "description": "See who are the top contributors helping to secure the web by reporting exposed API keys.",
              "mainEntity": {
                "@type": "ItemList",
                "name": "Biggest Snitches Leaderboard",
                "description": "Ranking of top contributors who have reported exposed API keys to help secure repositories."
              }
            })
          }}
        />
        
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
          <div className="inline-block max-w-2xl text-center justify-center">
            <h1 className={title()}>Biggest Snitches Leaderboard 🏆</h1>
            <h2 className={subtitle({ class: "mt-4" })}>
              Hall of Fame for Security Heroes
              <br />
              <span className="italic">
                Because sometimes being a snitch saves the day! 🕵️‍♂️
              </span>
            </h2>
            <p className="text-sm mt-2 text-gray-500">
              These legends have helped developers avoid embarrassing (and
              expensive) conversations with their bosses by reporting exposed API
              keys.
            </p>

            <div className="mt-6">
              <Snippet hideCopyButton hideSymbol variant="bordered">
                <span>
                  Want to join the leaderboard? Start submitting GitHub issues for
                  exposed keys! 🚀
                </span>
              </Snippet>
            </div>
          </div>

          {loading ? (
            <div className="w-full max-w-4xl mt-8">
              <div className="space-y-4">
                {[...Array(10)].map((_, i) => (
                  <div
                    key={i}
                    className="animate-pulse p-6 border rounded-lg bg-content1"
                  >
                    <div className="flex items-center space-x-4">
                      <div className="h-12 w-12 bg-gray-200 rounded-full" />
                      <div className="flex-1 space-y-2">
                        <div className="h-4 w-1/4 bg-gray-200 rounded" />
                        <div className="h-3 w-1/2 bg-gray-200 rounded" />
                      </div>
                      <div className="h-6 w-16 bg-gray-200 rounded" />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : error ? (
            <div className="w-full max-w-2xl mt-8 p-6 border border-danger rounded-lg bg-danger-50 text-center">
              <svg
                className="h-16 w-16 mx-auto text-danger mb-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                xmlns="http://www.w3.org/2000/svg"
              >
                <path
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                />
              </svg>

              <h3 className="text-xl font-bold mb-2 text-danger">
                Leaderboard Temporarily Unavailable
              </h3>
              <p className="mb-3">
                Our leaderboard is currently taking a coffee break. Even heroes
                need rest! ☕
              </p>

              <Button
                color="danger"
                variant="flat"
                onPress={() => window.location.reload()}
              >
                Try Again
              </Button>
            </div>
          ) : (
            <div className="w-full max-w-4xl mt-8">
              {leaderboard.length === 0 ? (
                <div className="text-center p-8">
                  <h3 className="text-xl font-bold mb-4">
                    No snitches yet! 🤷‍♂️
                  </h3>
                  <p className="text-gray-600">
                    Be the first to report an exposed API key and claim your spot
                    on the leaderboard!
                  </p>
                </div>
              ) : (
                <div className="space-y-4">
                  {leaderboard.map((entry, index) => {
                    const rank = entry.rank || index + 1;
                    const githubUser = entry.gitHubUsers && entry.gitHubUsers.length > 0 ? entry.gitHubUsers[0] : null;
                    const displayName = entry.displayName;
                    const avatarUrl = githubUser?.avatarUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(displayName)}&background=6366f1&color=ffffff`;
                    const profileUrl = githubUser?.username ? `https://github.com/${githubUser.username}` : null;
                    
                    return (
                      <Card key={`${entry.rank}-${entry.displayName}`} className="w-full">
                        <CardBody className="p-6">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center space-x-4">
                              <div
                                className={`text-3xl font-bold ${getRankColor(rank)} min-w-[60px]`}
                              >
                                {getRankEmoji(rank)}
                              </div>

                              <div className="flex items-center space-x-3">
                                <Image
                                  src={avatarUrl}
                                  alt={`${displayName} avatar`}
                                  width={48}
                                  height={48}
                                  unoptimized
                                  className="w-12 h-12 rounded-full border-2 border-gray-200"
                                />
                                <div>
                                  <h3 className="text-lg font-semibold">
                                    {profileUrl ? (
                                      <a
                                        href={profileUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="hover:text-primary transition-colors"
                                      >
                                        {displayName}
                                      </a>
                                    ) : (
                                      <span>{displayName}</span>
                                    )}
                                  </h3>
                                  <p className="text-sm text-gray-500">
                                    Last submission:{" "}
                                    {formatTimeAgo(entry.lastSubmissionAt)}
                                  </p>
                                  {entry.favoriteApiType && (
                                    <p className="text-xs text-gray-400">
                                      Favorite: {entry.favoriteApiType}
                                    </p>
                                  )}
                                </div>
                              </div>
                            </div>

                            <div className="text-right">
                              <div className="text-2xl font-bold text-primary">
                                <AnimatedNumber
                                  value={entry.totalIssuesSubmitted.toString()}
                                />
                              </div>
                              <p className="text-sm text-gray-500">
                                submission{entry.totalIssuesSubmitted !== 1 ? "s" : ""}
                              </p>
                              <p className="text-xs text-gray-400">
                                Score: {entry.snitchScore}
                              </p>
                            </div>
                          </div>

                          {rank <= 3 && (
                            <div className="mt-4 pt-4 border-t border-gray-200">
                              <div className="flex items-center justify-center space-x-2 text-sm text-gray-600">
                                <span>🏆</span>
                                <span>
                                  {rank === 1 && "Ultimate Security Snitch"}
                                  {rank === 2 && "Master Key Hunter"}
                                  {rank === 3 && "Elite Secret Spotter"}
                                </span>
                                <span>🏆</span>
                              </div>
                            </div>
                          )}

                          {entry.totalRepositoriesAffected > 0 && (
                            <div className="mt-3 pt-3 border-t border-gray-100">
                              <div className="flex justify-between text-xs text-gray-500">
                                <span>Repos affected: {entry.totalRepositoriesAffected}</span>
                                <span>Open: {entry.openIssuesSubmitted} | Closed: {entry.closedIssuesSubmitted}</span>
                              </div>
                            </div>
                          )}
                        </CardBody>
                      </Card>
                    );
                  })}
                </div>
              )}

              <div className="mt-8 text-center">
                <Divider className="my-6" />
                <h3 className="text-lg font-semibold mb-4">
                  Ready to Join the Fight? 🛡️
                </h3>
                <p className="text-gray-600 mb-4">
                  Head over to the main page and start submitting issues for
                  exposed API keys. Every submission helps make the web a more
                  secure place!
                </p>
                <NextLink href="/">
                  <Button
                    color="primary"
                    variant="shadow"
                    size="lg"
                  >
                    Start Hunting Keys 🔍
                  </Button>
                </NextLink>
              </div>
            </div>
          )}
        </section>
      </DefaultLayout>
    </>
  );
}
