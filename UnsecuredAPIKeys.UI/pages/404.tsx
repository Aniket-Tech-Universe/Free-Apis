import { useEffect } from 'react'
import { useRouter } from 'next/router'
import Head from 'next/head'
import DefaultLayout from '@/layouts/default'

export default function Custom404() {
  const router = useRouter()

  useEffect(() => {
    // Redirect to home page after a short delay
    const timer = setTimeout(() => {
      router.replace('/')
    }, 3000)

    return () => clearTimeout(timer)
  }, [router])

  const handleRedirectNow = () => {
    router.replace('/')
  }

  return (
    <DefaultLayout>
      <Head>
        <title>Page Not Found - UnsecuredAPIKeys</title>
        <meta name="description" content="Page not found. Redirecting to home page." />
      </Head>
      
      <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
        <div className="inline-block max-w-lg text-center justify-center">
          <h1 className="text-4xl font-bold text-danger mb-4">404</h1>
          <h2 className="text-2xl font-semibold mb-4">Page Not Found</h2>
          <p className="text-default-500 mb-6">
            The page you're looking for doesn't exist or the URL may be malformed.
            You'll be redirected to the home page in a few seconds.
          </p>
          
          <button
            onClick={handleRedirectNow}
            className="bg-primary text-white px-6 py-2 rounded-lg hover:bg-primary-600 transition-colors"
          >
            Go to Home Page Now
          </button>
        </div>
      </section>
    </DefaultLayout>
  )
}
