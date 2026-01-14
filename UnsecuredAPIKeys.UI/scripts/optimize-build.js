#!/usr/bin/env node

/**
 * Build optimization script for Unsecured API Keys UI
 * Ensures all optimizations are applied before production build
 */

const fs = require('fs');
const path = require('path');

console.log('🚀 Running build optimizations...');

// Update sitemap with current date
function updateSitemap() {
  const sitemapPath = path.join(__dirname, '../public/sitemap.xml');
  const currentDate = new Date().toISOString().split('T')[0];
  
  try {
    let sitemap = fs.readFileSync(sitemapPath, 'utf8');
    
    // Update all lastmod dates to current date
    sitemap = sitemap.replace(/<lastmod>\d{4}-\d{2}-\d{2}<\/lastmod>/g, `<lastmod>${currentDate}</lastmod>`);
    
    // Ensure all URLs use the www domain
    sitemap = sitemap.replace(/https:\/\/unsecuredapikeys\.com\//g, 'https://www.unsecuredapikeys.com/');
    
    fs.writeFileSync(sitemapPath, sitemap);
    console.log('✅ Sitemap updated with current date and www domain');
  } catch (error) {
    console.error('❌ Error updating sitemap:', error.message);
  }
}

// Validate all required files exist
function validateFiles() {
  const requiredFiles = [
    'public/robots.txt',
    'public/sitemap.xml',
    'public/site.webmanifest',
    'public/og-image.png',
    'public/lock_icon.svg'
  ];
  
  const missingFiles = [];
  
  requiredFiles.forEach(file => {
    const filePath = path.join(__dirname, '..', file);
    if (!fs.existsSync(filePath)) {
      missingFiles.push(file);
    }
  });
  
  if (missingFiles.length > 0) {
    console.warn('⚠️  Missing files:', missingFiles.join(', '));
    console.log('Note: lock_icon.svg is your primary favicon - ensure it exists');
  } else {
    console.log('✅ All required files present');
    console.log('✅ Primary favicon (lock_icon.svg) is properly configured');
  }
}

// Check for Next.js specific optimizations
function validateNextConfig() {
  const configPath = path.join(__dirname, '../next.config.js');
  
  try {
    const config = require(configPath);
    
    if (config.output !== 'export') {
      console.warn('⚠️  next.config.js: output should be "export" for static builds');
    }
    
    if (!config.trailingSlash) {
      console.warn('⚠️  next.config.js: trailingSlash should be true for static hosting');
    }
    
    if (!config.images?.unoptimized) {
      console.warn('⚠️  next.config.js: images should be unoptimized for static exports');
    }
    
    console.log('✅ Next.js configuration validated');
  } catch (error) {
    console.error('❌ Error validating Next.js config:', error.message);
  }
}

// Main optimization function
async function runOptimizations() {
  console.log('Starting build optimizations...\n');
  
  updateSitemap();
  validateFiles();
  validateNextConfig();
  
  console.log('\n🎉 Build optimizations complete!');
  console.log('\nRecommendations:');
  console.log('- Run "npm run build" to create production build');
  console.log('- Test the build locally with a static server');
  console.log('- Validate SEO with tools like Lighthouse');
  console.log('- Check accessibility compliance');
}

// Run if called directly
if (require.main === module) {
  runOptimizations().catch(console.error);
}

module.exports = { runOptimizations };
