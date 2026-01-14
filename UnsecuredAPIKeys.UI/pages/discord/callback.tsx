import { useEffect, useRef } from 'react';
import { useRouter } from 'next/router';
import Head from 'next/head';

interface DiscordAuthResponse {
  success: boolean;
  user?: {
    discordId: string;
    username: string;
    discriminator?: string;
    avatar?: string;
    isServerMember: boolean;
    rateLimitOverride?: number;
  };
  message: string;
}

export default function DiscordCallback() {
  const router = useRouter();
  const hasProcessedCallback = useRef(false);

  useEffect(() => {
    const handleDiscordCallback = async () => {
      // Prevent double execution of callback processing
      if (hasProcessedCallback.current) {
        console.log('Discord callback already processed, skipping...');
        return;
      }
      
      hasProcessedCallback.current = true;
      console.log('Processing Discord callback...');
      
      try {
        const { code, state, error } = router.query;

        // Check for OAuth errors
        if (error) {
          console.error(`Discord OAuth error: ${error}`);
          return;
        }

        if (!code || typeof code !== 'string') {
          console.error('No authorization code received from Discord');
          return;
        }

        // Send code to our API
        const response = await fetch('https://api.unsecuredapikeys.com/discordauth/callback', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ code, state }),
        });

        // Always try to parse the response first
        let data: DiscordAuthResponse;
        try {
          data = await response.json();
        } catch (parseError) {
          console.error('Failed to parse API response:', parseError);
          return;
        }

        // Check if authentication was successful and store user data
        if (data.success && data.user) {
          console.log('Discord authentication successful:', data);
          
          // Store Discord ID in localStorage for future API calls
          localStorage.setItem('discordId', data.user.discordId);
          localStorage.setItem('discordUser', JSON.stringify(data.user));
        } else {
          console.error('Discord authentication failed:', {
            response,
            data,
            status: response.status,
            statusText: response.statusText
          });
        }
      } catch (error) {
        console.error('Discord OAuth error:', error);
      }
    };

    // Only run when router is ready and has query params
    if (router.isReady) {
      handleDiscordCallback();
    }

    // Redirect to home page after 1 second regardless of outcome
    const redirectTimer = setTimeout(() => {
      router.push('/');
    }, 1000);

    // Cleanup function
    return () => {
      clearTimeout(redirectTimer);
      hasProcessedCallback.current = false;
    };
  }, [router.isReady, router.query, router]);

  return (
    <>
      <Head>
        <title>Discord Authentication - UnsecuredAPIKeys</title>
        <meta name="description" content="Processing Discord authentication..." />
      </Head>

      <div className="min-h-screen bg-gray-900 flex items-center justify-center px-4">
        <div className="max-w-md w-full bg-gray-800 rounded-lg shadow-lg p-8">
          <div className="text-center">
            <div className="w-16 h-16 bg-green-500 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-xl font-semibold text-white mb-2">Processing Complete</h2>
            <p className="text-gray-400">Redirecting to home page...</p>
          </div>
        </div>
      </div>
    </>
  );
}
