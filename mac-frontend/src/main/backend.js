const axios = require('axios');
const FormData = require('form-data');

class BackendClient {
  constructor() {
    // Production API
    this.backendUrl = 'https://pjemsvms4u.us-east-2.awsapprunner.com/evaluate';

    // Dev API
    // this.backendUrl = 'http://localhost:8000/evaluate';

    this.timeout = 30000; // 30 second timeout for AI processing
  }

  /**
   * Send frame data to backend for analysis
   * @param {Object} frameData - Frame data including base64 image and metadata
   * @returns {Promise<Object>} - Analysis result from backend
   */
  async analyzeFrame(frameData) {
    let imageBuffer = null; // Declare outside try block for error handler access
    try {
      console.log('Analyzing frame - converting base64 to buffer...');
      // Convert base64 image to buffer
      // The image is already plain base64, no need to strip data URL prefix
      imageBuffer = Buffer.from(frameData.image, 'base64');
      const sizeKB = (imageBuffer.length / 1024).toFixed(2);
      console.log(`Buffer created, size: ${sizeKB} KB (${imageBuffer.length} bytes)`);
      
      // Create form data with image and metadata
      const formData = new FormData();
      formData.append('file', imageBuffer, {
        filename: 'screenshot.jpg',
        contentType: 'image/jpeg'
      });
      
      // Add focused window metadata if available
      if (frameData.focusedWindow) {
        formData.append('window_title', frameData.focusedWindow.title || '');
        formData.append('window_app', frameData.focusedWindow.app || '');
      }
      
      // Add capture context
      formData.append('capture_type', frameData.captureType || 'screen');
      
      const startTime = Date.now();
      const headers = formData.getHeaders();
      console.log(`Sending request to ${this.backendUrl}... (timeout: ${this.timeout}ms)`);
      console.log('Headers:', headers);
      const response = await axios.post(this.backendUrl, formData, {
        timeout: this.timeout,
        headers: headers
      });
      const duration = Date.now() - startTime;
      console.log(`Response received: ${response.status} (took ${duration}ms)`);

      // API returns: { scoring: number, title: string, reason: string }
      // where scoring: 0 = safe, 10 = most risky
      console.log('\nResponse data:', response.data);
      const scoring = response.data.scoring || 0;
      const title = response.data.title
      const reason = response.data.reason || 'No specific reason provided';

      return {
        scoring: scoring,
        isPhishing: scoring >= 3, // Consider risky if scoring >= 3
        severity: this.getSeverityLevel(scoring),
        title: title, // Title from API
        message: reason, // Use API's reason directly
        details: this.getDetails(scoring)
      };
    } catch (error) {
      if (error.code === 'ECONNREFUSED') {
        console.error('❌ Backend service not available at', this.backendUrl);
      } else if (error.code === 'ETIMEDOUT' || error.code === 'ECONNABORTED') {
        console.error(`❌ Backend request timed out after ${this.timeout}ms`);
        console.error('   Image size:', imageBuffer ? `${imageBuffer.length} bytes` : 'unknown');
        console.error('   This might be an API performance issue.');
      } else {
        console.error('❌ Error analyzing frame:', error.message);
        console.error('   Error code:', error.code);
        if (error.response) {
          console.error('   Response status:', error.response.status);
          console.error('   Response data:', error.response.data);
        }
      }

      // Return safe default response on error
      return {
        isPhishing: false,
        score: 0,
        reason: 'error',
        error: error.message
      };
    }
  }

  /**
   * Test backend connectivity
   * @returns {Promise<boolean>} - True if backend is reachable
   */
  async testConnection() {
    try {
      await axios.get(this.backendUrl.replace('/analyze', '/health'), {
        timeout: 2000
      });
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * Get severity level based on scoring
   * @param {number} scoring - Risk score from 0-10
   * @returns {string} - Severity level: 'low', 'medium', 'high', 'critical'
   */
  getSeverityLevel(scoring) {
    if (scoring >= 8) return 'critical';
    if (scoring >= 6) return 'high';
    if (scoring >= 3) return 'medium';
    return 'low';
  }

  /**
   * Get alert details based on scoring
   * @param {number} scoring - Risk score from 0-10
   * @returns {string} - Alert details
   */
  getDetails(scoring) {
    if (scoring >= 8) return 'This appears to be a phishing attempt. Do NOT enter credentials!';
    if (scoring >= 6) return 'Suspicious patterns detected. Verify before proceeding.';
    if (scoring >= 3) return 'This might be suspicious. Check carefully.';
    return 'No threats detected';
  }
}

module.exports = BackendClient;
