const activeWin = require('active-win');
const ImageSimilarity = require('./imageSimilarity');

class ScreenCapture {
  constructor() {
    this.isCapturing = false;
    this.captureInterval = null;
    this.captureIntervalMs = 3000; // Default: Capture every 3 seconds
    this.focusedWindowUpdateMs = 500; // Update focused window info every 500ms
    this.lastFocusedWindow = null;
    this.focusedWindowInterval = null;
    this.captureMode = 'window'; // 'window' or 'screen'
    this.fallbackCount = 0;
    this.maxFallbacksBeforeWarning = 5;
    this.onWindowChangeCallback = null; // Callback for immediate window changes
    this.onFrameCallback = null; // Store callback for immediate captures

    // Adaptive timing state
    this.isRisky = false; // Current risk state
    this.safeFrameCount = 0; // Consecutive safe frames
    this.riskyIntervalMs = 1500; // Fast: 1.5s when risky
    this.normalIntervalMs = 2000; // Normal: 2s default
    this.slowIntervalMs = 3000; // Slow: 3s when safe for 20s
    this.safeFramesBeforeSlow = 10; // 10 frames * 2s = 20s

    // Image similarity detection (5% threshold)
    this.imageSimilarity = new ImageSimilarity(0.05);
    this.skippedFrameCount = 0;
  }

  /**
   * Start capturing screen frames
   * @param {Function} onFrameCallback - Callback to handle captured frames
   * @param {Function} onWindowChangeCallback - Callback when window changes (immediate)
   */
  async startCapture(onFrameCallback, onWindowChangeCallback = null) {
    this.onFrameCallback = onFrameCallback; // Store for immediate captures
    this.onWindowChangeCallback = onWindowChangeCallback;
    if (this.isCapturing) {
      console.log('Screen capture already running');
      return;
    }

    this.isCapturing = true;
    console.log('Starting intelligent screen capture...');
    console.log('Mode: Focused window with fallback to full screen');

    // Start tracking focused window
    this.startFocusedWindowTracking();

    // Capture first frame immediately
    await this.captureFrame(onFrameCallback);

    // Set up interval for subsequent captures
    this.captureInterval = setInterval(async () => {
      await this.captureFrame(onFrameCallback);
    }, this.captureIntervalMs);
  }

  /**
   * Start tracking the focused window
   */
  startFocusedWindowTracking() {
    // Get focused window immediately
    this.updateFocusedWindow();

    // Update focused window info periodically
    this.focusedWindowInterval = setInterval(() => {
      this.updateFocusedWindow();
    }, this.focusedWindowUpdateMs);
  }

  /**
   * Update the currently focused window information
   */
  async updateFocusedWindow() {
    try {
      const focusedWindow = await activeWin();
      
      if (focusedWindow) {
        const newWindow = {
          title: focusedWindow.title,
          owner: focusedWindow.owner,
          bounds: focusedWindow.bounds,
          platform: focusedWindow.platform,
          // Store for matching
          appName: focusedWindow.owner?.name || '',
          appPath: focusedWindow.owner?.path || '',
          processId: focusedWindow.owner?.processId || null
        };

        // Detect window change and notify immediately
        if (this.lastFocusedWindow && this.onWindowChangeCallback) {
          const oldKey = `${this.lastFocusedWindow.appName}|||${this.lastFocusedWindow.title}`;
          const newKey = `${newWindow.appName}|||${newWindow.title}`;

          if (oldKey !== newKey) {
            // Window changed - reset image similarity state
            this.imageSimilarity.reset();
            this.skippedFrameCount = 0;

            // Notify window change
            this.onWindowChangeCallback({
              title: newWindow.title,
              app: newWindow.appName
            });

            // Trigger immediate capture of new window (don't wait 3 seconds)
            if (this.onFrameCallback) {
              console.log('[Capture] Window changed - triggering immediate capture');
              this.captureFrame(this.onFrameCallback);
            }
          }
        }

        this.lastFocusedWindow = newWindow;
      }
    } catch (error) {
      // Silently handle errors in focused window tracking
      // This is not critical and shouldn't break capture
      if (this.fallbackCount === 0) {
        console.log('Note: Focused window tracking unavailable, using screen capture');
      }
    }
  }

  /**
   * Stop tracking the focused window
   */
  stopFocusedWindowTracking() {
    if (this.focusedWindowInterval) {
      clearInterval(this.focusedWindowInterval);
      this.focusedWindowInterval = null;
    }
  }

