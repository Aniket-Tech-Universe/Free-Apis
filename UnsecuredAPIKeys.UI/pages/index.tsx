import { useState, useEffect, useMemo, Suspense } from "react";
import { Link } from "@heroui/link";
import { Button } from "@heroui/button";
import { Code } from "@heroui/code";
import { Dropdown, DropdownTrigger, DropdownMenu, DropdownItem } from "@heroui/dropdown";
import Head from "next/head";
import Cookies from "js-cookie";

import { title, subtitle } from "@/components/primitives";
// Donation/Discord components removed
import { Tooltip } from "@heroui/tooltip";

// Enhanced Loading Messages
const loadingMessages = [
  "Hacking into the mainframe...",
  "Asking the API gods for mercy...",
  "Bribing GitHub's rate limiter...",
  "Searching for developers' dignity...",
  "Compiling list of career-ending secrets...",
  "Downloading someone's LLM bill...",
];

// Enhanced Error Messages
const errorMessages = [
  "Our hamsters stopped running the servers. We're bribing them with treats. �",
  "404: API key not found. Just like your job security!",
  "The keys are playing hide and seek. They're winning.",
  "Error: Too many developers crying at once. Please try again.",
  "Our servers are having an existential crisis. Again.",
];

interface RateLimitInfo {
  maxRequests: number;
  discordMaxRequests: number;
  timeWindowHours: number;
  description: string;
  note: string;
}

