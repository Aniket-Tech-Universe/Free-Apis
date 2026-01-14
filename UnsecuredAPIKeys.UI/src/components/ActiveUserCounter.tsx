import { useState, useEffect } from "react";
import { Card } from "@heroui/card";
import AnimatedNumber from "./AnimatedNumber";
import { signalRService } from "@/utils/signalrService";

export default function ActiveUserCounter() {
  const [activeUsers, setActiveUsers] = useState<number>(0);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionState, setConnectionState] = useState<string>("Disconnected");

  useEffect(() => {
    const handleActiveUserCountUpdate = (count: number) => {
      setActiveUsers(count);
    };

    const handleConnectionStateChange = (connected: boolean, state: string) => {
      setIsConnected(connected);
      setConnectionState(state);
    };

    const handleValidationPing = () => {
      // Respond to server's connection validation ping
      // This helps the server know the connection is still alive
      console.log('[SignalR] Received validation ping, responding with pong');
      signalRService.invoke("Pong").catch(err => 
        console.warn('[SignalR] Failed to send pong response:', err)
      );
    };

    const initializeSignalR = async () => {
      try {
        // Set up event listeners - note: SignalR event names are case-sensitive
        // Server sends PascalCase method names
        await signalRService.on("ActiveUserCountUpdated", handleActiveUserCountUpdate);
        await signalRService.on("ValidationPing", handleValidationPing);
        await signalRService.on("connectionStateChanged", handleConnectionStateChange);

        // Get initial connection state
        const state = signalRService.getConnectionState();
        setIsConnected(state.isConnected);
        setConnectionState(state.state);

        // Request initial active user count
        if (state.isConnected) {
          await signalRService.invoke("GetActiveUserCount");
        }

      } catch (err) {
        console.error("ActiveUserCounter SignalR Error: ", err);
        setConnectionState("Failed");
      }
    };

    initializeSignalR();

    // Cleanup on unmount
    return () => {
      signalRService.off("ActiveUserCountUpdated", handleActiveUserCountUpdate);
      signalRService.off("ValidationPing", handleValidationPing);
      signalRService.off("connectionStateChanged", handleConnectionStateChange);
    };
  }, []);

  return (
    <Card className="bg-gradient-to-r from-success/10 via-success/5 to-success/10 border border-success/20 p-6 backdrop-blur-sm hover:scale-105 transition-transform duration-300">
      <div className="text-center space-y-2">
        <p className="text-sm font-medium text-success/80 uppercase tracking-wider">
          👥 Active Users Online
        </p>
        <div className="relative">
          <div className="text-4xl md:text-5xl font-bold text-success tabular-nums">
            <AnimatedNumber value={activeUsers.toString()} />
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
          {activeUsers === 1 ? "developer" : "developers"} currently watching the show
        </p>
        <p className="text-xs text-success/60 font-medium">
          {isConnected ? "Live count" : `Connection: ${connectionState}`}
        </p>
      </div>
    </Card>
  );
}
