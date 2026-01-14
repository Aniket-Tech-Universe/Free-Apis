import React from 'react'
import { useRouter } from 'next/router'
import DefaultLayout from '@/layouts/default'

interface Props {
  children: React.ReactNode
}

interface State {
  hasError: boolean
  error?: Error
}

class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo)
    
    // Check if this is a URL-related error and redirect to home
    if (error.message.includes('Invalid URL') || error.message.includes('Failed to construct')) {
      setTimeout(() => {
        window.location.href = '/'
      }, 100)
    }
  }

  render() {
    if (this.state.hasError) {
      return (
        <DefaultLayout>
          <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
            <div className="inline-block max-w-lg text-center justify-center">
              <h1 className="text-4xl font-bold text-danger mb-4">Something went wrong</h1>
              <p className="text-default-500 mb-6">
                We're redirecting you to the home page...
              </p>
              <button
                onClick={() => window.location.href = '/'}
                className="bg-primary text-white px-6 py-2 rounded-lg hover:bg-primary-600 transition-colors"
              >
                Go to Home Page
              </button>
            </div>
          </section>
        </DefaultLayout>
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