  /**
   * Find the best matching window source for the focused window
   * @param {Array} windowSources - Available window sources from desktopCapturer
   * @returns {Object|null} - Matching source or null
   */
  findMatchingWindowSource(windowSources) {
    if (!this.lastFocusedWindow || windowSources.length === 0) {
      return null;
    }

    const focusedTitle = this.lastFocusedWindow.title.toLowerCase();
    const focusedApp = this.lastFocusedWindow.appName.toLowerCase();

    // Try to find exact or close match
    for (const source of windowSources) {
      const sourceName = source.name.toLowerCase();
      
      // Strategy 1: Exact title match
      if (sourceName === focusedTitle) {
        return source;
      }

      // Strategy 2: Title contains the focused window title
      if (focusedTitle.length > 3 && sourceName.includes(focusedTitle)) {
        return source;
      }

      // Strategy 3: Source name contains app name and partial title match
      if (focusedApp.length > 2) {
        const titleWords = focusedTitle.split(' ').filter(w => w.length > 2);
        const hasAppMatch = sourceName.includes(focusedApp);
        const hasTitleWordMatch = titleWords.some(word => sourceName.includes(word));
        
        if (hasAppMatch && hasTitleWordMatch) {
          return source;
        }
      }

      // Strategy 4: App name match (less precise but better than nothing)
      if (focusedApp.length > 3 && sourceName.includes(focusedApp)) {
        return source;
      }
    }

    return null;
  }

  /**
   * Capture a single frame - tries focused window first, falls back to screen
   * @param {Function} callback - Callback to handle the captured frame
   */
  async captureFrame(callback) {
    try {
      const { desktopCapturer } = require('electron');
      
      let capturedSource = null;
      let captureType = 'screen'; // 'window' or 'screen'
      let sourceName = '';

      // Try to capture focused window first
      if (this.lastFocusedWindow) {
        try {
          // Get available window sources
          const windowSources = await desktopCapturer.getSources({
            types: ['window'],
            thumbnailSize: { width: 1280, height: 720 } // Reduced for faster processing
          });

          // Find matching window
          const matchedWindow = this.findMatchingWindowSource(windowSources);
          
          if (matchedWindow) {
            capturedSource = matchedWindow;
            captureType = 'window';
            sourceName = matchedWindow.name;
            this.fallbackCount = 0; // Reset fallback counter
          }
        } catch (windowError) {
          // Window capture failed, will fall back to screen
          console.log('Window capture unavailable, falling back to screen');
        }
      }

      // Fallback to screen capture if window capture failed
      if (!capturedSource) {
        const screenSources = await desktopCapturer.getSources({
          types: ['screen'],
          thumbnailSize: { width: 1280, height: 720 } // Reduced for faster processing
        });

        if (screenSources.length === 0) {
          console.error('No screen sources available');
          return;
        }

        capturedSource = screenSources[0];
        sourceName = screenSources[0].name;
        this.fallbackCount++;

        // Warn if consistently falling back
        if (this.fallbackCount === this.maxFallbacksBeforeWarning) {
          console.log('Notice: Consistently using screen capture. Window matching may not be working.');
          this.fallbackCount = 0; // Reset to avoid spam
        }
      }

      // Get thumbnail
      const thumbnail = capturedSource.thumbnail;

      // OPTIMIZATION 1: Parallel Image Processing
      // Resize to optimal dimensions for LLM (640x360 is sufficient for phishing detection)
      const size = thumbnail.getSize();
      const targetWidth = 640;
      const targetHeight = Math.round((size.height / size.width) * targetWidth);
      
      // Resize image before encoding (faster processing + smaller upload)
      const resizedThumbnail = thumbnail.resize({
        width: targetWidth,
        height: targetHeight,
        quality: 'good' // Balance between speed and quality
      });

      // Get raw bitmap data for similarity comparison (BEFORE JPEG encoding)
      const bitmapBuffer = resizedThumbnail.toBitmap();

      // Check image similarity using raw bitmap data
      const metadata = {
        width: targetWidth,
        height: targetHeight,
        captureType: captureType
      };

      const shouldSend = this.imageSimilarity.shouldSendFrame(bitmapBuffer, metadata);

      if (!shouldSend) {
        this.skippedFrameCount++;
        console.log(`⏭️  Frame skipped - too similar to previous (${this.skippedFrameCount} consecutive skips)`);
        return;
      }

      // Convert to JPEG only if we're sending it
      const imageData = resizedThumbnail.toJPEG(65); // 65% quality - optimal for upload speed
      const base64Image = imageData.toString('base64');

      // Reset skip counter when we send a frame
      if (this.skippedFrameCount > 0) {
        console.log(`✅ Frame different - sending to backend (skipped ${this.skippedFrameCount} similar frames)`);
        this.skippedFrameCount = 0;
      } else {
        console.log(`✅ Frame different - sending to backend`);
      }

      // Prepare frame metadata
      const frameData = {
        image: base64Image,
        timestamp: Date.now(),
        captureType: captureType,
        sourceName: sourceName,
        width: thumbnail.getSize().width,
        height: thumbnail.getSize().height,
        // Include focused window info for context
        focusedWindow: this.lastFocusedWindow ? {
          title: this.lastFocusedWindow.title,
          app: this.lastFocusedWindow.appName
        } : null
      };

      // Log capture info occasionally (every 10th capture)
      if (Math.random() < 0.1) {
        console.log(`Captured ${captureType}: "${sourceName.substring(0, 50)}${sourceName.length > 50 ? '...' : ''}"`);
        if (this.lastFocusedWindow) {
          console.log(`  Focused: ${this.lastFocusedWindow.appName} - "${this.lastFocusedWindow.title.substring(0, 50)}${this.lastFocusedWindow.title.length > 50 ? '...' : ''}"`);
        }
      }

      // Call the callback with the frame data
      if (callback) {
        callback(frameData);
      }
    } catch (error) {
      console.error('Error capturing frame:', error.message);
      // Don't throw - just log and continue
    }
  }

