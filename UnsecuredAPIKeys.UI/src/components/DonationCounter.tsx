import { useState, useEffect } from "react";
import { fetchWithRateLimit } from "@/utils/api";
import AnimatedNumber from "./AnimatedNumber";

interface DonationStats {
  totalClicks: number;
  totalDonations: number;
  totalDonationAmount: number;
  clicksToday: number;
  uniqueClickersToday: number;
}

interface DonationCounterProps {
  className?: string;
}

export default function DonationCounter({ className = "" }: DonationCounterProps) {
  const [stats, setStats] = useState<DonationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [recentDonation, setRecentDonation] = useState<{
    amount: number;
    currency: string;
    transactionId: string;
    confirmedAt: string;
  } | null>(null);

  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | null = null;
    let isVisible = true;

    const fetchStats = async () => {
      try {
        const response = await fetchWithRateLimit<DonationStats>("/API/GetDonationStats", {
          requestId: "donationStats",
        });

        if (response.cancelled) return;

        if (response.data) {
          setStats(response.data);
          setError(false);
        } else if (response.error) {
          setError(true);
          console.error("Failed to fetch donation stats:", response.error);
        }
      } catch (err) {
        setError(true);
        console.error("Error fetching donation stats:", err);
      } finally {
        setLoading(false);
      }
    };

    const handleVisibilityChange = () => {
      isVisible = !document.hidden;
      
      if (isVisible && !interval) {
        fetchStats();
        interval = setInterval(fetchStats, 30000); // Update every 30 seconds
      } else if (!isVisible && interval) {
        clearInterval(interval);
        interval = null;
      }
    };

    // SignalR connection setup
    const setupSignalR = () => {
      if (typeof window !== "undefined" && (window as any).signalRConnection) {
        const connection = (window as any).signalRConnection;
        
        // Listen for donation updates
        connection.on("DonationUpdated", (data: {
          totalDonations: number;
          totalAmount: number;
          recentDonation?: {
            amount: number;
            currency: string;
            transactionId: string;
            confirmedAt: string;
          };
        }) => {
          console.log("Received donation update:", data);
          
          // Update stats with new totals
          setStats(prevStats => {
            if (!prevStats) return null;
            return {
              ...prevStats,
              totalDonations: data.totalDonations,
              totalDonationAmount: data.totalAmount
            };
          });
          
          // Show recent donation notification
          if (data.recentDonation) {
            setRecentDonation(data.recentDonation);
            
            // Clear notification after 5 seconds
            setTimeout(() => {
              setRecentDonation(null);
            }, 5000);
          }
        });
      }
    };

    // Initial fetch
    fetchStats();

    // Set up interval if page is visible
    if (!document.hidden) {
      interval = setInterval(fetchStats, 30000);
    }

    // Setup SignalR
    setupSignalR();

    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      if (interval) clearInterval(interval);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
    };
  }, []);

  if (loading) {
    return (
      <div className={`animate-pulse ${className}`}>
        <div className="h-8 w-48 bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700 bg-[length:200%_100%] animate-shimmer rounded-lg" />
      </div>
    );
  }

  if (error || !stats) {
    return (
      <div className={`text-danger ${className}`}>
        <span className="text-sm">💸 Donation tracker offline (probably broke from lack of donations)</span>
      </div>
    );
  }

  return (
    <div className={`text-center ${className}`}>
      <div className="space-y-2">
        <div className="text-lg font-bold text-default-700">
          <AnimatedNumber
            value={stats.totalClicks.toLocaleString()}
            className="text-pink-600 dark:text-pink-400"
          />
          <span className="text-default-600 ml-2">
            times developers have clicked &quot;donate&quot; 
          </span>
        </div>
        
        <div className="text-lg font-bold text-default-700">
          <span className="text-default-600">and only </span>
          <AnimatedNumber
            value={`$${stats.totalDonationAmount.toFixed(2)}`}
            className="text-green-600 dark:text-green-400"
          />
          <span className="text-default-600 ml-2">
            has been donated by those who&apos;ve abused the keys! 
          </span>
        </div>

        <div className="text-sm text-default-500 italic mt-2">
          ({stats.totalDonations} confirmed donations • {stats.clicksToday} clicks today • {stats.uniqueClickersToday} unique cheapskates)
        </div>
        
        {/* Real-time donation notification */}
        {recentDonation && (
          <div className="mt-4 p-3 bg-green-100 dark:bg-green-900/20 border border-green-300 dark:border-green-700 rounded-lg animate-pulse">
            <div className="text-sm text-green-800 dark:text-green-200">
              🎉 Holy crap! Someone actually donated ${recentDonation.amount} {recentDonation.currency}!
            </div>
            <div className="text-xs text-green-600 dark:text-green-400 mt-1">
              Transaction: {recentDonation.transactionId}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
