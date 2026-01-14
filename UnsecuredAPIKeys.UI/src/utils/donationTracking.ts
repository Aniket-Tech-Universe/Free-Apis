import { fetchWithRateLimit } from "./api";

interface DonationClickData {
  clickLocation: string;
  sessionId?: string;
}

interface DonationConfirmationData {
  amount: number;
  transactionId?: string;
  notes?: string;
}

interface DonationClickResponse {
  success: boolean;
  trackingId?: number;
  message?: string;
}

interface DonationConfirmationResponse {
  success: boolean;
  message: string;
}

interface AutomaticDonationConfirmationData {
  trackingId: string;
  transactionId: string;
  amount: number;
  currency: string;
  status: string;
  itemName?: string;
  itemNumber?: string;
  customMessage?: string;
}

// PayPal SDK types
declare global {
  interface Window {
    PayPal: {
      Donation: {
        Button: (config: {
          env?: 'sandbox' | 'production';
          hosted_button_id?: string;
          business?: string;
          image?: {
            src: string;
            title: string;
            alt: string;
          };
          onClick?: () => void | Promise<void>;
          onComplete?: (params: {
            tx: string;
            st: string;
            amt: string;
            cc: string;
            cm?: string;
            item_number?: string;
            item_name?: string;
          }) => void;
        }) => {
          render: (selector: string) => void;
        };
      };
    };
  }
}

/**
 * Track a donation button click and return tracking ID
 */
export const trackDonationClick = async (clickLocation: string): Promise<DonationClickResponse> => {
  try {
    // Generate a simple session ID if not available
    const sessionId = sessionStorage.getItem('donation-session') || 
      `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    if (!sessionStorage.getItem('donation-session')) {
      sessionStorage.setItem('donation-session', sessionId);
    }

    const response = await fetchWithRateLimit<DonationClickResponse>("/API/TrackDonationClick", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        clickLocation,
        sessionId,
      } as DonationClickData),
      requestId: "trackDonationClick",
    });

    if (response.data?.success) {
      console.log(`Tracked donation click at ${clickLocation}`);
      return response.data;
    } else {
      console.warn("Failed to track donation click:", response.error);
      return { success: false, message: typeof response.error === 'string' ? response.error : "Unknown error" };
    }
  } catch (error) {
    console.error("Error tracking donation click:", error);
    return { success: false, message: "Network error" };
  }
};

/**
 * Confirm a donation (for manual tracking)
 */
export const confirmDonation = async (
  amount: number,
  transactionId?: string,
  notes?: string
): Promise<boolean> => {
  try {
    const response = await fetchWithRateLimit<DonationConfirmationResponse>("/API/ConfirmDonation", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        amount,
        transactionId,
        notes,
      } as DonationConfirmationData),
      requestId: "confirmDonation",
    });

    if (response.data?.success) {
      console.log(`Confirmed donation of $${amount}`);
      return true;
    } else {
      console.warn("Failed to confirm donation:", response.error);
      return false;
    }
  } catch (error) {
    console.error("Error confirming donation:", error);
    return false;
  }
};

/**
 * Enhanced PayPal button click handler with tracking
 */
export const handlePayPalClick = async (
  clickLocation: string,
  paypalUrl: string = "https://www.paypal.com/donate/?hosted_button_id=2FPWHYZ949CE8"
): Promise<void> => {
  // Track the click first
  await trackDonationClick(clickLocation);

  // Track with Google Analytics if available
  if (typeof window !== "undefined" && (window as any).gtag) {
    (window as any).gtag("event", "donate_click", {
      event_category: "donation",
      event_label: clickLocation,
    });
  }

  // Open PayPal in new window
  window.open(paypalUrl, "_blank");
};

/**
 * Confirm a donation automatically via PayPal SDK callback
 */
export const confirmDonationAutomatically = async (paymentData: {
  trackingId: string;
  transactionId: string;
  amount: number;
  currency: string;
  status: string;
  itemName?: string;
  itemNumber?: string;
  customMessage?: string;
}): Promise<boolean> => {
  try {
    const response = await fetchWithRateLimit<DonationConfirmationResponse>("/API/ConfirmDonationAutomatic", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(paymentData as AutomaticDonationConfirmationData),
      requestId: "confirmDonationAutomatic",
    });

    if (response.data?.success) {
      console.log(`Auto-confirmed donation of $${paymentData.amount} with transaction ${paymentData.transactionId}`);
      
      // Refresh donation stats in real-time
      await getDonationStats();
      
      return true;
    } else {
      console.warn("Failed to auto-confirm donation:", response.error);
      return false;
    }
  } catch (error) {
    console.error("Error auto-confirming donation:", error);
    return false;
  }
};

/**
 * Render PayPal Donate Button with tracking on actual click
 */
export const renderPayPalDonateButton = async (
  containerId: string, 
  clickLocation: string,
  hostedButtonId: string = "2FPWHYZ949CE8" // Default value, should be configured
): Promise<void> => {
  try {
    const containerElement = document.getElementById(containerId);
    if (!containerElement) {
      console.error(`Container element with ID ${containerId} not found`);
      return;
    }

    // Clear any existing PayPal buttons in this container
    containerElement.innerHTML = '';

    // Track with Google Analytics if available
    if (typeof window !== "undefined" && (window as any).gtag) {
      (window as any).gtag("event", "donate_button_render", {
        event_category: "donation",
        event_label: clickLocation,
      });
    }

    // Render PayPal button with tracking
    if (window.PayPal && window.PayPal.Donation) {
      window.PayPal.Donation.Button({
        env: 'production', // Change to 'sandbox' for testing
        hosted_button_id: hostedButtonId,
        image: {
          src: 'https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif',
          title: 'PayPal - The safer, easier way to pay online!',
          alt: 'Donate with PayPal button'
        },
        onClick: async function() {
          // Track the click when user actually clicks the button
          const trackingResponse = await trackDonationClick(clickLocation);
          console.log('Donation click tracked:', trackingResponse);
        },
        onComplete: async function (params) {
          // Track the click first if not already done
          const trackingResponse = await trackDonationClick(clickLocation);
          
          if (trackingResponse.success && trackingResponse.trackingId) {
            // Automatic donation confirmation!
            console.log('PayPal donation completed:', params);
            
            await confirmDonationAutomatically({
              trackingId: trackingResponse.trackingId.toString(),
              transactionId: params.tx,
              amount: parseFloat(params.amt),
              currency: params.cc,
              status: params.st,
              itemName: params.item_name,
              itemNumber: params.item_number,
              customMessage: params.cm
            });
          }

          // Track completion with Google Analytics
          if (typeof window !== "undefined" && (window as any).gtag) {
            (window as any).gtag("event", "donate_completed", {
              event_category: "donation",
              event_label: clickLocation,
              value: parseFloat(params.amt)
            });
          }

          // Redirect to donation success page with transaction details
          const donatedUrl = new URL('/donated', window.location.origin);
          donatedUrl.searchParams.set('txn_id', params.tx);
          if (trackingResponse.trackingId) {
            donatedUrl.searchParams.set('tracking_id', trackingResponse.trackingId.toString());
          }
          window.location.href = donatedUrl.toString();
        },
      }).render(`#${containerId}`);
    } else {
      console.error("PayPal SDK not loaded. Make sure to include the PayPal Donate SDK script.");
    }
  } catch (error) {
    console.error("Error rendering PayPal donate button:", error);
  }
};