export default function IndexPage() {
  const [apiKey, setApiKey] = useState<ApiKey | null>(null);
  const [keyStats, setKeyStats] = useState<KeyStats | null>(null);
  const [apiKeyTypes, setApiKeyTypes] = useState<ApiKeyType[]>([]);
  const [selectedKeyType, setSelectedKeyType] = useState<string | number>(
    "Random",
  );
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [showDrawer, setShowDrawer] = useState<boolean | null>(null);
  const [loadingMessage, setLoadingMessage] = useState(loadingMessages[0]);
  const [errorMessage, setErrorMessage] = useState(errorMessages[0]);
  const [copySuccess, setCopySuccess] = useState(false);

  // Pipeline State
  const [pipelineRunning, setPipelineRunning] = useState(false);
  const [pipelineStatus, setPipelineStatus] = useState("");

  const handleRunPipeline = async () => {
    setPipelineRunning(true);
    setPipelineStatus("Triggering Pipeline...");
    try {
      const res = await fetchWithRateLimit("/API/RunPipeline", { method: "POST", requestId: "pipeline" });
      if (res.data) {
        setPipelineStatus("Pipeline Started! 🚀");
        setTimeout(() => setPipelineStatus(""), 3000);
      } else {
        setPipelineStatus("Failed to start.");
      }
    } catch (e) {
      console.error(e);
      setPipelineStatus("Error triggering pipeline.");
    } finally {
      setPipelineRunning(false);
    }
  };

  // Enhanced API type colors with glow effects
  const apiTypeColors: Record<string, { bg: string; text: string; border: string; glow: string }> = {
    OpenAI: {
      bg: "bg-[#74AA9C]/5",
      text: "text-[#74AA9C]",
      border: "border-[#74AA9C]/20",
      glow: "glow-openai",
    },
    AnthropicClaude: {
      bg: "bg-[#5436DA]/5",
      text: "text-[#5436DA]",
      border: "border-[#5436DA]/20",
      glow: "glow-anthropic",
    },
    GoogleAI: {
      bg: "bg-[#4285F4]/5",
      text: "text-[#4285F4]",
      border: "border-[#4285F4]/20",
      glow: "glow-google",
    },
    Cohere: {
      bg: "bg-[#F9B3AC]/5",
      text: "text-[#F9B3AC]",
      border: "border-[#F9B3AC]/20",
      glow: "glow-cohere",
    },
    HuggingFace: {
      bg: "bg-[#FFD21E]/5",
      text: "text-[#FFD21E]",
      border: "border-[#FFD21E]/20",
      glow: "glow-huggingface",
    },
    StabilityAI: {
      bg: "bg-[#FF2BC2]/5",
      text: "text-[#FF2BC2]",
      border: "border-[#FF2BC2]/20",
      glow: "glow-stability",
    },
    MistralAI: {
      bg: "bg-[#DE4815]/5",
      text: "text-[#DE4815]",
      border: "border-[#DE4815]/20",
      glow: "glow-mistral",
    },
    Replicate: {
      bg: "bg-[#000000]/5",
      text: "text-[#000000] dark:text-[#FFFFFF]",
      border: "border-[#000000]/20 dark:border-[#FFFFFF]/20",
      glow: "",
    },
    TogetherAI: {
      bg: "bg-[#5B44F3]/5",
      text: "text-[#5B44F3]",
      border: "border-[#5B44F3]/20",
      glow: "glow-together",
    },
    OpenRouter: {
      bg: "bg-[#3B82F6]/5",
      text: "text-[#3B82F6]",
      border: "border-[#3B82F6]/20",
      glow: "glow-openrouter",
    },
  };

  // Rotate loading messages
  useEffect(() => {
    if (loading) {
      const interval = setInterval(() => {
        setLoadingMessage(prev => {
          const currentIndex = loadingMessages.indexOf(prev);
          return loadingMessages[(currentIndex + 1) % loadingMessages.length];
        });
      }, 1500);
      return () => clearInterval(interval);
    }
  }, [loading]);

  // Fetch API Key Types
  useEffect(() => {
    const fetchKeyTypes = async () => {
      const response = await fetchWithRateLimit<ApiKeyType[]>("/API/GetKeyTypes", {
        requestId: "keyTypes",
      });

      if (response.cancelled) return;

      if (response.data) {
        setApiKeyTypes(response.data);
      }

      if (response.error) {
        console.error("Failed to fetch API key types:", response.error);
      }
    };

    fetchKeyTypes();

    return () => {
      cancelRequests("keyTypes");
    };
  }, []);

  // Fetch Random Key
  useEffect(() => {
    const hasSeenDisclaimer = Cookies.get("hasSeenDisclaimer");
    setShowDrawer(hasSeenDisclaimer !== "true");

    const fetchRandomKey = async () => {
      setLoading(true);
      setError(false);
      setApiKey(null);
      setIsRandomKeyRateLimited(false);
      setFallbackApiKey(undefined);
      setErrorMessage(errorMessages[Math.floor(Math.random() * errorMessages.length)]);

      // Simplified key logic - Removed Rate Limit Logic
      const endpoint = selectedKeyType === "Random"
        ? "/API/GetRandomKey"
        : `/API/GetRandomKey?type=${selectedKeyType}`;

      const response = await fetchWithRateLimit<ApiKey>(endpoint, {
        requestId: "randomKey",
      });

      if (response.cancelled) return;

      if (response.data) {
        setApiKey(response.data);
        setLoading(false);
        analytics.trackApiKeyView(response.data.apiType, response.data.id);
      } else {
        setLoading(false);
        setError(true);
      }
    };

    fetchRandomKey();

    return () => {
      cancelRequests("randomKey");
    };
  }, [selectedKeyType, refreshTrigger]);

  // Fetch Key Statistics
  useEffect(() => {
    let interval: NodeJS.Timeout | null = null;
    let isVisible = true;
    let errorCount = 0;
    let baseDelay = 30000;

    const fetchKeyStats = async () => {
      const response = await fetchWithRateLimit<KeyStats>(
        "/API/GetKeyStatistics",
        {
          requestId: "keyStats",
        }
      );

      if (response.cancelled) return;

      if (response.data) {
        setKeyStats(response.data);
        errorCount = 0;
      }

      if (response.error && !response.cancelled) {
        console.error("Failed to fetch key statistics:", response.error);
        errorCount++;

        const delay = Math.min(baseDelay * Math.pow(2, errorCount - 1), 300000);
        if (interval) {
          clearInterval(interval);
          interval = setInterval(fetchKeyStats, delay);
        }
      }
    };

    const handleVisibilityChange = () => {
      isVisible = !document.hidden;

      if (isVisible && !interval) {
        fetchKeyStats();
        interval = setInterval(fetchKeyStats, baseDelay);
      } else if (!isVisible && interval) {
        clearInterval(interval);
        interval = null;
      }
    };

    fetchKeyStats();

    if (!document.hidden) {
      interval = setInterval(fetchKeyStats, baseDelay);
    }

    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      if (interval) clearInterval(interval);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      cancelRequests("keyStats");
    };
  }, []);

  const handleCloseDrawer = () => {
    Cookies.set("hasSeenDisclaimer", "true", { expires: 30 });
    setShowDrawer(false);
  };

  const handleIssueSubmission = async (apiKey: ApiKey) => {
    // Check if references exist before proceeding
    if (!apiKey.references || apiKey.references.length === 0 || !apiKey.references[0]) {
      console.error("Cannot submit issue: No references available for this API key");
      return;
    }

    analytics.trackIssueSubmission(apiKey.apiType, apiKey.references[0].repoURL);

    try {
      const response = await fetchWithRateLimit("/API/TrackIssueSubmission", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          apiKeyId: apiKey.id,
          apiType: apiKey.apiType,
          repoUrl: apiKey.references[0].repoURL,
          timestamp: new Date().toISOString(),
        }),
        requestId: "issueSubmission",
      });

      if (response.error && !response.cancelled) {
        console.error("Failed to track issue submission:", response.error);
      }
    } catch (error) {
      console.error("Failed to track issue submission:", error);
    }

    // Enhanced issue template with more sass
    const issueUrl = `${apiKey.references[0].repoURL}/issues/new?title=${encodeURIComponent(`🚨 URGENT: Your ${apiKey.apiType} key is having a public meltdown! 🎭`)}&body=${encodeURIComponent(
      `Dear Repository Owner,
  
  🎉 CONGRATULATIONS! 🎉 
  
  You've just won our prestigious "Most Public Secret of the Day" award! Your ${apiKey.apiType} API key has achieved internet fame and is currently trending on the "Things That Should Be Private But Aren't" charts!
  
  ## 📊 The Damage Report
  
  - **The Star of the Show**: Your ${apiKey.apiType} key (ending in ...${apiKey.apiKey.slice(-4)})
  - **Current Status**: Living its best life in public (YOLO! 💃)
  - **Privacy Level**: As private as a Times Square billboard on New Year's Eve
  - **Exposure Count**: ${apiKey.timesDisplayed.toLocaleString()} people have already admired your key
  - **Potential Cost**: Somewhere between "there goes my coffee budget" and "I need to fake my own death"
  
  ## 🕵️ The Mystery Location
  
  I could tell you EXACTLY where this key is hiding in your repo, but where's the fun in that? Think of it as an educational treasure hunt! Here's a hint: it's in one of your files. You're welcome! 😉
  
  ## 🚀 Your Action Plan (If You Like Having Money)
  
  1. **PANIC** (Get it out of your system - 30 seconds max)
  2. **Revoke this key** faster than you can say "unauthorized charges"
  3. **Check your billing** (Spoiler: You might need tissues)
  4. **Generate a new key** (and maybe don't commit it this time?)
  5. **Learn about .env files** (They're like secret diaries for code!)
  6. **Update your resume** (Just in case... 😬)
  
  ## 🎓 Free Life Lessons Included!
  
  - **Lesson 1**: Git never forgets (like that embarrassing photo from 2009)
  - **Lesson 2**: Public repos are PUBLIC (shocking, we know!)
  - **Lesson 3**: .gitignore is your friend (unlike your API key right now)
  - **Lesson 4**: Environment variables exist (mind = blown 🤯)
  
  ## 💡 Pro Tips from Someone Who Cares
  
  - Use secret management tools (they're like password managers but cooler)
  - Never hardcode secrets (unless you enjoy surprise bankruptcy)
  - Always use environment variables (seriously, it's 2024!)
  - Consider this a learning opportunity (glass half full, right?)
  
  ## 🎪 The Silver Lining
  
  Hey, at least you're not alone! You're joining an elite club of developers who've accidentally funded someone else's crypto mining operation. There are dozens of us! DOZENS!
  
  Stay strong, and remember: This too shall pass (but your Git history is forever).
  
  Sincerely,
  Your Friendly Neighborhood Secret Exposer 🦸‍♂️
  
  P.S. If you need the exact location, I charge $999.99 for consulting (or you could just search for "${apiKey.apiKey.slice(0, 10)}..." in your repo)
  P.P.S. Don't forget to thank me in your commit message when you fix this! 
  P.P.P.S. We accept donations at unsecuredapikeys.com (server hamsters need food) 🐹`,
    )}`;

    window.open(issueUrl, "_blank");
  };

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

    if (seconds < 5) return "just now (RIP)";

    for (const [unit, secondsInUnit] of Object.entries(intervals)) {
      const interval = Math.floor(seconds / secondsInUnit);

      if (interval >= 1) {
        return interval === 1 ? `1 ${unit} ago` : `${interval} ${unit}s ago`;
      }
    }

    return "just now";
  };

  const dropdownItems = useMemo(() => {
    const items = [
      { key: "Random", label: "Random (Surprise me! 🎲)" }
    ];

    apiKeyTypes.forEach((type) => {
      items.push({
        key: type.apiTypeId.toString(),
        label: `${type.apiType} (${type.keyCount.toLocaleString()} victims)`
      });
    });

    return items;
  }, [apiKeyTypes]);

  const selectedKeyName = useMemo(() => {
    if (selectedKeyType === "Random") {
      return "Random (Chaos Mode)";
    }
    const selectedType = apiKeyTypes.find(
      (type) => type.apiTypeId == selectedKeyType,  // Using == for type coercion
    );

    return selectedType
      ? `${selectedType.apiType} (${selectedType.keyCount.toLocaleString()} exposed)`
      : "Random (Chaos Mode)";  // Default to Random instead of "Select Type"
  }, [selectedKeyType, apiKeyTypes]);

  const handleSelectionChange = (key: React.Key | undefined) => {
    const newKeyType = (key === undefined ? "Random" : key) as string | number;

    const selectedTypeName = newKeyType === "Random" ? "Random" :
      apiKeyTypes.find(type => type.apiTypeId === newKeyType)?.apiType || "Unknown";
    analytics.trackApiTypeSelection(selectedTypeName);

    if (newKeyType === selectedKeyType) {
      setRefreshTrigger((prev) => prev + 1);
      analytics.trackButtonClick("refresh_api_key", "dropdown");
    } else {
      setSelectedKeyType(newKeyType);
    }
  };

  return (
    <>
      <style jsx global>{`
        /* Enhanced Animations */
        @keyframes float {
          0%, 100% { transform: translateY(0); }
          50% { transform: translateY(-10px); }
        }
        
        @keyframes pulse-glow {
          0%, 100% { box-shadow: 0 0 20px rgba(139, 92, 246, 0.5); }
          50% { box-shadow: 0 0 40px rgba(139, 92, 246, 0.8); }
        }
        
        @keyframes pulse-glow-green {
          0%, 100% { box-shadow: 0 0 20px rgba(34, 197, 94, 0.5); }
          50% { box-shadow: 0 0 40px rgba(34, 197, 94, 0.8); }
        }
        
        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          25% { transform: translateX(-5px); }
          75% { transform: translateX(5px); }
        }
        
        @keyframes gradient-shift {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }
        
        @keyframes glitch {
          0%, 100% { transform: translate(0); }
          20% { transform: translate(-2px, 2px); }
          40% { transform: translate(-2px, -2px); }
          60% { transform: translate(2px, 2px); }
          80% { transform: translate(2px, -2px); }
        }
        
        @keyframes shimmer {
          0% { background-position: -200% 0; }
          100% { background-position: 200% 0; }
        }
        
        @keyframes fade-in {
          from { opacity: 0; transform: translateY(10px); }
          to { opacity: 1; transform: translateY(0); }
        }
        
        /* Enhanced Classes */
        .animate-float {
          animation: float 6s ease-in-out infinite;
        }
        
        .animate-pulse-glow {
          animation: pulse-glow 2s ease-in-out infinite;
        }
        
        .animate-pulse-glow-green {
          animation: pulse-glow-green 2s ease-in-out infinite;
        }
        
        .btn-shake:hover {
          animation: shake 0.5s ease-in-out;
        }
        
        .text-gradient-animate {
          background: linear-gradient(90deg, #667eea 0%, #764ba2 50%, #667eea 100%);
          background-size: 200% auto;
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          background-clip: text;
          animation: gradient-shift 3s ease infinite;
        }
        
        .text-glitch:hover {
          animation: glitch 0.3s cubic-bezier(.25,.46,.45,.94) both;
        }
        
        .animate-shimmer {
          animation: shimmer 2s linear infinite;
        }
        
        .animate-fade-in {
          animation: fade-in 0.5s ease-out;
        }
        
        .loading-dots::after {
          content: "";
          animation: loading-dots 1.5s infinite;
        }
        
        @keyframes loading-dots {
          0% { content: ""; }
          25% { content: "."; }
          50% { content: ".."; }
          75% { content: "..."; }
        }
        
        /* Card Effects */
        .card-glass {
          background: rgba(255, 255, 255, 0.05);
          backdrop-filter: blur(10px);
          border: 1px solid rgba(255, 255, 255, 0.1);
          box-shadow: 0 8px 32px 0 rgba(31, 38, 135, 0.37);
        }

        /* Counter specific styling */
        .counter-glow {
          background: rgba(139, 92, 246, 0.1);
          border: 1px solid rgba(139, 92, 246, 0.2);
          box-shadow: 0 0 40px rgba(139, 92, 246, 0.4);
          border-radius: 12px;
          display: inline-block;
          padding: 2px;
        }
        
        .active-user-glow {
          background: rgba(34, 197, 94, 0.1);
          border: 1px solid rgba(34, 197, 94, 0.2);
          box-shadow: 0 0 40px rgba(34, 197, 94, 0.4);
          border-radius: 12px;
          display: inline-block;
          padding: 2px;
        }
        
        .card-elegant {
          background: linear-gradient(135deg, rgba(255, 255, 255, 0.1) 0%, rgba(255, 255, 255, 0) 100%);
          backdrop-filter: blur(10px);
          -webkit-backdrop-filter: blur(10px);
          border: 1px solid rgba(255, 255, 255, 0.18);
        }
        
        .card-spacing {
          padding: 2rem;
        }
        
        /* Hover Effects */
        .hover-lift {
          transition: transform 0.3s ease, box-shadow 0.3s ease;
        }
        
        .hover-lift:hover {
          transform: translateY(-2px);
          box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
        }
        
        .hover-lift-lg {
          transition: all 0.3s ease;
        }
        
        .hover-lift-lg:hover {
          transform: translateY(-8px) scale(1.02);
          box-shadow: 0 20px 40px -5px rgba(0, 0, 0, 0.2);
        }
        
        /* Gradient Borders */
        .gradient-border {
          position: relative;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          padding: 2px;
        }
        
        .gradient-border > * {
          background: #0a0a0a;
          border-radius: inherit;
        }
        
        /* Button Glow */
        .btn-glow {
          box-shadow: 0 0 20px rgba(236, 72, 153, 0.5);
          transition: all 0.3s ease;
        }
        
        .btn-glow:hover {
          box-shadow: 0 0 30px rgba(236, 72, 153, 0.8);
        }
        
        /* API Type Glows */
        .glow-openai {
          box-shadow: 0 0 30px rgba(116, 170, 156, 0.4);
        }
        
        .glow-anthropic {
          box-shadow: 0 0 30px rgba(84, 54, 218, 0.4);
        }
        
        .glow-google {
          box-shadow: 0 0 30px rgba(66, 133, 244, 0.4);
        }
        
        .glow-cohere {
          box-shadow: 0 0 30px rgba(249, 179, 172, 0.4);
        }
        
        .glow-huggingface {
          box-shadow: 0 0 30px rgba(255, 210, 30, 0.4);
        }
        
        .glow-stability {
          box-shadow: 0 0 30px rgba(255, 43, 194, 0.4);
        }
        
        .glow-mistral {
          box-shadow: 0 0 30px rgba(222, 72, 21, 0.4);
        }
        
        .glow-together {
          box-shadow: 0 0 30px rgba(91, 68, 243, 0.4);
        }
        
        .glow-openrouter {
          box-shadow: 0 0 30px rgba(59, 130, 246, 0.4);
        }
        
        /* Text Shadow */
        .text-shadow-lg {
          text-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
        }
        
        /* Space utilities */
        .space-y-relaxed > * + * {
          margin-top: 1.5rem;
        }
        
        .section-spacing {
          padding: 2rem 1rem 4rem 1rem;
        }
        
        /* Matrix Background */
        .matrix-bg {
          position: fixed;
          top: 0;
          left: 0;
          width: 100%;
          height: 100%;
          z-index: -1;
          opacity: 0.02;
          background-image: repeating-linear-gradient(
            0deg,
            transparent,
            transparent 2px,
            rgba(139, 92, 246, 0.1) 2px,
            rgba(139, 92, 246, 0.1) 4px
          );
          pointer-events: none;
        }
        
        /* Floating Counter Styles */
        .floating-counter-left {
          position: fixed;
          left: 2rem;
          top: 50%;
          transform: translateY(-50%) scale(0.75);
          transform-origin: left center;
          z-index: 40;
          transition: all 0.3s ease;
        }
        
        .floating-counter-right {
          position: fixed;
          right: 2rem;
          top: 50%;
          transform: translateY(-50%) scale(0.75);
          transform-origin: right center;
          z-index: 40;
          transition: all 0.3s ease;
        }
        
        /* Hide on mobile and tablet */
        @media (max-width: 1024px) {
          .floating-counter-left,
          .floating-counter-right {
            display: none;
          }
        }
        
        /* Adjust position on medium screens */
        @media (max-width: 1280px) {
          .floating-counter-left {
            left: 1rem;
            transform: translateY(-50%) scale(0.7);
          }
          
          .floating-counter-right {
            right: 1rem;
            transform: translateY(-50%) scale(0.7);
          }
        }
        
        /* Hover effects for floating counters */
        .floating-counter-left:hover {
          transform: translateY(-50%) scale(0.8);
        }
        
        .floating-counter-right:hover {
          transform: translateY(-50%) scale(0.8);
        }
        
        /* Vertical orientation for counters */
        .counter-vertical {
          writing-mode: vertical-rl;
          text-orientation: mixed;
        }
        
        /* Reduced Motion Support */
        @media (prefers-reduced-motion: reduce) {
          *, *::before, *::after {
            animation-duration: 0.01ms !important;
            animation-iteration-count: 1 !important;
            transition-duration: 0.01ms !important;
          }
        }
      `}</style>

      <Head>
        <title>Unsecured API Keys - The Wall of "My Boss Hasn't Found Out Yet™"</title>
        <meta
          name="description"
          content="Where your private keys go to become public celebrities! Help developers avoid that awkward $50,000 LLM bill. Find and report exposed API keys before the hackers do."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="API keys, security, GitHub, exposed secrets, vulnerability, cybersecurity, code security, developer tools, LLM bills, career endings" />
        <link rel="canonical" href="https://unsecuredapikeys.com/" />

        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Unsecured API Keys" />
        <meta property="og:title" content="Unsecured API Keys - The Wall of 'My Boss Hasn't Found Out Yet™'" />
        <meta property="og:description" content="Where your private keys go to become public celebrities! Help developers avoid that awkward $50,000 LLM bill." />
        <meta property="og:url" content="https://unsecuredapikeys.com/" />
        <meta property="og:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:image:alt" content="Unsecured API Keys - Security Tool for Developers Who Like Their Jobs" />
        <meta property="og:locale" content="en_US" />

        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Unsecured API Keys - The Wall of 'My Boss Hasn't Found Out Yet™'" />
        <meta name="twitter:description" content="Where your private keys go to become public celebrities! Help developers avoid that awkward $50,000 LLM bill." />
        <meta name="twitter:image" content="https://unsecuredapikeys.com/og-image.png" />
        <meta name="twitter:image:alt" content="Unsecured API Keys - Security Tool for Developers Who Like Their Jobs" />

        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        {/* Matrix-style background effect */}
        <div className="matrix-bg" />

        {/* Floating counters stacked on left side */}
        <div className="floating-counter-left">
          <div className="flex flex-col gap-2">
            <div className="counter-glow animate-pulse-glow">
              <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                <TotalDisplayCounter />
              </Suspense>
            </div>

            <div className="active-user-glow animate-pulse-glow-green">
              <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                <ActiveUserCounter />
              </Suspense>
            </div>
          </div>
        </div>

        {showDrawer !== null && showDrawer && (
          <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
            <Disclaimer isOpen={showDrawer} onClose={handleCloseDrawer} />
          </Suspense>
        )}

        <section className="flex flex-col items-center justify-center section-spacing">
          <div className="inline-block max-w-4xl text-center justify-center">
            <h1 className="m-0">
              <span className={title({ size: "lg" }) + " text-gradient-animate"}>Unsecured&nbsp;</span>
              <span className={title({ color: "violet", size: "lg" }) + " text-glitch"}>API Keys&nbsp;</span>
            </h1>

            {/* Enhanced Hero Buttons */}
            {/* Enhanced Hero Buttons */}
            <div className="mt-8 animate-float flex flex-col sm:flex-row gap-4 items-center justify-center">
              <Button
                className="bg-gradient-to-r from-green-500 to-emerald-600 text-white font-bold shadow-lg hover:shadow-xl transition-all duration-300 btn-glow min-w-[200px]"
                size="lg"
                onPress={handleRunPipeline}
                isLoading={pipelineRunning}
              >
                {pipelineStatus || "🔥 Run Pipeline Now"}
              </Button>
            </div>
            <p className="text-default-400 mt-4 italic text-sm">(Directly triggers Scraper & Verifier Bots)</p>
          </div>

          {/* Rate Limit Banner Removed */}

          {/* Enhanced API Key Type Dropdown */}
          <div className="flex justify-center w-full max-w-xs mt-4 mb-8">
            <Dropdown>
              <DropdownTrigger>
                <Button
                  className="min-w-[200px] gradient-border hover-lift-lg"
                  variant="bordered"
                >
                  {selectedKeyName}
                </Button>
              </DropdownTrigger>
              <DropdownMenu
                aria-label="Select API Key Type"
                selectedKeys={new Set([selectedKeyType.toString()])}
                selectionMode="single"
                items={dropdownItems}
                onSelectionChange={(keys) =>
                  handleSelectionChange(
                    keys instanceof Set ? Array.from(keys)[0] : undefined,
                  )
                }
              >
                {(item) => (
                  <DropdownItem key={item.key}>
                    {item.label}
                  </DropdownItem>
                )}
              </DropdownMenu>
            </Dropdown>
          </div>

          {loading ? (
            <div className="w-full max-w-3xl mt-8 p-12 card-glass text-center animate-fade-in">
              <div className="animate-pulse flex flex-col items-center space-y-6">
                <div className="h-8 w-72 bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700 bg-[length:200%_100%] animate-shimmer rounded-lg" />
                <div className="h-14 w-full bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700 bg-[length:200%_100%] animate-shimmer rounded-lg" />
                <div className="space-y-3 w-full">
                  <div className="h-4 w-3/4 bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700 bg-[length:200%_100%] animate-shimmer rounded" />
                  <div className="h-4 w-1/2 bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700 bg-[length:200%_100%] animate-shimmer rounded" />
                </div>
              </div>
              <div className="mt-8 flex items-center justify-center space-x-3">
                <div className="w-3 h-3 bg-primary rounded-full animate-bounce [animation-delay:-0.3s]"></div>
                <div className="w-3 h-3 bg-primary rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                <div className="w-3 h-3 bg-primary rounded-full animate-bounce"></div>
              </div>
              <p className="mt-4 text-primary font-medium text-lg loading-dots">
                {loadingMessage}
              </p>
            </div>
          ) : error ? (
            <div className="w-full max-w-3xl mt-8 p-12 card-elegant border-danger/20 text-center animate-fade-in">
              <div className="animate-float">
                <svg
                  className="h-20 w-20 mx-auto text-danger mb-6"
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
              </div>

              <h3 className="text-2xl font-bold mb-4 text-danger text-glitch">
                {isRandomKeyRateLimited
                  ? "Whoa There, Speed Demon! 🏎️"
                  : "Critical System Failure (aka Tuesday)"}
              </h3>

              {isRandomKeyRateLimited ? (
                <div className="space-y-4">
                  <RateLimitInfo
                    fallbackApiKey={fallbackApiKey}
                    isRateLimited={isRandomKeyRateLimited}
                    rateLimitInfo={randomKeyRateLimit}
                  />
                  <p className="text-default-600 font-medium">
                    Even exposed keys need coffee breaks! ☕
                  </p>
                  <p className="text-sm text-default-400 italic">
                    Or donate to unlock the "I Have No Patience" tier
                  </p>
                </div>
              ) : (
                <div className="space-y-3">
                  <p className="text-default-600 font-medium">
                    {errorMessage}
                  </p>

                  <p className="text-sm italic text-default-500">
                    Error Code: ID-10-T (It's probably your fault somehow)
                  </p>
                </div>
              )}

              <Button
                className="mt-8 btn-shake"
                color="danger"
                radius="full"
                size="lg"
                variant="shadow"
                onPress={() => window.location.reload()}
              >
                Try Your Luck Again 🎰
              </Button>
            </div>
          ) : (
            apiKey && (
              <div className="w-full max-w-4xl mt-8">
                <div className={`card-spacing rounded-lg hover-lift-lg transition-all duration-300 ${apiTypeColors[apiKey.apiType]?.border || ""} ${apiTypeColors[apiKey.apiType]?.glow || ""} border-2 card-elegant`}>
                  <div className="flex items-center justify-between mb-6">
                    <h3 className="text-2xl font-bold flex items-center gap-3">
                      <span className="text-shadow-lg text-gradient-animate">💀 Exposed Key Alert! 💀</span>
                      <span
                        className={`px-3 py-1.5 text-sm font-medium rounded-full ${apiTypeColors[apiKey.apiType]?.bg || "bg-primary/10"} ${apiTypeColors[apiKey.apiType]?.text || "text-primary"} ${apiTypeColors[apiKey.apiType]?.border || ""} border animate-pulse`}
                      >
                        {apiKey.apiType}
                      </span>
                    </h3>
                    <div className="text-sm text-default-500 flex items-center gap-2 bg-default-100 dark:bg-default-50 px-3 py-1.5 rounded-full animate-float">
                      <svg
                        className="h-4 w-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                        />
                        <path
                          d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                        />
                      </svg>
                      <span className="font-medium animate-pulse">{apiKey.timesDisplayed.toLocaleString()}</span> eyes on this key
                    </div>
                  </div>

                  {/* Rate limit info */}
                  {randomKeyRateLimit && (
                    <div className="mb-6">
                      <RateLimitInfo
                        fallbackApiKey={fallbackApiKey}
                        isRateLimited={isRandomKeyRateLimited}
                        rateLimitInfo={randomKeyRateLimit}
                      />
                    </div>
                  )}

                  <div className="w-full mb-8">
                    <div className="relative group">
                      {/* Gradient border effect */}
                      <div className="absolute -inset-0.5 bg-gradient-to-r from-purple-600 to-pink-600 rounded-lg blur opacity-30 group-hover:opacity-50 transition duration-300"></div>

                      {/* Main key container */}
                      <div className="relative bg-gradient-to-br from-purple-900/20 via-purple-800/10 to-pink-900/20 backdrop-blur-sm rounded-lg py-3 px-4 border border-purple-500/20 hover:border-purple-400/30 transition-all duration-300">
                        <div className="flex items-center justify-between gap-3">
                          {/* Key display */}
                          <div className="flex-1 overflow-hidden">
                            <code className="block text-sm font-mono text-purple-200 dark:text-purple-100 break-all whitespace-normal select-all hover:text-purple-50 dark:hover:text-purple-50 transition-colors duration-200">
                              {apiKey.apiKey}
                            </code>
                          </div>

                          {/* Copy button */}
                          <button
                            onClick={() => {
                              navigator.clipboard.writeText(apiKey.apiKey);
                              setCopySuccess(true);
                              setTimeout(() => setCopySuccess(false), 2000);
                            }}
                            className="flex-shrink-0 p-2 rounded-md bg-purple-600/20 hover:bg-purple-600/30 border border-purple-500/30 hover:border-purple-400/50 transition-all duration-200 group/copy relative"
                            title="Copy to clipboard"
                          >
                            {copySuccess ? (
                              <svg
                                className="w-5 h-5 text-green-400 transition-colors"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                              >
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  strokeWidth={2}
                                  d="M5 13l4 4L19 7"
                                />
                              </svg>
                            ) : (
                              <svg
                                className="w-5 h-5 text-purple-300 group-hover/copy:text-white transition-colors"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                              >
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  strokeWidth={2}
                                  d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                                />
                              </svg>
                            )}
                          </button>
                        </div>
                      </div>

                      {/* Bottom accent */}
                      <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 w-3/4 h-px bg-gradient-to-r from-transparent via-purple-400/50 to-transparent"></div>
                    </div>
                  </div>

                  <div className="space-y-relaxed">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-4">
                      <div className="space-y-3">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-default-700">Status:</span>
                          <span
                            className={`px-2 py-0.5 rounded-full text-sm font-medium ${apiKey.status === "Valid"
                                ? "bg-success/10 text-success border border-success/20 animate-pulse"
                                : "bg-danger/10 text-danger border border-danger/20"
                              }`}
                          >
                            {apiKey.status === "Valid" ? "Still Working! 💸" : "Someone beat you to it!"}
                          </span>
                          {apiKey.status === "Valid" && (
                            <span className="text-xs text-default-400">
                              (your wallet isn't safe yet)
                            </span>
                          )}
                        </div>

                        {apiKey.status === "Valid" && (apiKey.lastCheckedUTC || apiKey.lastValidUTC) && (
                          <div>
                            <span className="font-semibold text-default-700">Last Verified:</span>{" "}
                            <span className="text-default-600 font-medium">
                              {formatTimeAgo(apiKey.lastCheckedUTC || apiKey.lastValidUTC)} ✅
                            </span>
                          </div>
                        )}

                        <div>
                          <span className="font-semibold text-default-700">First Exposed:</span>{" "}
                          <span className="text-default-600">
                            {new Date(apiKey.firstFoundUTC).toLocaleString()}
                          </span>
                        </div>

                        <div>
                          <span className="font-semibold text-default-700">Surviving in the wild:</span>{" "}
                          <span className="text-default-600 font-medium">
                            {formatTimeAgo(apiKey.firstFoundUTC).replace(" ago", "")} 🏃‍♂️
                          </span>
                        </div>
                      </div>

                      {apiKey.references && apiKey.references.length > 0 && apiKey.references[0] && (
                        <div className="space-y-3">
                          <div>
                            <span className="font-semibold text-default-700">Crime Scene:</span>
                            <Link
                              className="ml-2 font-medium hover-lift"
                              href={apiKey.references[0].repoURL}
                              isExternal
                              showAnchorIcon
                            >
                              {apiKey.references[0].repoURL.split("/").slice(-1)[0] || "Mystery Repo"}
                            </Link>
                          </div>

                          <div>
                            <span className="font-semibold text-default-700">The Culprit:</span>
                            <Link
                              className="ml-2 font-medium hover-lift"
                              href={`https://github.com/${apiKey.references[0].repoOwner}`}
                              isExternal
                            >
                              @{apiKey.references[0].repoOwner} 🎭
                            </Link>
                          </div>

                          <div>
                            <span className="font-semibold text-default-700 block mb-1">Evidence Location:</span>
                            <Link
                              href={apiKey.references[0].fileURL}
                              isExternal
                              showAnchorIcon
                              className="hover-lift block"
                            >
                              <Code className="text-sm break-all whitespace-normal max-w-full overflow-hidden">
                                {apiKey.references[0].filePath}
                              </Code>
                            </Link>
                          </div>
                        </div>
                      )}
                    </div>

                    {apiKey.references && apiKey.references.length > 0 && apiKey.references[0] && apiKey.references[0].codeContext && (
                      <div className="pt-4 border-t border-default-200 dark:border-default-100">
                        <span className="font-semibold text-default-700 block mb-2">The Smoking Gun 🔫</span>
                        <Code className="block p-3 text-sm bg-default-100 dark:bg-default-50 overflow-x-auto break-all whitespace-pre-wrap max-w-full">
                          {apiKey.references[0].codeContext.length > 150
                            ? `${apiKey.references[0].codeContext.substring(0, 150)}... (redacted for your safety)`
                            : apiKey.references[0].codeContext}
                        </Code>
                        <p className="mt-2 text-sm text-default-400">
                          Line {apiKey.references[0].lineNumber} (the scene of the crime)
                        </p>
                      </div>
                    )}

                    {/* Show message for keys without references */}
                    {(!apiKey.references || apiKey.references.length === 0) && (
                      <div className="pt-4 border-t border-default-200 dark:border-default-100">
                        <div className="bg-warning/10 rounded-lg p-4 border border-warning/20">
                          <p className="text-warning font-semibold mb-2">⚠️ Limited Information Available</p>
                          <p className="text-sm text-default-600">
                            This key doesn't have repository information available. It may have been found without source context.
                          </p>
                          <p className="text-xs text-default-500 mt-2 italic">
                            The key is still valid and exploitable, but we can't show you where it came from! 🕵️
                          </p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )
          )}

          {apiKey && (
            <div className="flex flex-wrap gap-4 mt-10 justify-center animate-fade-in">
              <Button
                className="min-w-[180px] btn-shake btn-glow"
                color="primary"
                isDisabled={
                  !apiKey || !apiKey.references || apiKey.references.length === 0
                }
                radius="full"
                size="lg"
                startContent={<GithubIcon size={20} />}
                variant="shadow"
                onPress={() => apiKey && handleIssueSubmission(apiKey)}
              >
                Be a Hero 🦸‍♂️
              </Button>

              <Button
                as={Link}
                className="min-w-[180px] hover-lift"
                href={apiKey.references && apiKey.references.length > 0 && apiKey.references[0] ? apiKey.references[0].repoURL : "#"}
                isDisabled={!apiKey.references || apiKey.references.length === 0}
                isExternal
                radius="full"
                size="lg"
                startContent={<GithubIcon size={20} />}
                variant="bordered"
              >
                Visit the Crime Scene 🔍
              </Button>
            </div>
          )}

          {/* Stats Only - Cleaned up */}
          <section className="mt-20 mb-12 w-full max-w-5xl animate-fade-in">
            <h2 className="sr-only">API Key Statistics</h2>
            <div className="space-y-8">
              <div className="text-center">
                <div className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-primary/10 to-secondary/10 rounded-full border border-primary/20 animate-float">
                  <span className="text-lg">🔐</span>
                  <span className="font-medium text-default-700">
                    Making bad developers famous since 2024™
                  </span>
                  <span className="text-lg">😈</span>
                </div>
              </div>

              {keyStats && (
                <div className="card-glass p-8 hover-lift-lg">
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
                    <div className="text-center space-y-2">
                      {/* Stats content remains same */}
                      <div className="bg-primary/10 rounded-2xl p-4 inline-block hover:scale-110 transition-transform">
                        <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                          <AnimatedNumber
                            className="font-bold text-primary text-3xl"
                            value={keyStats.totalNumberOfKeys.toLocaleString()}
                          />
                        </Suspense>
                      </div>
                      <p className="text-default-600 font-medium">Career Enders</p>
                    </div>

                    <div className="text-center space-y-2">
                      <div className="bg-success/10 rounded-2xl p-4 inline-block hover:scale-110 transition-transform">
                        <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                          <AnimatedNumber
                            className="font-bold text-success text-3xl"
                            value={keyStats.numberOfValidKeys.toLocaleString()}
                          />
                        </Suspense>
                      </div>
                      <p className="text-default-600 font-medium">Still Exploitable</p>
                    </div>

                    <div className="text-center space-y-2">
                      <div className="bg-warning/10 rounded-2xl p-4 inline-block hover:scale-110 transition-transform">
                        <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                          <AnimatedNumber
                            className="font-bold text-warning text-3xl"
                            value={keyStats.newKeysFoundToday.toLocaleString()}
                          />
                        </Suspense>
                      </div>
                      <p className="text-default-600 font-medium">Fresh Victims Today</p>
                    </div>

                    <div className="text-center space-y-2">
                      <div className="bg-danger/10 rounded-2xl p-4 inline-block hover:scale-110 transition-transform">
                        <Suspense fallback={<div className="animate-pulse">Loading...</div>}>
                          <AnimatedNumber
                            className="font-bold text-danger text-3xl"
                            value={keyStats.mostRecentFind}
                          />
                        </Suspense>
                      </div>
                      <p className="text-default-600 font-medium">Latest Oopsie</p>
                    </div>
                  </div>

                  <div className="text-center mt-8">
                    <p className="text-sm text-default-400 italic animate-pulse">
                      * Valid Keys: Verified by our team of highly caffeinated bots 🤖
                    </p>
                  </div>
                </div>
              )}
            </div>
          </section>
        </section>
      </DefaultLayout>
    </>
  );
}
