import type { AppProps } from 'next/app'
import * as Sentry from "@sentry/nextjs"
import dynamic from 'next/dynamic'
import { useRouter } from 'next/router'
import { useEffect } from 'react'
import ErrorBoundary from '@/components/ErrorBoundary'
import { trackPageView } from '@/utils/analytics'
import "@/styles/globals.css"

const ClientProvider = dynamic(() => import("@/components/ClientProvider"), {
  ssr: false
})

// Initialize Sentry
if (process.env.NEXT_PUBLIC_SENTRY_DSN) {
  Sentry.init({
    dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,
    environment: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT || 'development',
    sendDefaultPii: true,
    integrations: [],
    tracesSampleRate: parseFloat(process.env.NEXT_PUBLIC_SENTRY_TRACES_SAMPLE_RATE || '0.1'),
    tracePropagationTargets: [
      process.env.NEXT_PUBLIC_API_URL || "https://api.unsecuredapikeys.com",
      "https://unsecuredapikeys.com",
      "localhost",
    ],
    // Session Replay
    replaysSessionSampleRate: parseFloat(process.env.NEXT_PUBLIC_SENTRY_REPLAYS_SESSION_SAMPLE_RATE || '0.0'),
    replaysOnErrorSampleRate: parseFloat(process.env.NEXT_PUBLIC_SENTRY_REPLAYS_ON_ERROR_SAMPLE_RATE || '0.0'),
  })
}

function MyApp({ Component, pageProps }: AppProps) {
  const router = useRouter()

  useEffect(() => {
    console.log('[GA] Initializing page tracking system')
    
    // Track initial page view
    const handleRouteChange = (url: string) => {
      console.log(`[GA] Route changed to: ${url}`)
      trackPageView(url)
    }

    // Track the current page on mount
    console.log(`[GA] Tracking initial page: ${router.asPath}`)
    handleRouteChange(router.asPath)

    // Track route changes
    router.events.on('routeChangeComplete', handleRouteChange)
    console.log('[GA] Route change listener attached')

    // Cleanup function to remove event listener
    return () => {
      console.log('[GA] Cleaning up route change listener')
      router.events.off('routeChangeComplete', handleRouteChange)
    }
  }, [router.events, router.asPath])

  return (
    <ErrorBoundary>
      <ClientProvider>
        <Component {...pageProps} />
      </ClientProvider>
    </ErrorBoundary>
  )
}

export default MyApp
