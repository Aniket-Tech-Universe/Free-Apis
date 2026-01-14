import * as signalR from "@microsoft/signalr";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isInitializing = false;
  private listeners: { [event: string]: Array<(...args: any[]) => void> } = {};
  private connectionState: string = "Disconnected";
  private isConnected = false;

  async getConnection(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (this.isInitializing) {
      // Wait for initialization to complete
      while (this.isInitializing) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      return this.connection!;
    }

    this.isInitializing = true;

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/stats`, {
          withCredentials: true,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount === 0) return 0;
            if (retryContext.previousRetryCount === 1) return 2000;
            if (retryContext.previousRetryCount === 2) return 10000;
            return 30000;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Connection state handlers
      this.connection.onreconnecting(() => {
        this.isConnected = false;
        this.connectionState = "Reconnecting";
        console.log("SharedSignalR Reconnecting...");
        this.notifyConnectionStateChange();
      });

      this.connection.onreconnected(() => {
        this.isConnected = true;
        this.connectionState = "Connected";
        console.log("SharedSignalR Reconnected!");
        this.notifyConnectionStateChange();
      });

      this.connection.onclose((error) => {
        this.isConnected = false;
        this.connectionState = "Disconnected";
        console.error("SharedSignalR Connection Closed:", error);
        this.notifyConnectionStateChange();
      });

      // Register existing listeners
      this.reattachListeners();

      await this.connection.start();
      console.log("SharedSignalR Connected!");
      this.isConnected = true;
      this.connectionState = "Connected";
      this.notifyConnectionStateChange();

    } catch (err) {
      console.error("SharedSignalR Connection Error: ", err);
      this.connectionState = "Failed";
      this.notifyConnectionStateChange();
      throw err;
    } finally {
      this.isInitializing = false;
    }

    return this.connection;
  }

  private reattachListeners() {
    if (!this.connection) return;

    Object.entries(this.listeners).forEach(([event, callbacks]) => {
      callbacks.forEach(callback => {
        this.connection!.on(event, callback);
      });
    });
  }

  private notifyConnectionStateChange() {
    const stateChangeCallbacks = this.listeners['connectionStateChanged'] || [];
    stateChangeCallbacks.forEach(callback => {
      callback(this.isConnected, this.connectionState);
    });
  }

  async on(event: string, callback: (...args: any[]) => void) {
    if (!this.listeners[event]) {
      this.listeners[event] = [];
    }
    this.listeners[event].push(callback);

    const connection = await this.getConnection();
    connection.on(event, callback);
  }

  async off(event: string, callback: (...args: any[]) => void) {
    if (this.listeners[event]) {
      this.listeners[event] = this.listeners[event].filter(c => c !== callback);
    }

    if (this.connection) {
      this.connection.off(event, callback);
    }
  }

  async invoke(method: string, ...args: any[]) {
    const connection = await this.getConnection();
    return connection.invoke(method, ...args);
  }

  getConnectionState() {
    return {
      isConnected: this.isConnected,
      state: this.connectionState
    };
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.isConnected = false;
      this.connectionState = "Disconnected";
    }
  }
}

// Export singleton instance
export const signalRService = new SignalRService();
