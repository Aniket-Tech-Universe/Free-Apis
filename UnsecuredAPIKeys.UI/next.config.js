const { withSentryConfig } = require('@sentry/nextjs');

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  trailingSlash: true,
  images: {
    unoptimized: true
  },
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:7227'
  },
  
  // Enhanced optimization settings
  swcMinify: true, // Use SWC for faster minification
  
  // Compiler optimizations
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production' ? {
      exclude: ['error', 'warn']
    } : false,
  },
  
  // Webpack optimizations
  webpack: (config, { dev, isServer }) => {
    // Production optimizations
    if (!dev) {
      // Enable tree shaking
      config.optimization.usedExports = true;
      config.optimization.sideEffects = false;
      
      // Split chunks more aggressively
      config.optimization.splitChunks = {
        chunks: 'all',
        cacheGroups: {
          vendor: {
            test: /[\\/]node_modules[\\/]/,
            name: 'vendors',
            chunks: 'all',
            priority: 10,
          },
          heroui: {
            test: /[\\/]node_modules[\\/]@heroui[\\/]/,
            name: 'heroui',
            chunks: 'all',
            priority: 20,
          },
          heroicons: {
            test: /[\\/]node_modules[\\/]@heroicons[\\/]/,
            name: 'heroicons',
            chunks: 'all',
            priority: 15,
          },
          framer: {
            test: /[\\/]node_modules[\\/]framer-motion[\\/]/,
            name: 'framer-motion',
            chunks: 'all',
            priority: 15,
          },
        },
      };
      
      // Minimize bundle size
      config.optimization.minimize = true;
      
      // Remove duplicate modules
      config.optimization.providedExports = true;
      config.optimization.concatenateModules = true;
    }
    
    return config;
  },
  
  // Experimental features for better performance
  experimental: {
    optimizePackageImports: [
      '@heroui/button',
      '@heroui/card', 
      '@heroui/code',
      '@heroui/divider',
      '@heroui/dropdown',
      '@heroui/input',
      '@heroui/kbd',
      '@heroui/link',
      '@heroui/navbar',
      '@heroui/snippet',
      '@heroui/switch',
      '@heroui/system',
      '@heroui/theme',
      '@heroicons/react'
    ],
  },
  
  // Disable source maps in production for smaller bundles
  productionBrowserSourceMaps: false,
};

const sentryWebpackPluginOptions = {
  org: process.env.SENTRY_ORG || "your-sentry-org",
  project: process.env.SENTRY_PROJECT || "your-sentry-project",
  silent: true,
  // Disable Sentry in development to reduce bundle size during dev
  disableServerWebpackPlugin: process.env.NODE_ENV !== 'production',
  disableClientWebpackPlugin: process.env.NODE_ENV !== 'production',
};

// Only use Sentry config if organization and project are properly configured
const shouldUseSentry = process.env.SENTRY_ORG && process.env.SENTRY_PROJECT;
module.exports = shouldUseSentry ? withSentryConfig(nextConfig, sentryWebpackPluginOptions) : nextConfig;