  /**
   * Stop capturing screen frames
   */
  stopCapture() {
    if (this.captureInterval) {
      clearInterval(this.captureInterval);
      this.captureInterval = null;
    }
    this.stopFocusedWindowTracking();
    this.isCapturing = false;
    console.log('Screen capture stopped');
  }

  /**
   * Check if capture is currently running
   */
  isActive() {
    return this.isCapturing;
  }

  /**
   * Update capture interval based on risk level (Adaptive Timing)
   * @param {boolean} isRisky - Whether current content is risky
   */
  updateCaptureInterval(isRisky) {
    const wasRisky = this.isRisky;
    this.isRisky = isRisky;
    
    let newInterval = this.normalIntervalMs;
    
    if (isRisky) {
      // Risky content detected - capture more frequently
      newInterval = this.riskyIntervalMs;
      this.safeFrameCount = 0; // Reset safe counter
      
      if (!wasRisky) {
        console.log(`[Capture] Switching to FAST mode (${newInterval}ms) - risky content detected`);
      }
    } else {
      // Safe content
      this.safeFrameCount++;
      
      if (this.safeFrameCount >= this.safeFramesBeforeSlow) {
        // Safe for extended period - slow down
        newInterval = this.slowIntervalMs;
        
        if (this.captureIntervalMs !== this.slowIntervalMs) {
          console.log(`[Capture] Switching to SLOW mode (${newInterval}ms) - safe for 30s`);
        }
      } else {
        // Normal capture rate
        newInterval = this.normalIntervalMs;
        
        if (wasRisky) {
          console.log(`[Capture] Switching to NORMAL mode (${newInterval}ms) - content now safe`);
        }
      }
    }
    
    // Only restart interval if timing changed
    if (newInterval !== this.captureIntervalMs && this.isCapturing) {
      this.captureIntervalMs = newInterval;
      this.restartCaptureInterval();
    }
  }

  /**
   * Restart capture interval with new timing
   */
  restartCaptureInterval() {
    if (!this.isCapturing || !this.onFrameCallback) return;
    
    // Clear existing interval
    if (this.captureInterval) {
      clearInterval(this.captureInterval);
    }
    
    // Set new interval with updated timing
    this.captureInterval = setInterval(async () => {
      await this.captureFrame(this.onFrameCallback);
    }, this.captureIntervalMs);
  }

  /**
   * Get current capture statistics
   */
  getStats() {
    return {
      isCapturing: this.isCapturing,
      captureMode: this.captureMode,
      focusedWindow: this.lastFocusedWindow,
      fallbackCount: this.fallbackCount,
      captureIntervalMs: this.captureIntervalMs,
      isRisky: this.isRisky,
      safeFrameCount: this.safeFrameCount,
      skippedFrameCount: this.skippedFrameCount,
      similarityStats: this.imageSimilarity.getStats()
    };
  }
}

module.exports = ScreenCapture;
