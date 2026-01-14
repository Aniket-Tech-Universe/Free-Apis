import Head from "next/head";
import { Button } from "@heroui/button";
import { Card, CardBody, CardHeader } from "@heroui/card";
import { CheckIcon } from "@heroicons/react/24/outline";
import { HeartIcon } from "@heroicons/react/24/solid";
import NextLink from "next/link";
import { useState, useEffect } from "react";

import { title, subtitle } from "@/components/primitives";
import DefaultLayout from "@/layouts/default";
import { fetchWithRateLimit } from "@/utils/api";

interface RateLimitInfo {
  maxRequests: number;
  discordMaxRequests: number;
  timeWindowHours: number;
  description: string;
  note: string;
}

export default function PricingPage() {
  const [rateLimitInfo, setRateLimitInfo] = useState<RateLimitInfo | null>(null);

  useEffect(() => {
    const fetchRateLimitInfo = async () => {
      try {
        const response = await fetchWithRateLimit<RateLimitInfo>("/API/GetRateLimitInfo", {
          requestId: "rateLimitInfo",
        });

        if (response.data) {
          setRateLimitInfo(response.data);
        }
      } catch (error) {
        console.error("Failed to fetch rate limit info:", error);
        // Fallback to default values if API fails
        setRateLimitInfo({
          maxRequests: 5,
          discordMaxRequests: 20,
          timeWindowHours: 1,
          description: "Fair usage limits to ensure service availability for all users",
          note: "After reaching the limit, a fallback key will be provided instead of blocking access"
        });
      }
    };

    fetchRateLimitInfo();
  }, []);

  const getRateLimitText = () => {
    if (!rateLimitInfo) return "Fair usage limits*";

    return `Fair usage limits (${rateLimitInfo.maxRequests} requests/${rateLimitInfo.timeWindowHours} hour)*`;
  };

  return (
    <>
      <Head>
        <title>Pricing - Unsecured API Keys</title>
        <meta
          name="description"
          content="Our pricing is simple: completely free! Help secure the web without any cost. No hidden fees, no premium tiers - just free API key security tools."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="pricing, free security tools, API key security, cost, GitHub security, free cybersecurity, developer tools" />
        <link rel="canonical" href="https://unsecuredapikeys.com/pricing/" />

        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Unsecured API Keys" />
        <meta property="og:title" content="Pricing - Unsecured API Keys" />
        <meta property="og:description" content="Our pricing is simple: completely free! Help secure the web without any cost. No hidden fees, no premium tiers." />
        <meta property="og:url" content="https://unsecuredapikeys.com/pricing/" />
        <meta property="og:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:image:alt" content="Pricing - Free Security Tools at Unsecured API Keys" />
        <meta property="og:locale" content="en_US" />

        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Pricing - Unsecured API Keys" />
        <meta name="twitter:description" content="Our pricing is simple: completely free! Help secure the web without any cost." />
        <meta name="twitter:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta name="twitter:image:alt" content="Pricing - Free Security Tools at Unsecured API Keys" />

        {/* Structured Data - Pricing */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              "@context": "https://schema.org",
              "@type": "WebPage",
              "name": "Pricing - Unsecured API Keys",
              "url": "https://unsecuredapikeys.com/pricing",
              "description": "Our pricing is simple: completely free! Help secure the web without any cost.",
              "mainEntity": {
                "@type": "Product",
                "name": "Unsecured API Keys Security Tool",
                "description": "Free tool to find and report exposed API keys in public repositories",
                "offers": {
                  "@type": "Offer",
                  "price": "0",
                  "priceCurrency": "USD",
                  "availability": "https://schema.org/InStock",
                  "priceValidUntil": "2030-12-31"
                }
              }
            })
          }}
        />

        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
          <div className="inline-block max-w-4xl text-center justify-center">
            <h1 className={title()}>Pricing</h1>
            <h2 className={subtitle({ class: "mt-4" })}>
              Simple, transparent, and completely free
              <br />
              <span className="italic">
                Because security shouldn&apos;t cost a fortune 💸
              </span>
            </h2>
          </div>

          <div className="max-w-5xl mt-8">
            <div className="grid md:grid-cols-3 gap-6">
              {/* Free Tier */}
              <Card className="relative border-2 border-primary overflow-visible mt-8">
                <div className="absolute -top-3 left-1/2 transform -translate-x-1/2 z-10">
                  <div className="bg-primary text-white px-4 py-2 rounded-full text-xs font-semibold whitespace-nowrap">
                    Most Popular for Poor People
                  </div>
                </div>
                <CardHeader className="text-center pt-12">
                  <h3 className="text-xl font-bold">Free Forever</h3>
                  <div className="mt-4">
                    <span className="text-4xl font-bold">$0</span>
                    <span className="text-gray-500 text-sm">/forever</span>
                  </div>
                  <p className="text-gray-600 mt-2 text-sm">
                    Perfect for people who don&apos;t want to see this site live forever
                  </p>
                </CardHeader>
                <CardBody className="pt-0">
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Unlimited API key viewing</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>GitHub issue submission</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Leaderboard participation</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>All supported API types</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Real-time statistics</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>{getRateLimitText()}</span>
                    </li>
                  </ul>
                  <div className="space-y-3 mt-4">
                    <NextLink href="/">
                      <Button
                        color="primary"
                        variant="shadow"
                        className="w-full"
                        size="md"
                      >
                        Get Started Now
                      </Button>
                    </NextLink>
                  </div>
                </CardBody>
              </Card>

              {/* Discord Tier */}
              <Card className="relative border-2 border-indigo-500 overflow-visible mt-8 bg-gradient-to-br from-indigo-50 to-purple-50 dark:from-indigo-950/30 dark:to-purple-950/30">
                <div className="absolute -top-3 left-1/2 transform -translate-x-1/2 z-10">
                  <div className="bg-gradient-to-r from-indigo-500 to-purple-500 text-white px-4 py-2 rounded-full text-xs font-semibold whitespace-nowrap">
                    ⭐ VIP Chaos Enthusiast
                  </div>
                </div>
                <CardHeader className="text-center pt-12">
                  <h3 className="text-xl font-bold text-indigo-600 dark:text-indigo-400">Discord Member</h3>
                  <div className="mt-4">
                    <span className="text-4xl font-bold text-indigo-600 dark:text-indigo-400">$0</span>
                    <span className="text-gray-500 text-sm">/forever</span>
                  </div>
                  <p className="text-indigo-600 dark:text-indigo-400 mt-2 text-sm font-medium">
                    For developers who like to watch chaos unfold in real-time! 🍿
                  </p>
                </CardHeader>
                <CardBody className="pt-0">
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span className="font-semibold text-indigo-600 dark:text-green-400">Costs literally nothing.</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span>Everything in Free tier</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span className="font-semibold text-indigo-600 dark:text-indigo-400">{rateLimitInfo?.discordMaxRequests || 20} requests/hour</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span>Live panic notifications 📢</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span>Developer therapy sessions 🛋️</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span>Automatic recognition 🎭</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-indigo-500 mr-2" />
                      <span>Epic memes & gifs 🎪</span>
                    </li>
                  </ul>
                  <div className="space-y-3 mt-4">
                    <Button
                      className="w-full bg-gradient-to-r from-indigo-500 to-purple-500 hover:from-indigo-600 hover:to-purple-600 text-white font-semibold"
                      onPress={() => {
                        if (typeof window !== "undefined" && (window as any).gtag) {
                          (window as any).gtag("event", "discord_join_click", {
                            event_category: "pricing",
                            event_label: "discord_tier_button",
                          });
                        }
                        window.open("https://discord.gg/SyjgFTd7Ee", "_blank");
                      }}
                      radius="lg"
                      size="md"
                      variant="shadow"
                    >
                      Join Discord Server
                    </Button>
                  </div>
                </CardBody>
              </Card>

              {/* Premium Tier (Joke) */}
              <Card className="relative border-2 border-pink-500 overflow-visible mt-8">
                <div className="absolute -top-3 left-1/2 transform -translate-x-1/2 z-10">
                  <div className="bg-gradient-to-r from-pink-500 to-rose-500 text-white px-4 py-2 rounded-full text-xs font-semibold whitespace-nowrap">
                    💸 For Sexy Rich People
                  </div>
                </div>
                <CardHeader className="text-center pt-12">
                  <h3 className="text-xl font-bold text-gray-400">Premium</h3>
                  <div className="mt-4">
                    <span className="text-4xl font-bold text-gray-400">$∞</span>
                    <span className="text-gray-500 text-sm">/forever</span>
                  </div>
                  <p className="text-gray-400 mt-2 text-sm">
                    For those with massive amounts of sex appeal
                  </p>
                </CardHeader>
                <CardBody className="pt-0">
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Same as free tier</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">But costs money</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Bragging rights</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Golden badge*</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Our eternal gratitude</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Optionally included in our supporters page</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">*Badge not included</span>
                    </li>
                  </ul>
                  <div className="space-y-3 mt-4">
                    <Button
                      className="w-full bg-gradient-to-r from-pink-500 to-rose-500 hover:from-pink-600 hover:to-rose-600 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-300"
                      onPress={() => {
                        if (typeof window !== "undefined" && (window as any).gtag) {
                          (window as any).gtag("event", "donate_click", {
                            event_category: "donation",
                            event_label: "pricing_premium_tier",
                          });
                        }
                        window.open("https://www.paypal.com/donate/?hosted_button_id=2FPWHYZ949CE8", "_blank");
                      }}
                      radius="lg"
                      size="md"
                      startContent={<HeartIcon className="h-4 w-4" />}
                      variant="shadow"
                    >
                      Support This Project
                    </Button>
                  </div>
                </CardBody>
              </Card>
            </div>

            <div className="mt-12 text-center">
              <h3 className="text-2xl font-bold mb-4">Frequently Asked Questions 🤔</h3>
              <div className="grid md:grid-cols-2 gap-8 mt-8 text-left">
                <div>
                  <h4 className="font-bold mb-2">Is this really free?</h4>
                  <p className="text-gray-600 text-sm">
                    Yes! This is a passion project to help improve web security.
                    No hidden costs, no premium tiers, no enterprise nonsense.
                  </p>
                </div>
                <div>
                  <h4 className="font-bold mb-2">What&apos;s the catch?</h4>
                  <p className="text-gray-600 text-sm">
                    There isn&apos;t one. Just help make the web more secure by
                    reporting exposed keys when you find them.
                  </p>
                </div>
                <div>
                  <h4 className="font-bold mb-2">Can I donate?</h4>
                  <p className="text-gray-600 text-sm mb-3">
                    Absolutely! We&apos;d be thrilled if you wanted to help keep our servers
                    running and our coffee cups full. Every donation helps us continue
                    scanning for exposed API keys 24/7.
                  </p>
                  <Button
                    className="bg-gradient-to-r from-pink-500 to-rose-500 hover:from-pink-600 hover:to-rose-600 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-300"
                    onPress={() => {
                      if (typeof window !== "undefined" && (window as any).gtag) {
                        (window as any).gtag("event", "donate_click", {
                          event_category: "donation",
                          event_label: "pricing_faq",
                        });
                      }
                      window.open("https://www.paypal.com/donate/?hosted_button_id=2FPWHYZ949CE8", "_blank");
                    }}
                    radius="lg"
                    size="sm"
                    startContent={<HeartIcon className="h-4 w-4" />}
                    variant="shadow"
                  >
                    Donate Now
                  </Button>
                </div>
                <div>
                  <h4 className="font-bold mb-2">Will you add paid features?</h4>
                  <p className="text-gray-600 text-sm">
                    Highly unlikely. The core mission of helping secure exposed
                    API keys should remain free and accessible to everyone.
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-12 text-center">
              <div className="bg-gray-50 rounded-lg p-8">
                <h3 className="text-xl font-bold mb-4">The Real Cost 💭</h3>
                <p className="text-gray-600 mb-4">
                  While our service is free, exposed API keys can cost developers
                  and companies thousands of dollars in unauthorized usage,
                  security breaches, and remediation efforts.
                </p>
                <p className="text-gray-600">
                  Help us prevent these costs by responsibly reporting exposed
                  keys. Together, we can make the web a more secure place! 🛡️
                </p>
              </div>
            </div>
          </div>
        </section>
      </DefaultLayout>
    </>
  );
}
