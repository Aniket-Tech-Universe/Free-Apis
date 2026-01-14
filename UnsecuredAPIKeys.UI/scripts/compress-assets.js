#!/usr/bin/env node

/**
 * Asset compression and optimization script
 * Compresses HTML, CSS, and JS files after build
 */

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

console.log('🗜️  Starting asset compression...');

const outDir = path.join(__dirname, '../out');

// Minify HTML files
function minifyHtml(content) {
  return content
    .replace(/\s+/g, ' ') // Replace multiple spaces with single space
    .replace(/>\s+</g, '><') // Remove spaces between tags
    .replace(/\s+>/g, '>') // Remove spaces before closing tags
    .replace(/<\s+/g, '<') // Remove spaces after opening tags
    .replace(/<!--[\s\S]*?-->/g, '') // Remove HTML comments
    .trim();
}

// Minify CSS content
function minifyCss(content) {
  return content
    .replace(/\/\*[\s\S]*?\*\//g, '') // Remove CSS comments
    .replace(/\s+/g, ' ') // Replace multiple spaces with single space
    .replace(/;\s*}/g, '}') // Remove semicolon before closing brace
    .replace(/\s*{\s*/g, '{') // Remove spaces around opening brace
    .replace(/;\s*/g, ';') // Remove spaces after semicolon
    .replace(/:\s*/g, ':') // Remove spaces after colon
    .replace(/,\s*/g, ',') // Remove spaces after comma
    .trim();
}

// Process files recursively
function processDirectory(dir) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory()) {
      processDirectory(filePath);
    } else if (stat.isFile()) {
      const ext = path.extname(file).toLowerCase();
      
      if (['.html', '.css', '.js'].includes(ext)) {
        let content = fs.readFileSync(filePath, 'utf8');
        let originalSize = content.length;
        let minified = content;
        
        // Apply minification based on file type
        if (ext === '.html') {
          minified = minifyHtml(content);
        } else if (ext === '.css') {
          minified = minifyCss(content);
        }
        // JS files are already minified by Next.js
        
        // Write minified content back
        if (minified !== content) {
          fs.writeFileSync(filePath, minified, 'utf8');
          const newSize = minified.length;
          const savings = ((originalSize - newSize) / originalSize * 100).toFixed(1);
          console.log(`✅ ${file}: ${originalSize} → ${newSize} bytes (${savings}% smaller)`);
        }
        
        // Create gzip version for servers that support it
        const gzipped = zlib.gzipSync(minified);
        fs.writeFileSync(filePath + '.gz', gzipped);
        
        const gzipSavings = ((originalSize - gzipped.length) / originalSize * 100).toFixed(1);
        console.log(`📦 ${file}.gz: ${gzipped.length} bytes (${gzipSavings}% smaller than original)`);
      }
    }
  });
}

// Check if out directory exists
if (!fs.existsSync(outDir)) {
  console.error('❌ Build output directory not found. Run "npm run build" first.');
  process.exit(1);
}

try {
  processDirectory(outDir);
  console.log('\n🎉 Asset compression complete!');
  console.log('\nOptimizations applied:');
  console.log('- HTML minification (whitespace removal, comment removal)');
  console.log('- CSS minification (comment removal, whitespace optimization)');
  console.log('- Gzip compression for all text assets');
  console.log('\nTo serve gzipped files, configure your web server to serve .gz files when available.');
} catch (error) {
  console.error('❌ Error during compression:', error.message);
  process.exit(1);
}