/**
 * Initialize PayPal SDK (call this to ensure SDK is loaded)
 */
export const initializePayPalSDK = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    // Check if SDK is already loaded
    if (window.PayPal && window.PayPal.Donation) {
      resolve();
      return;
    }

    // Check if script is already in DOM
    if (document.querySelector('script[src*="paypalobjects.com/donate/sdk"]')) {
      // Script exists, wait for it to load
      const checkInterval = setInterval(() => {
        if (window.PayPal && window.PayPal.Donation) {
          clearInterval(checkInterval);
          resolve();
        }
      }, 100);
      
      // Timeout after 10 seconds
      setTimeout(() => {
        clearInterval(checkInterval);
        reject(new Error("PayPal SDK failed to load"));
      }, 10000);
      return;
    }

    // Load the SDK script
    const script = document.createElement('script');
    script.src = 'https://www.paypalobjects.com/donate/sdk/donate-sdk.js';
    script.charset = 'UTF-8';
    
    script.onload = () => {
      // Wait a bit for SDK to initialize
      setTimeout(() => {
        if (window.PayPal && window.PayPal.Donation) {
          resolve();
        } else {
          reject(new Error("PayPal SDK loaded but not initialized"));
        }
      }, 100);
    };
    
    script.onerror = () => {
      reject(new Error("Failed to load PayPal SDK script"));
    };
    
    document.head.appendChild(script);
  });
};

/**
 * Get donation statistics
 */
export const getDonationStats = async () => {
  try {
    const response = await fetchWithRateLimit("/API/GetDonationStats", {
      requestId: "getDonationStats",
    });

    return response.data;
  } catch (error) {
    console.error("Error fetching donation stats:", error);
    return null;
  }
};
