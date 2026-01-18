// Google Analytics 4 utility functions for static export compatibility
// This replaces @next/third-parties functionality with custom implementation

// Hardcoded GA measurement ID for static export compatibility
const GA_MEASUREMENT_ID = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID || 'YOUR_GA_MEASUREMENT_ID'

// Check if GA is properly loaded
export const isGALoaded = (): boolean => {
  const windowExists = typeof window !== 'undefined'
  const gtagExists = windowExists && typeof window.gtag === 'function'
  const isLoaded = windowExists && gtagExists
  
  // Log GA status for debugging
  if (windowExists) {
    console.log(`[GA] Status check - gtag exists: ${gtagExists}, loaded: ${isLoaded}`)
  }
  
  return isLoaded
}

// Track page views
export const trackPageView = (url: string, title?: string): void => {
  console.log(`[GA] Attempting to track page view: ${url}`)
  
  if (!isGALoaded()) {
    console.warn('[GA] Page view not tracked - Google Analytics not loaded')
    return
  }

  const config = {
    page_title: title || document.title,
    page_location: url,
    page_path: url,
  }
  
  console.log(`[GA] Tracking page view:`, config)
  window.gtag('config', GA_MEASUREMENT_ID, config)
}

// Track custom events
export const trackEvent = (
  eventName: string,
  parameters?: {
    event_category?: string
    event_label?: string
    value?: number
    custom_parameter?: string
    [key: string]: any
  }
): void => {
  console.log(`[GA] Attempting to track event: ${eventName}`, parameters)
  
  if (!isGALoaded()) {
    console.warn(`[GA] Event '${eventName}' not tracked - Google Analytics not loaded`)
    return
  }

  const eventData = {
    event_category: parameters?.event_category || 'engagement',
    event_label: parameters?.event_label,
    value: parameters?.value,
    ...parameters,
  }
  
  console.log(`[GA] Tracking event '${eventName}':`, eventData)
  window.gtag('event', eventName, eventData)
}

// Common tracking functions for the UnsecuredAPIKeys site
export const analytics = {
  // Track API key searches
  trackSearch: (query: string, provider?: string) => {
    trackEvent('search', {
      search_term: query,
      event_category: 'api_search',
      event_label: provider || 'unknown',
    })
  },

  // Track API key downloads/views
  trackKeyView: (keyType: string, provider: string) => {
    trackEvent('view_key', {
      event_category: 'api_interaction',
      event_label: `${provider}_${keyType}`,
      key_type: keyType,
      provider: provider,
    })
  },

  // Track API key views (alias for compatibility)
  trackApiKeyView: (keyType: string, keyId: number) => {
    trackEvent('view_key', {
      event_category: 'api_interaction',
      event_label: keyType,
      key_type: keyType,
      key_id: keyId,
    })
  },

  // Track issue submissions
  trackIssueSubmission: (apiType: string, repoUrl: string) => {
    trackEvent('issue_submission', {
      event_category: 'conversion',
      event_label: apiType,
      repo_url: repoUrl,
    })
  },

  // Track API type selection
  trackApiTypeSelection: (selectedType: string) => {
    trackEvent('api_type_selection', {
      event_category: 'interaction',
      event_label: selectedType,
    })
  },

  // Track button clicks
  trackButtonClick: (buttonName: string, location: string) => {
    trackEvent('click', {
      event_category: 'ui_interaction',
      event_label: buttonName,
      click_location: location,
    })
  },

  // Track navigation
  trackNavigation: (destination: string, source: string) => {
    trackEvent('navigation', {
      event_category: 'navigation',
      event_label: `${source}_to_${destination}`,
      link_destination: destination,
      link_source: source,
    })
  },

  // Track form submissions
  trackFormSubmission: (formName: string, success: boolean) => {
    trackEvent('form_submit', {
      event_category: 'form_interaction',
      event_label: formName,
      form_success: success,
    })
  },

  // Track donation-related events
  trackDonation: (amount: number, currency: string = 'USD') => {
    trackEvent('donation', {
      event_category: 'donation',
      currency: currency,
      value: amount,
    })
  },

  // Track leaderboard interactions
  trackLeaderboardView: (category: string) => {
    trackEvent('view_leaderboard', {
      event_category: 'leaderboard',
      event_label: category,
    })
  },

  // Track error events
  trackError: (errorType: string, errorMessage: string, location: string) => {
    trackEvent('exception', {
      event_category: 'error',
      event_label: errorType,
      description: errorMessage,
      fatal: false,
      error_location: location,
    })
  },

  // Track performance metrics
  trackPerformance: (metric: string, value: number, unit: string) => {
    trackEvent('performance', {
      event_category: 'performance',
      event_label: metric,
      value: value,
      unit: unit,
    })
  },

  // Track social sharing
  trackSocialShare: (platform: string, contentType: string) => {
    trackEvent('share', {
      event_category: 'social',
      event_label: platform,
      content_type: contentType,
    })
  },

  // Track external link clicks
  trackExternalLink: (url: string, linkText: string) => {
    trackEvent('click', {
      event_category: 'external_link',
      event_label: linkText,
      link_url: url,
    })
  },
}

// Debug function to test GA setup
export const debugGA = (): void => {
  if (typeof window === 'undefined') {
    console.log('GA Debug: Running on server side')
    return
  }

  console.log('GA Debug Information:')
  console.log('- GA_MEASUREMENT_ID:', GA_MEASUREMENT_ID)
  console.log('- window.gtag exists:', typeof window.gtag === 'function')
  console.log('- window.dataLayer exists:', Array.isArray(window.dataLayer))
  console.log('- dataLayer length:', window.dataLayer?.length || 0)
  console.log('- GA loaded:', isGALoaded())

  if (isGALoaded()) {
    console.log('✅ Google Analytics is properly loaded')
    // Send a test event
    trackEvent('debug_test', {
      event_category: 'debug',
      event_label: 'ga_debug_test',
    })
    console.log('🧪 Test event sent')
  } else {
    console.log('❌ Google Analytics is not loaded properly')
  }
}
