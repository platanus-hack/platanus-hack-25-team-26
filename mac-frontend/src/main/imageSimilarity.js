/**
 * ImageSimilarity - Efficient image comparison to prevent redundant backend calls
 *
 * This module compares consecutive frames using fast bitmap sampling to detect
 * similarity. If images are too similar (difference < threshold), the frame is
 * skipped to avoid unnecessary API calls.
 *
 * Implementation details:
 * - Compares raw RGBA bitmap data (not JPEG - avoids compression artifacts)
 * - Uses pixel-based sampling (samples ~2000 pixels from RGBA data)
 * - Compares RGB channels with normalized difference calculation
 * - Default threshold: 5% (only sends if frames differ by >5%)
 * - Resets state on window changes for accurate detection
 *
 * Performance:
 * - Comparison time: ~1-2ms for typical frame sizes
 * - Memory efficient: stores only previous bitmap reference
 */
class ImageSimilarity {
  constructor(threshold = 0.05) {
    this.threshold = threshold;
    this.previousBitmap = null;
    this.previousMetadata = null;
  }

  /**
   * Compare current frame with previous frame to detect similarity
   * @param {Buffer} imageBuffer - Current frame as raw bitmap buffer (RGBA)
   * @param {Object} metadata - Frame metadata (width, height, etc.)
   * @returns {boolean} - True if images are different enough (should be sent), false if too similar
   */
  shouldSendFrame(imageBuffer, metadata) {
    if (!this.previousBitmap) {
      this.previousBitmap = imageBuffer;
      this.previousMetadata = metadata;
      console.log(`[Similarity] First frame - storing as reference`);
      return true;
    }

    const similarity = this.compareImages(imageBuffer, this.previousBitmap, metadata);
    const difference = 1 - similarity;

    console.log(`[Similarity] Similarity: ${(similarity * 100).toFixed(2)}%, Difference: ${(difference * 100).toFixed(2)}%, Threshold: ${(this.threshold * 100)}%`);

    const shouldSend = difference > this.threshold;

    if (shouldSend) {
      this.previousBitmap = imageBuffer;
      this.previousMetadata = metadata;
    }

    return shouldSend;
  }

  /**
   * Compare two bitmap buffers using efficient sampling
   * Uses a grid-based sampling approach for fast comparison
   * Compares raw RGBA bitmap data for accurate similarity detection
   * @param {Buffer} currentBuffer - Current bitmap buffer (RGBA format)
   * @param {Buffer} previousBuffer - Previous bitmap buffer (RGBA format)
   * @param {Object} metadata - Image metadata
   * @returns {number} - Similarity score (0-1, where 1 is identical)
   */
  compareImages(currentBuffer, previousBuffer, metadata) {
    // Buffers must be same length
    if (currentBuffer.length !== previousBuffer.length) {
      console.log(`[Similarity] Buffer length mismatch: ${currentBuffer.length} vs ${previousBuffer.length}`);
      return 0;
    }

    // Sample every Nth pixel for performance
    // Bitmap is RGBA (4 bytes per pixel)
    const bytesPerPixel = 4;
    const totalPixels = currentBuffer.length / bytesPerPixel;

    // Sample 2000 pixels evenly distributed
    const sampleCount = Math.min(2000, totalPixels);
    const pixelStep = Math.floor(totalPixels / sampleCount);

    let totalDifference = 0;

    for (let pixel = 0; pixel < totalPixels; pixel += pixelStep) {
      const i = pixel * bytesPerPixel;

      // Compare RGB values (ignore alpha channel at index i+3)
      const rDiff = Math.abs(currentBuffer[i] - previousBuffer[i]);
      const gDiff = Math.abs(currentBuffer[i + 1] - previousBuffer[i + 1]);
      const bDiff = Math.abs(currentBuffer[i + 2] - previousBuffer[i + 2]);

      // Average RGB difference for this pixel
      const pixelDiff = (rDiff + gDiff + bDiff) / 3;

      // Normalize to 0-1 range (max difference is 255)
      totalDifference += pixelDiff / 255;
    }

    // Calculate average normalized difference
    const avgDifference = totalDifference / sampleCount;

    // Convert to similarity (1 = identical, 0 = completely different)
    const similarity = 1 - avgDifference;

    return similarity;
  }

  /**
   * Reset the comparison state (useful when window changes)
   */
  reset() {
    this.previousBitmap = null;
    this.previousMetadata = null;
  }

  /**
   * Update the threshold for similarity detection
   * @param {number} threshold - New threshold value (0-1)
   */
  setThreshold(threshold) {
    if (threshold >= 0 && threshold <= 1) {
      this.threshold = threshold;
    }
  }

  /**
   * Get current statistics
   * @returns {Object} - Current state information
   */
  getStats() {
    return {
      threshold: this.threshold,
      hasPreviousFrame: this.previousBitmap !== null,
      previousFrameSize: this.previousBitmap ? this.previousBitmap.length : 0
    };
  }
}

module.exports = ImageSimilarity;
