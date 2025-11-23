/**
 * Overlay Renderer Process
 * Handles phishing alert panel display and cursor tracking
 */

console.log('[Overlay] overlay.js loaded!');
console.log('[Overlay] window.api available:', typeof window.api);

class OverlayManager {
  constructor() {
    this.panel = document.getElementById('alert-panel');
    this.titleElement = document.getElementById('alert-title');
    this.reasonElement = document.getElementById('alert-reason');

    this.isAlertActive = false;
    this.cursorTrackingInterval = null;
    this.offsetX = 12; // Horizontal offset from cursor (4px to the right)
    this.offsetY = 12; // Vertical offset from cursor (4px to the bottom)

    this.initialize();
  }

  /**
   * Initialize the overlay manager
   */
  initialize() {
    console.log('[Overlay] OverlayManager initializing...');

    // Set up IPC listeners
    this.setupIPCListeners();

    // Set up panel click handler
    this.setupPanelClickHandler();

    console.log('[Overlay] OverlayManager initialized');
  }

  /**
   * Set up IPC event listeners
   */
  setupIPCListeners() {
    // Listen for phishing alerts (new)
    window.api.onPhishingAlert((alertData) => {
      console.log('Received phishing alert:', alertData);
      this.showAlert(alertData);
    });

    // Listen for alert updates (silent)
    window.api.onUpdateAlert((alertData) => {
      console.log('Received alert update:', alertData);
      this.updateAlert(alertData);
    });

    // Listen for clear alert events
    window.api.onClearAlert(() => {
      console.log('Received clear alert');
      this.hideAlert();
    });
  }

  /**
   * Set up click handler for the panel
   */
  setupPanelClickHandler() {
    this.panel.addEventListener('click', () => {
      console.log('Panel clicked, dismissing alert');
      this.hideAlert();
      window.api.dismissAlert();
    });
  }

  /**
   * Show the alert panel
   * @param {Object} alertData - Alert data from main process
   */
  async showAlert(alertData) {
    console.log('[Overlay] showAlert() called');

    // Update panel content - title in bold, reason below
    this.titleElement.textContent = alertData.title || 'Security Alert';
    this.reasonElement.textContent = alertData.message || 'Suspicious content detected';

    // Apply severity-based styling
    const severity = alertData.severity || 'medium';
    this.panel.className = 'alert-panel'; // Reset classes
    this.panel.classList.add(`severity-${severity}`);

    // Set active flag
    this.isAlertActive = true;
    console.log('[Overlay] isAlertActive set to true, calling updatePanelPosition()');

    // Position panel FIRST and wait for it
    await this.updatePanelPosition();
    
    console.log('[Overlay] updatePanelPosition() completed');

    // Now show with smooth transition (position is already set)
    requestAnimationFrame(() => {
      this.panel.classList.add('show');
      console.log('[Overlay] Panel shown');
    });

    // Start tracking cursor for movement
    this.startCursorTracking();
  }

  /**
   * Update alert content without animation (silent update)
   * @param {Object} alertData - Updated alert data
   */
  updateAlert(alertData) {
    if (!this.isAlertActive) return;

    // Update content silently - title in bold, reason below
    this.titleElement.textContent = alertData.title || 'Security Alert';
    this.reasonElement.textContent = alertData.message || 'Suspicious content detected';

    // Update severity styling if changed
    const severity = alertData.severity || 'medium';
    const currentSeverity = this.panel.className.match(/severity-(\w+)/)?.[1];
    
    if (currentSeverity !== severity) {
      // Remove old severity class and add new one
      this.panel.classList.remove('severity-medium', 'severity-high', 'severity-critical');
      this.panel.classList.add(`severity-${severity}`);
    }
  }

  /**
   * Hide the alert panel
   */
  hideAlert() {
    // Add hide animation
    this.panel.classList.add('hide');

    // After animation completes, hide the panel
    setTimeout(() => {
      this.panel.classList.remove('show', 'hide', 'severity-medium', 'severity-high', 'severity-critical');
      this.isAlertActive = false;

      // Stop tracking cursor
      this.stopCursorTracking();
    }, 200); // Match animation duration
  }

  /**
   * Start tracking cursor position
   */
  startCursorTracking() {
    if (this.cursorTrackingInterval) {
      return; // Already tracking
    }

    // Update position at ~60fps (no initial call - already positioned in showAlert)
    this.cursorTrackingInterval = setInterval(() => {
      this.updatePanelPosition();
    }, 16); // ~60fps
  }

  /**
   * Stop tracking cursor position
   */
  stopCursorTracking() {
    if (this.cursorTrackingInterval) {
      clearInterval(this.cursorTrackingInterval);
      this.cursorTrackingInterval = null;
    }
  }

  /**
   * Update panel position based on cursor
   */
  async updatePanelPosition() {
    if (!this.isAlertActive) {
      return;
    }

    try {
      const cursorPos = await window.api.getCursorPosition();
      
      console.log(`[Overlay] Cursor position: (${cursorPos.x}, ${cursorPos.y})`);
      console.log(`[Overlay] Offset: (${this.offsetX}, ${this.offsetY})`);

      // Get screen dimensions
      const screenWidth = window.innerWidth;
      const screenHeight = window.innerHeight;

      // Get panel dimensions
      const panelRect = this.panel.getBoundingClientRect();
      const panelWidth = panelRect.width;
      const panelHeight = panelRect.height;
      
      console.log(`[Overlay] Panel size: ${panelWidth}x${panelHeight}`);

      // Calculate desired position (below and to the right of cursor)
      let x = cursorPos.x + this.offsetX;
      let y = cursorPos.y + this.offsetY;
      
      console.log(`[Overlay] Initial position: (${x}, ${y})`);

      // Clamp to screen boundaries
      // If panel would go off right edge, position to left of cursor
      if (x + panelWidth > screenWidth) {
        x = cursorPos.x - panelWidth - this.offsetX;
        console.log(`[Overlay] Adjusted for right edge: x=${x}`);
      }

      // If panel would go off bottom edge, position above cursor
      if (y + panelHeight > screenHeight) {
        y = cursorPos.y - panelHeight - this.offsetY;
        console.log(`[Overlay] Adjusted for bottom edge: y=${y}`);
      }

      // Ensure we don't go off left or top edges
      x = Math.max(0, x);
      y = Math.max(0, y);

      console.log(`[Overlay] Final position: (${x}, ${y})`);

      // Update panel position
      this.panel.style.left = `${x}px`;
      this.panel.style.top = `${y}px`;
    } catch (error) {
      console.error('Error updating panel position:', error);
    }
  }
}

// Initialize when DOM is ready
console.log('[Overlay] Setting up DOMContentLoaded listener');
document.addEventListener('DOMContentLoaded', () => {
  console.log('[Overlay] DOMContentLoaded fired, creating OverlayManager');
  new OverlayManager();
});
