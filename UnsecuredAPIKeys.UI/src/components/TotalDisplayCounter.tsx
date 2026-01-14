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

  // Easter Egg State
  const [clickCount, setClickCount] = useState(0);
  const [isVaultOpen, setIsVaultOpen] = useState(false);
  const clickTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const [vaultKeys, setVaultKeys] = useState<any[]>([]);
  const [vaultLoading, setVaultLoading] = useState(false);

  // Easter Egg Handler
  const handleEasterEggClick = () => {
    setClickCount(prev => {
      const newCount = prev + 1;
      if (newCount >= 5) {
        setIsVaultOpen(true);
        loadVaultKeys();
        return 0;
      }
      return newCount;
    });

    // Reset count if not clicked quickly enough
    if (clickTimeoutRef.current) clearTimeout(clickTimeoutRef.current);
    clickTimeoutRef.current = setTimeout(() => setClickCount(0), 2000);
  };

  const loadVaultKeys = async () => {
    setVaultLoading(true);
    try {
      // Use pure fetch to bypass any interceptors if needed, or use fetchWithRateLimit
      const res = await fetchWithRateLimit<any[]>("/API/GetVault");
      if (res.data) setVaultKeys(res.data);
    } catch (e) {
      console.error("Failed to load vault", e);
    } finally {
      setVaultLoading(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    alert("Key copied to clipboard! 🕵️");
  };

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
      if (clickTimeoutRef.current) clearTimeout(clickTimeoutRef.current);
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
      <Card
        className="bg-gradient-to-r from-danger/10 via-danger/5 to-danger/10 border border-danger/20 p-6 backdrop-blur-sm hover:scale-105 transition-transform duration-300 cursor-pointer select-none"
        onClick={handleEasterEggClick}
      >
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

      {/* EASTER EGG VAULT MODAL */}
      {isVaultOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
          <div className="bg-neutral-900 border border-success/30 w-full max-w-2xl max-h-[80vh] flex flex-col rounded-lg shadow-2xl">
            <div className="p-4 border-b border-success/20 flex justify-between items-center bg-black/40">
              <h2 className="text-xl font-mono text-success flex items-center gap-2">
                <span className="animate-pulse">🔓</span> THE VAULT
              </h2>
              <button
                onClick={(e) => { e.stopPropagation(); setIsVaultOpen(false); }}
                className="text-success/50 hover:text-success hover:bg-success/10 px-3 py-1 rounded"
              >
                CLOSE [X]
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-4 font-mono text-sm space-y-2">
              <p className="text-success/70 mb-4">{`> Accessing leaked database...`}</p>

              {vaultLoading ? (
                <div className="text-center p-8 animate-pulse text-success">
                  [Decrypting...]
                </div>
              ) : (
                <div className="space-y-3">
                  {vaultKeys.map((key, i) => (
                    <div key={i} className="flex flex-col md:flex-row md:items-center justify-between bg-white/5 p-3 rounded border border-white/10 hover:border-success/50 transition-colors gap-2">
                      <div className="overflow-hidden">
                        <div className="flex items-center gap-2 text-xs text-neutral-400 mb-1">
                          <span className={`px-1.5 rounded ${key.status === 0 ? 'bg-success/20 text-success' : 'bg-warning/20 text-warning'}`}>
                            {key.status === 0 ? 'VALID' : 'UNVERIFIED'}
                          </span>
                          <span>{key.apiType === 1 ? 'GOOGLE' : key.apiType === 2 ? 'OPENAI' : 'OTHER'}</span>
                          <span>{new Date(key.lastFoundUTC).toLocaleDateString()}</span>
                        </div>
                        <code className="text-success/90 block truncate max-w-md" title={key.apiKey}>
                          {key.apiKey}
                        </code>
                      </div>
                      <button
                        onClick={(e) => { e.stopPropagation(); copyToClipboard(key.apiKey); }}
                        className="shrink-0 px-3 py-1.5 bg-success/10 hover:bg-success/20 text-success border border-success/30 rounded text-xs uppercase tracking-wider"
                      >
                        Copy
                      </button>
                    </div>
                  ))}
                  {vaultKeys.length === 0 && (
                    <div className="text-neutral-500 italic text-center py-8">
                      {`> No valid keys found in recent scrape.`}
                    </div>
                  )}
                </div>
              )}
            </div>
            <div className="p-3 border-t border-success/20 bg-black/40 text-[10px] text-success/40 text-center font-mono uppercase">
              // FOR EDUCATIONAL PURPOSES ONLY // DO NOT USE MALICIOUSLY //
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
