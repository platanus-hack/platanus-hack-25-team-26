
class IPCManager {
  constructor() {
    this.overlayWindow = null;
    this.handlers = new Map();
  }

  /**
   * Set the overlay window reference
   * @param {BrowserWindow} window - The overlay window
   */
  setOverlayWindow(window) {
    this.overlayWindow = window;
  }

  /**
   * Setup IPC handlers
   */
  setupHandlers() {
    const { ipcMain } = require('electron');

    // Handle alert dismissed from renderer
    ipcMain.on('alertDismissed', (event) => {
      console.log('Alert dismissed by user');
      if (this.handlers.has('alertDismissed')) {
        this.handlers.get('alertDismissed')();
      }
    });

    // Handle cursor position requests
    ipcMain.handle('getCursorPosition', async () => {
      try {
        const { screen } = require('electron');
        const point = screen.getCursorScreenPoint();
        
        // Get overlay window bounds to convert screen coords to window coords
        const overlayWindow = require('electron').BrowserWindow.getAllWindows()
          .find(w => w.getURL().includes('overlay/index.html'));
        
        if (overlayWindow) {
          const bounds = overlayWindow.getBounds();
          // console.log(`[IPC] Screen cursor: (${point.x}, ${point.y}), Window bounds: (${bounds.x}, ${bounds.y})`);
          
          // Convert screen coordinates to window coordinates
          return { 
            x: point.x - bounds.x, 
            y: point.y - bounds.y 
          };
        }
        
        return { x: point.x, y: point.y };
      } catch (error) {
        console.error('Error getting cursor position:', error);
        return { x: 0, y: 0 };
      }
    });

    console.log('IPC handlers registered');
  }

  /**
   * Register a handler for a specific event
   * @param {string} event - Event name
   * @param {Function} handler - Handler function
   */
  on(event, handler) {
    this.handlers.set(event, handler);
  }

  /**
   * Send phishing alert to overlay window
   * @param {Object} alertData - Alert data to send
   */
  sendPhishingAlert(alertData) {
    if (this.overlayWindow && !this.overlayWindow.isDestroyed()) {
      this.overlayWindow.webContents.send('phishingAlert', alertData);
      console.log('Phishing alert sent to overlay:', alertData);
    }
  }

  /**
   * Send alert update to overlay window (silent update)
   * @param {Object} alertData - Updated alert data
   */
  sendUpdateAlert(alertData) {
    if (this.overlayWindow && !this.overlayWindow.isDestroyed()) {
      this.overlayWindow.webContents.send('updateAlert', alertData);
      console.log('Alert update sent to overlay');
    }
  }

  /**
   * Send clear alert to overlay window
   */
  sendClearAlert() {
    if (this.overlayWindow && !this.overlayWindow.isDestroyed()) {
      this.overlayWindow.webContents.send('clearAlert');
      console.log('Clear alert sent to overlay');
    }
  }

  /**
   * Cleanup IPC handlers
   */
  cleanup() {
    const { ipcMain } = require('electron');
    ipcMain.removeAllListeners('alertDismissed');
    ipcMain.removeHandler('getCursorPosition');
    this.handlers.clear();
  }
}

module.exports = IPCManager;
