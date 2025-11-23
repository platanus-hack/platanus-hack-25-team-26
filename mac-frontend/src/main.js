const { app, BrowserWindow, Tray, Menu, screen, systemPreferences } = require('electron');
const path = require('path');
const ScreenCapture = require('./main/capture');
const BackendClient = require('./main/backend');
const PhishingLogic = require('./main/phishingLogic');
const IPCManager = require('./main/ipc');

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
      preload: path.join(__dirname, 'preload/overlayPreload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  // Make window click-through by default
  overlayWindow.setIgnoreMouseEvents(true, { forward: true });

  // Load the overlay HTML
  overlayWindow.loadFile(path.join(__dirname, 'renderer/overlay/index.html'));

  // Hide from mission control and window switcher
  if (process.platform === 'darwin') {
    app.dock.hide();
  }

  // Open DevTools to see overlay console
  // overlayWindow.webContents.openDevTools({ mode: 'detach' });

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

  // Window change callback - fires immediately (every 500ms)
  const onWindowChange = (newWindow) => {
    console.log('[Main] Window changed immediately, clearing alert and resetting state');
    
    // Hide alert in UI
    ipcManager.sendClearAlert();
    
    // Reset phishingLogic state completely for new window
    phishingLogic.isAlertVisible = false;
    phishingLogic.currentAlert = null;
    phishingLogic.originalAlertScore = null;
    phishingLogic.lastWindowKey = `${newWindow.app}|||${newWindow.title}`;
    
    // Reset adaptive timing - each window starts fresh at normal speed
    screenCapture.isRisky = false;
    screenCapture.safeFrameCount = 0;
    screenCapture.captureIntervalMs = screenCapture.normalIntervalMs;
  };

  // Start screen capture with immediate window change detection
  await screenCapture.startCapture(
    // Frame capture callback
    async (frameData) => {
      console.log('Frame captured, analyzing...');
      
      // Send frame to backend for analysis
      const result = await backendClient.analyzeFrame(frameData);
      console.log('Analysis complete:', result);

      // Get CURRENT window (not stale frameData window)
      // The user may have switched windows while API was processing
      const currentWindow = screenCapture.lastFocusedWindow ? {
        title: screenCapture.lastFocusedWindow.title,
        app: screenCapture.lastFocusedWindow.appName
      } : null;

      // Check if window changed during API call - discard stale data
      const capturedWindow = frameData.focusedWindow;
      if (capturedWindow && currentWindow && capturedWindow.app !== currentWindow.app) {
        console.log(`[Main] Discarding stale data: captured from "${capturedWindow.app}", now on "${currentWindow.app}"`);
        return; // Don't process this frame - it's from a different window
      }

      // Process the result through phishing logic with CURRENT window context
      const actionResult = phishingLogic.processResponse(result, currentWindow);

      // OPTIMIZATION 2: Adaptive timing based on risk level
      const isRisky = result.scoring >= 3;
      screenCapture.updateCaptureInterval(isRisky);

      // Handle different action types
      switch (actionResult.action) {
        case 'SHOW_NEW':
          // Show new alert immediately
          console.log('[Main] Sending SHOW_NEW to overlay');
          ipcManager.sendPhishingAlert(actionResult.alertData);
          console.log('[Main] IPC message sent');
          break;

        case 'UPDATE':
          // Update existing alert silently (scoring changed by 2+ points)
          console.log('Updating existing alert');
          ipcManager.sendUpdateAlert(actionResult.alertData);
          break;

        case 'HIDE':
          // Hide alert smoothly
          console.log('Hiding alert');
          ipcManager.sendClearAlert();
          break;

        case 'NOTHING':
          // Do nothing - no alert needed
          break;

        default:
          console.warn('Unknown action type:', actionResult.action);
      }
    },
    onWindowChange // Pass window change callback for immediate detection
  );
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
