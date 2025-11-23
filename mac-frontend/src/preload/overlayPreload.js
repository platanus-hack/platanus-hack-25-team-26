const { contextBridge, ipcRenderer } = require('electron');

/**
 * Expose protected methods for overlay window
 */
contextBridge.exposeInMainWorld('api', {
  /**
   * Listen for phishing alerts
   * @param {Function} callback - Callback to handle alert data
   */
  onPhishingAlert: (callback) => {
    ipcRenderer.on('phishingAlert', (_event, data) => {
      callback(data);
    });
  },

  /**
   * Listen for alert updates (silent content updates)
   * @param {Function} callback - Callback to handle updated alert data
   */
  onUpdateAlert: (callback) => {
    ipcRenderer.on('updateAlert', (_event, data) => {
      callback(data);
    });
  },

  /**
   * Listen for clear alert events
   * @param {Function} callback - Callback when alert should be cleared
   */
  onClearAlert: (callback) => {
    ipcRenderer.on('clearAlert', () => {
      callback();
    });
  },

  /**
   * Notify main process that alert was dismissed
   */
  dismissAlert: () => {
    ipcRenderer.send('alertDismissed');
  },

  /**
   * Get current cursor position
   * @returns {Promise<{x: number, y: number}>}
   */
  getCursorPosition: async () => {
    return await ipcRenderer.invoke('getCursorPosition');
  }
});
