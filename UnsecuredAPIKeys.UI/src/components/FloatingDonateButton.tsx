import { Button } from "@heroui/button";
import { HeartIcon } from "@heroicons/react/24/solid";
import { useState } from "react";

export default function FloatingDonateButton() {
  const [isHovered, setIsHovered] = useState(false);

  const handleDonate = () => {
    // Track the donation click
    if (typeof window !== "undefined" && (window as any).gtag) {
      (window as any).gtag("event", "donate_click", {
        event_category: "donation",
        event_label: "floating_button",
      });
    }

    // Open PayPal donation link in new tab
    window.open("https://www.paypal.com/donate/?hosted_button_id=2FPWHYZ949CE8", "_blank");
  };

  return (
    <div className="fixed bottom-6 right-6 z-50">
      <Button
        className={`
          min-w-0 h-14 px-4 
          bg-gradient-to-r from-pink-500 to-rose-500 
          hover:from-pink-600 hover:to-rose-600 
          text-white font-semibold
          shadow-lg hover:shadow-xl
          transition-all duration-300 ease-in-out
          ${isHovered ? "scale-110" : "scale-100"}
          animate-pulse
        `}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        onPress={handleDonate}
        radius="full"
        size="lg"
        startContent={
          <HeartIcon 
            className={`h-5 w-5 transition-transform duration-300 ${
              isHovered ? "scale-125" : "scale-100"
            }`} 
          />
        }
        title="Help keep the servers running! ❤️"
      >
        <span className="hidden sm:inline">Donate</span>
      </Button>
      
      {/* Tooltip for mobile */}
      {isHovered && (
        <div className="absolute bottom-16 right-0 bg-black/80 text-white text-xs px-2 py-1 rounded whitespace-nowrap">
          Help keep the servers running! ❤️
        </div>
      )}
    </div>
  );
}
