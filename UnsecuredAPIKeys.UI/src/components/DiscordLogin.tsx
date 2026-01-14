import { useState, useEffect } from 'react';
import { Button } from '@heroui/button';
import { DiscordIcon } from '@/components/icons';

interface DiscordUser {
  discordId: string;
  username: string;
  discriminator?: string;
  avatar?: string;
  isServerMember: boolean;
  rateLimitOverride?: number;
}

export default function DiscordLogin() {
  const [user, setUser] = useState<DiscordUser | null>(null);
  const [loading, setLoading] = useState(false);

  // Check for existing Discord user on component mount
  useEffect(() => {
    const savedUser = localStorage.getItem('discordUser');
    if (savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (error) {
        console.error('Failed to parse saved Discord user:', error);
        localStorage.removeItem('discordUser');
      }
    }
  }, []);

  const handleDiscordLogin = async () => {
    setLoading(true);
    try {
      // Get Discord OAuth URL from our API
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:7227'}/discordauth/login`);
      if (!response.ok) {
        throw new Error('Failed to get Discord login URL');
      }
      
      const data = await response.json();
      if (data.authUrl) {
        // Redirect to Discord OAuth
        window.location.href = data.authUrl;
      } else {
        throw new Error('No auth URL returned');
      }
    } catch (error) {
      console.error('Discord login error:', error);
      setLoading(false);
      // Could show a toast notification here
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('discordUser');
    localStorage.removeItem('discordId');
    setUser(null);
  };

  if (user) {
    return (
      <div className="flex flex-col sm:flex-row items-center gap-3 p-4 bg-gradient-to-r from-indigo-500/10 to-purple-500/10 rounded-lg border border-indigo-500/20">
        <div className="flex items-center gap-3">
          {user.avatar && (
            <img
              src={`https://cdn.discordapp.com/avatars/${user.discordId}/${user.avatar}.png`}
              alt="Discord Avatar"
              className="w-8 h-8 rounded-full"
            />
          )}
          <div className="text-left">
            <p className="text-sm font-medium text-white">
              {user.username}
              {user.discriminator && user.discriminator !== '0' && `#${user.discriminator}`}
            </p>
            <p className="text-xs text-gray-300">
              {user.isServerMember ? (
                <span className="text-green-400">✓ Enhanced Rate Limits ({user.rateLimitOverride || 20}/hour)</span>
              ) : (
                <span className="text-yellow-400">Standard Limits (5/hour)</span>
              )}
            </p>
          </div>
        </div>
        <Button
          size="sm"
          variant="ghost"
          onPress={handleLogout}
          className="text-gray-300 hover:text-white"
        >
          Logout
        </Button>
      </div>
    );
  }

  return (
    <Button
      className="bg-gradient-to-r from-indigo-500 to-purple-500 hover:from-indigo-600 hover:to-purple-600 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-300"
      onPress={handleDiscordLogin}
      isLoading={loading}
      radius="full"
      size="lg"
      startContent={!loading && <DiscordIcon className="h-5 w-5" />}
      variant="shadow"
    >
      {loading ? 'Connecting...' : 'Login with Discord'}
    </Button>
  );
}
