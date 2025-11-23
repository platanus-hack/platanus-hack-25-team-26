/**
 * Simple test backend for phishing detection
 * This is a dummy server that randomly detects "phishing" for testing purposes
 */

const express = require('express');
const app = express();
const PORT = 3000;

// Middleware
app.use(express.json({ limit: '10mb' }));

// Track request count for demo purposes
let requestCount = 0;

// Analyze endpoint
app.post('/analyze', (req, res) => {
  requestCount++;
  const { image, timestamp, metadata } = req.body;

  console.log(`[${new Date().toISOString()}] Received frame analysis request #${requestCount}`);
  console.log(`Timestamp: ${timestamp}, Metadata:`, metadata);

  // Dummy logic: Randomly flag as phishing (20% chance)
  // OR flag every 5th request to make testing more predictable
  const isPhishing = requestCount % 5 === 0 || Math.random() < 0.2;

  const response = {
    isPhishing: isPhishing,
    score: isPhishing ? Math.random() * 0.5 + 0.5 : Math.random() * 0.3,
    reason: isPhishing ? 'suspicious_pattern' : 'safe',
    message: isPhishing ? 'Possible phishing attempt detected' : 'No threat detected',
    details: isPhishing
      ? 'Suspicious content detected. Verify before entering credentials.'
      : 'This page appears safe.',
    severity: isPhishing ? 'warning' : 'info'
  };

  if (isPhishing) {
    console.log('⚠️  PHISHING DETECTED! Sending alert...');
  } else {
    console.log('✓ Safe - no phishing detected');
  }

  res.json(response);
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ status: 'ok', message: 'Phishing detection backend is running' });
});

// Start server
app.listen(PORT, () => {
  console.log('=================================');
  console.log('Phishing Detection Test Backend');
  console.log('=================================');
  console.log(`Server running on http://localhost:${PORT}`);
  console.log(`Health check: http://localhost:${PORT}/health`);
  console.log(`Analysis endpoint: POST http://localhost:${PORT}/analyze`);
  console.log('\nWaiting for frame analysis requests...\n');
});
