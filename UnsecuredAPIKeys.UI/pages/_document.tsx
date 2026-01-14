import { Html, Head, Main, NextScript } from 'next/document'

export default function Document() {
  return (
    <Html lang="en">
      <Head>
        {/* Primary Meta Tags */}
        <meta name="robots" content="index, follow" />
        <meta name="googlebot" content="index, follow" />
        <meta name="language" content="English" />
        <meta name="author" content="Unsecured API Keys" />
        <meta name="referrer" content="origin-when-cross-origin" />
        
        {/* Performance Optimization - Resource Hints */}
        <link rel="preconnect" href="https://api.unsecuredapikeys.com" />
        <link rel="dns-prefetch" href="https://github.com" />
        <link rel="dns-prefetch" href="https://ui-avatars.com" />
        
        {/* Analytics - DNS Prefetch */}
        <link rel="dns-prefetch" href="https://www.googletagmanager.com" />
        <link rel="dns-prefetch" href="https://analytics.ahrefs.com" />
        
        {/* Favicons */}
        <link rel="icon" href="/lock_icon.svg" type="image/svg+xml" />
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="apple-touch-icon" href="/lock_icon.svg" />
        <link rel="manifest" href="/site.webmanifest" />
        
        {/* Theme Colors */}
        <meta name="theme-color" content="#000000" />
        <meta name="msapplication-TileColor" content="#000000" />
        <meta name="color-scheme" content="light dark" />
        
        {/* Security Headers - Note: X-Frame-Options should be set as HTTP header, not meta tag */}
        <meta httpEquiv="X-Content-Type-Options" content="nosniff" />
        <meta httpEquiv="X-XSS-Protection" content="1; mode=block" />
        <meta httpEquiv="Referrer-Policy" content="strict-origin-when-cross-origin" />
        
        {/* Google Analytics GA4 - Hardcoded for Static Export */}
        <script
          async
          src={`https://www.googletagmanager.com/gtag/js?id=${process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID || 'YOUR_GA_MEASUREMENT_ID'}`}
        />
        <script
          dangerouslySetInnerHTML={{
            __html: `
              window.dataLayer = window.dataLayer || [];
              function gtag(){dataLayer.push(arguments);}
              gtag('js', new Date());
              gtag('config', '${process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID || 'YOUR_GA_MEASUREMENT_ID'}', {
                page_title: document.title,
                page_location: window.location.href,
              });
            `,
          }}
        />
        
        {/* Ahrefs Analytics */}
        <script
          async
          src="https://analytics.ahrefs.com/analytics.js"
          data-key="VoVCWNEgbDpnTDwxcIS/0Q"
        />
        
        {/* Canonical URL will be set per page */}
      </Head>
      <body className="antialiased">
        <Main />
        <NextScript />
      </body>
    </Html>
  )
}
