/**
 * Renderer Process Script
 * This script runs in the renderer process and can safely interact with the DOM
 */

// Wait for DOM to be fully loaded
document.addEventListener('DOMContentLoaded', () => {
  initializeApp();
});

/**
 * Initialize the application
 */
function initializeApp() {
  displaySystemInfo();
  console.log('Phishing AI application initialized successfully!');
}

/**
 * Display system information using the electronAPI
 */
function displaySystemInfo() {
  if (window.electronAPI) {
    const platformElement = document.getElementById('platform');
    const versionElement = document.getElementById('electron-version');

    if (platformElement) {
      platformElement.textContent = window.electronAPI.platform;
    }

    if (versionElement) {
      versionElement.textContent = window.electronAPI.version;
    }
  } else {
    console.error('electronAPI not available');
  }
}

