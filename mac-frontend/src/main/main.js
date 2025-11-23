const { app, BrowserWindow, Tray, Menu, screen, systemPreferences } = require('electron');
const path = require('path');
const ScreenCapture = require('./capture');
const BackendClient = require('./backend');
const PhishingLogic = require('./phishingLogic');
const IPCManager = require('./ipc');

// Global references
let overlayWindow = null;
let tray = null;
let screenCapture = null;
let backendClient = null;
let phishingLogic = null;
let ipcManager = null;

/**
 * Create transparent overlay window
 */
function createOverlayWindow() {
  const primaryDisplay = screen.getPrimaryDisplay();
  const { width, height } = primaryDisplay.bounds;

  overlayWindow = new BrowserWindow({
    width: width,
    height: height,
    x: 0,
    y: 0,
    transparent: true,
    frame: false,
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    movable: false,
    hasShadow: false,
    focusable: false,
    webPreferences: {
      preload: path.join(__dirname, '../preload/overlayPreload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  // Make window click-through by default
  overlayWindow.setIgnoreMouseEvents(true, { forward: true });

  // Load the overlay HTML
  overlayWindow.loadFile(path.join(__dirname, '../renderer/overlay/index.html'));

  // Hide from mission control and window switcher
  if (process.platform === 'darwin') {
    app.dock.hide();
  }

  // Open DevTools in development
  // if (process.env.NODE_ENV === 'development') {
  //   overlayWindow.webContents.openDevTools({ mode: 'detach' });
  // }

  overlayWindow.on('closed', () => {
    overlayWindow = null;
  });

  console.log('Overlay window created');
  return overlayWindow;
}

/**
 * Create system tray icon
 */
function createTray() {
  const { nativeImage } = require('electron');

  // Create a simple text-based icon for the tray
  const icon = nativeImage.createEmpty();
  tray = new Tray(icon);
  tray.setTitle('🛡️'); // Shield emoji as the tray icon

  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Phishing AI Protection',
      enabled: false
    },
    { type: 'separator' },
    {
      label: phishingLogic.getState() === 'capturing' ? 'Pause Monitoring' : 'Resume Monitoring',
      click: () => {
        toggleMonitoring();
      }
    },
    { type: 'separator' },
    {
      label: 'Quit',
      click: () => {
        app.quit();
      }
    }
  ]);

  tray.setToolTip('Phishing AI Protection');
  tray.setContextMenu(contextMenu);

  console.log('System tray created');
}

/**
 * Toggle monitoring on/off
 */
function toggleMonitoring() {
  if (screenCapture.isActive()) {
    screenCapture.stopCapture();
    phishingLogic.setState('idle');
    console.log('Monitoring paused');
  } else {
    startMonitoring();
    console.log('Monitoring resumed');
  }

  // Update tray menu
  createTray();
}

/**
 * Request screen recording permission on macOS
 */
async function requestScreenPermission() {
  if (process.platform === 'darwin') {
    try {
      const status = systemPreferences.getMediaAccessStatus('screen');
      console.log('Screen recording permission status:', status);

      if (status !== 'granted') {
        console.log('Requesting screen recording permission...');
        console.log('Please grant screen recording permission in System Preferences > Privacy & Security > Screen Recording');
      }
    } catch (error) {
      console.error('Error checking screen permission:', error);
    }
  }
}

/**
 * Start the monitoring system
 */
async function startMonitoring() {
  console.log('Starting phishing detection monitoring...');

  phishingLogic.setState('capturing');

  // Start screen capture with callback
  await screenCapture.startCapture(async (frameData) => {
    // Send frame to backend for analysis
    const result = await backendClient.analyzeFrame(frameData);

    // Process the result through phishing logic
    const action = phishingLogic.processResponse(result);

    if (action.showAlert) {
      // Send alert to overlay
      ipcManager.sendPhishingAlert(action.alertData);

      // Start auto-dismiss timer
      phishingLogic.startAlertTimer(() => {
        ipcManager.sendClearAlert();
      });
    }
  });
}

/**
 * Initialize the application
 */
async function initialize() {
  console.log('Initializing Phishing AI Protection...');

  // Request screen recording permission
  await requestScreenPermission();

  // Initialize modules
  screenCapture = new ScreenCapture();
  backendClient = new BackendClient();
  phishingLogic = new PhishingLogic();
  ipcManager = new IPCManager();

  // Create overlay window
  createOverlayWindow();
  ipcManager.setOverlayWindow(overlayWindow);

  // Setup IPC handlers
  ipcManager.setupHandlers();
  ipcManager.on('alertDismissed', () => {
    phishingLogic.dismissAlert();
    ipcManager.sendClearAlert();
  });

  // Create system tray
  createTray();

  // Wait for overlay window to be ready, then start monitoring
  overlayWindow.webContents.on('did-finish-load', () => {
    console.log('Overlay window loaded, starting monitoring...');
    startMonitoring();
  });
}

/**
 * App lifecycle events
 */
app.whenReady().then(() => {
  initialize();
});

// Prevent app from quitting when all windows are closed
app.on('window-all-closed', (e) => {
  e.preventDefault();
});

// Cleanup on quit
app.on('before-quit', () => {
  if (screenCapture) {
    screenCapture.stopCapture();
  }
  if (ipcManager) {
    ipcManager.cleanup();
  }
});

// Handle activate (macOS)
app.on('activate', () => {
  if (overlayWindow === null) {
    createOverlayWindow();
  }
});
