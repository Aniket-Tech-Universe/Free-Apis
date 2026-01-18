// Global type declarations for Google Analytics
declare global {
  interface Window {
    gtag: (
      command: 'config' | 'event' | 'js' | 'set',
      targetId: string,
      config?: Record<string, any>
    ) => void;
    dataLayer: unknown[];
    PayPal?: {
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

declare module "@react-types/shared" {
  interface RouterConfig {
    routerOptions: unknown;
  }
}

export {};
