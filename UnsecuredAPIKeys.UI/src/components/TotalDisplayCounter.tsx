import { useState, useEffect, useRef } from "react";
import { Card } from "@heroui/card";
import { fetchWithRateLimit } from "@/utils/api";
import AnimatedNumber from "./AnimatedNumber";
import { signalRService } from "@/utils/signalrService";

export default function TotalDisplayCounter() {
  const [displayCount, setDisplayCount] = useState<number | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionState, setConnectionState] = useState<string>("Disconnected");
  const [showMilestone, setShowMilestone] = useState(false);
  const milestoneTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const lastMilestoneRef = useRef<number>(0);

  useEffect(() => {
    // Fetch initial count
    const fetchInitialCount = async () => {
      try {
        const response = await fetchWithRateLimit<number>("/API/GetDisplayCount", {
          requestId: "displayCount",
        });

        if (response.data !== undefined) {
          setDisplayCount(response.data);
        }
      } catch (error) {
        console.error("Failed to fetch initial display count:", error);
      }
    };

    fetchInitialCount();

    const handleDisplayCountUpdate = (newCount: number) => {
      setDisplayCount(newCount);
      
      // Check for milestone
      if (newCount > 0 && newCount % 1000 === 0 && newCount !== lastMilestoneRef.current) {
        lastMilestoneRef.current = newCount;
        setShowMilestone(true);
        
        // Clear any existing timeout
        if (milestoneTimeoutRef.current) {
          clearTimeout(milestoneTimeoutRef.current);
        }
        
        // Hide milestone after 5 seconds
        milestoneTimeoutRef.current = setTimeout(() => {
          setShowMilestone(false);
        }, 5000);
      }
    };

    const handlePong = () => {
      console.log("SignalR Ping/Pong successful");
    };

    const handleConnectionStateChange = (connected: boolean, state: string) => {
      setIsConnected(connected);
      setConnectionState(state);
    };

    const initializeSignalR = async () => {
      try {
        // Set up event listeners
        await signalRService.on("DisplayCountUpdated", handleDisplayCountUpdate);
        await signalRService.on("Pong", handlePong);
        await signalRService.on("connectionStateChanged", handleConnectionStateChange);

        // Get initial connection state
        const state = signalRService.getConnectionState();
        setIsConnected(state.isConnected);
        setConnectionState(state.state);

      } catch (err) {
        console.error("TotalDisplayCounter SignalR Error: ", err);
        setConnectionState("Failed");
      }
    };

    initializeSignalR();

    // Cleanup on unmount
    return () => {
      signalRService.off("DisplayCountUpdated", handleDisplayCountUpdate);
      signalRService.off("Pong", handlePong);
      signalRService.off("connectionStateChanged", handleConnectionStateChange);
      
      if (milestoneTimeoutRef.current) {
        clearTimeout(milestoneTimeoutRef.current);
      }
    };
  }, []);

  const formatNumber = (num: number): string => {
    return num.toLocaleString("en-US");
  };

  if (displayCount === null) {
    return null; // Don't show anything while loading
  }

  return (
    <div className="relative">
      <Card className="bg-gradient-to-r from-danger/10 via-danger/5 to-danger/10 border border-danger/20 p-6 backdrop-blur-sm hover:scale-105 transition-transform duration-300">
        <div className="text-center space-y-2">
          <p className="text-sm font-medium text-danger/80 uppercase tracking-wider">
            🚨 Global Exposure Counter™ 🚨
          </p>
          <div className="relative">
            <div className="text-4xl md:text-5xl font-bold text-danger tabular-nums">
              <AnimatedNumber value={formatNumber(displayCount)} />
            </div>
            <span className="absolute -top-1 -right-1 flex h-3 w-3">
              {isConnected ? (
                <>
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-3 w-3 bg-success" title="Real-time connected"></span>
                </>
              ) : (
                <span className="relative inline-flex rounded-full h-3 w-3 bg-warning animate-pulse" title={`Connection: ${connectionState}`}></span>
              )}
            </span>
          </div>
          <p className="text-xs text-default-500 italic">
            Times developers have <span className="line-through">secured</span> shared their secrets
          </p>
          <p className="text-xs text-danger/60 font-medium">
            ({isConnected ? "Updates in real-time" : `Connection: ${connectionState}`}, unlike your security practices)
          </p>
        </div>
      </Card>
      
      {/* Floating badge for milestone celebrations */}
      {showMilestone && (
        <div className="absolute -top-2 -right-2 animate-bounce">
          <div className="bg-warning text-warning-foreground text-xs font-bold px-2 py-1 rounded-full animate-pulse">
            🎉 MILESTONE!
          </div>
        </div>
      )}
    </div>
  );
}
