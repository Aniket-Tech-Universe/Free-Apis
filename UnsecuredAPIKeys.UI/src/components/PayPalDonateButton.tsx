import { useState, useEffect, useRef, useMemo } from "react";
import { Button } from "@heroui/button";
import { initializePayPalSDK, renderPayPalDonateButton } from "@/utils/donationTracking";

interface PayPalDonateButtonProps {
  location: string;
  size?: "sm" | "md" | "lg";
  className?: string;
  hostedButtonId?: string;
  fallbackUrl?: string;
}

export default function PayPalDonateButton({
  location,
  size = "md",
  className = "",
  hostedButtonId = "2FPWHYZ949CE8", // Default hosted button ID
  fallbackUrl = "https://www.paypal.com/donate/?hosted_button_id=2FPWHYZ949CE8"
}: PayPalDonateButtonProps) {
  const [sdkLoaded, setSdkLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [buttonRendered, setButtonRendered] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  
  // Use useMemo to ensure containerId is stable across re-renders
  const containerId = useMemo(() => 
    `paypal-donate-${location}-${Math.random().toString(36).substr(2, 9)}`,
    [location]
  );

  useEffect(() => {
    const loadSDK = async () => {
      try {
        await initializePayPalSDK();
        setSdkLoaded(true);
        setError(null);
      } catch (err) {
        console.error("Failed to load PayPal SDK:", err);
        setError(err instanceof Error ? err.message : "Failed to load PayPal SDK");
      } finally {
        setLoading(false);
      }
    };

    loadSDK();
  }, []);

  useEffect(() => {
    // Only render the button once when SDK is loaded and container is available
    if (sdkLoaded && containerRef.current && !buttonRendered) {
      renderPayPalDonateButton(containerId, location, hostedButtonId);
      setButtonRendered(true);
    }
  }, [sdkLoaded, buttonRendered, containerId, location, hostedButtonId]);

  const handleFallbackClick = () => {
    window.open(fallbackUrl, "_blank");
  };

  if (loading) {
    return (
      <Button
        size={size}
        className={`bg-gradient-to-r from-blue-500 to-blue-600 text-white ${className}`}
        disabled
      >
        <span className="animate-pulse">Loading PayPal...</span>
      </Button>
    );
  }

  if (error) {
    return (
      <Button
        size={size}
        className={`bg-gradient-to-r from-blue-500 to-blue-600 text-white hover:from-blue-600 hover:to-blue-700 ${className}`}
        onClick={handleFallbackClick}
      >
        💸 Donate (Fallback)
      </Button>
    );
  }

  return (
    <div>
      <div
        ref={containerRef}
        id={containerId}
        className="paypal-donate-button"
      />
      
      {/* Fallback button if PayPal button fails to render */}
      <div className="paypal-fallback" style={{ display: 'none' }}>
        <Button
          size={size}
          className="bg-gradient-to-r from-blue-500 to-blue-600 text-white hover:from-blue-600 hover:to-blue-700"
          onClick={handleFallbackClick}
        >
          💸 Donate via PayPal
        </Button>
      </div>
    </div>
  );
}
