using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    public partial class MainForm : Form
    {
        private DialogForm? dialogForm;
        private Timer? mouseTimer;
        private Timer? screenshotTimer;
        private Timer? cursorFollowTimer;
        private bool isProcessingScreenshot = false;
        private NotifyIcon? notifyIcon;
        private ContextMenuStrip? contextMenu;

        public MainForm()
        {
            InitializeComponent();
            InitializeSystemTray();
            InitializeDialog();
            InitializeScreenshotTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties - hide the main window
            this.Text = "Phishing Finder";
            this.Size = new Size(1, 1);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-2000, -2000); // Move off-screen
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0;
            
            this.ResumeLayout(false);
        }

        private void InitializeDialog()
        {
            dialogForm = new DialogForm();
            dialogForm.Hide(); // Always keep hidden - only show on threats
            dialogForm.DialogClosed += DialogForm_DialogClosed; // Handle dialog close event
            
            // Set up timer to check if we're over allowed apps
            mouseTimer = new Timer();
            mouseTimer.Interval = 1000; // Check every second
            mouseTimer.Tick += MouseTimer_Tick;
            mouseTimer.Start();
            
            // Set up timer to follow cursor when dialog is visible
            cursorFollowTimer = new Timer();
            cursorFollowTimer.Interval = 50; // Update every 50ms for smooth following
            cursorFollowTimer.Tick += CursorFollowTimer_Tick;
        }

        private void InitializeSystemTray()
        {
            // Create context menu
            contextMenu = new ContextMenuStrip();
            ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
            closeMenuItem.Click += CloseMenuItem_Click;
            contextMenu.Items.Add(closeMenuItem);

            // Create notify icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application; // Use default application icon
            notifyIcon.Text = "Phishing Finder";
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;
        }

        private void CloseMenuItem_Click(object? sender, EventArgs e)
        {
            // Close the application
            Application.Exit();
        }

        private void InitializeScreenshotTimer()
        {
            screenshotTimer = new Timer();
            screenshotTimer.Interval = 5000; // 5 seconds
            screenshotTimer.Tick += ScreenshotTimer_Tick;
            // Timer will be started/stopped based on whether we're over an allowed app
        }

        private async void ScreenshotTimer_Tick(object? sender, EventArgs e)
        {
            // Prevent concurrent screenshot processing
            if (isProcessingScreenshot)
                return;

            // Get the browser window handle
            IntPtr browserHandle = WindowDetector.GetBrowserWindowHandle();
            
            if (browserHandle != IntPtr.Zero)
            {
                isProcessingScreenshot = true;
                
                try
                {
                    // Generate filename and capture screenshot
                    string filePath = ScreenshotCapture.GenerateScreenshotFileName();
                    bool captured = ScreenshotCapture.CaptureWindow(browserHandle, filePath);
                    
                    if (captured)
                    {
                        // Send screenshot to API (silently, no dialog update)
                        PhishingResponse? response = await PhishingApiClient.EvaluateScreenshotAsync(filePath);
                        
                        // Only show dialog if threat level is Warning (4-6) or Alert (7-10)
                        if (response != null && response.Scoring >= 4)
                        {
                            // Stop screenshot timer while dialog is showing
                            screenshotTimer?.Stop();
                            
                            // Update dialog with threat information
                            dialogForm?.UpdatePhishingResult(response);
                            
                            // Show dialog and position it near mouse cursor
                            ShowThreatDialog();
                        }
                    }
                }
                finally
                {
                    isProcessingScreenshot = false;
                }
            }
        }

        private void MouseTimer_Tick(object? sender, EventArgs e)
        {
            // Check if mouse is over an allowed app (browser)
            bool isOverBrowser = WindowDetector.IsMouseOverBrowser();
            
            // Start/stop screenshot timer based on whether we're over an allowed app
            if (isOverBrowser)
            {
                // Start screenshot timer if not already running
                if (screenshotTimer != null && !screenshotTimer.Enabled)
                {
                    screenshotTimer.Start();
                }
            }
            else
            {
                // Stop screenshot timer when not over allowed app
                screenshotTimer?.Stop();
            }
        }
        
        private void ShowThreatDialog()
        {
            if (dialogForm == null || dialogForm.IsDisposed)
                return;
            
            // Update dialog position and show it
            UpdateDialogPosition();
            dialogForm.Show();
            
            // Start cursor following timer
            cursorFollowTimer?.Start();
        }
        
        private void DialogForm_DialogClosed(object? sender, EventArgs e)
        {
            // Dialog was closed by user - resume screenshot analysis
            cursorFollowTimer?.Stop();
            
            // Resume screenshot timer if we're still over browser
            if (WindowDetector.IsMouseOverBrowser())
            {
                // Restart screenshot timer to resume analysis
                screenshotTimer?.Stop();
                screenshotTimer?.Start();
            }
        }
        
        private void CursorFollowTimer_Tick(object? sender, EventArgs e)
        {
            // Update dialog position to follow cursor when visible
            // Only update if dialog is visible and not being interacted with
            if (dialogForm != null && !dialogForm.IsDisposed && dialogForm.Visible)
            {
                // Check if mouse is over the dialog (to avoid moving it while user is trying to click)
                var mousePos = Control.MousePosition;
                var dialogScreenPos = dialogForm.PointToScreen(Point.Empty);
                var dialogRect = new Rectangle(dialogScreenPos, dialogForm.Size);
                
                // Only update position if mouse is not over the dialog
                // This prevents the dialog from moving away when user tries to click the close button
                if (!dialogRect.Contains(mousePos))
                {
                    UpdateDialogPosition();
                }
            }
        }
        
        private void UpdateDialogPosition()
        {
            if (dialogForm == null || dialogForm.IsDisposed)
                return;
            
            // Position dialog near mouse cursor
            var mousePos = Control.MousePosition;
            var screenBounds = Screen.FromPoint(mousePos).Bounds;
            
            // Calculate offset for bottom-right positioning
            const int offsetX = 15; // Offset from cursor
            const int offsetY = 15;
            
            int dialogX = mousePos.X + offsetX;
            int dialogY = mousePos.Y + offsetY;
            
            // Check if dialog would go off screen at bottom
            if (dialogY + dialogForm.Height > screenBounds.Bottom)
            {
                // Position at top-right instead
                dialogY = mousePos.Y - dialogForm.Height - offsetY;
                
                // If still off screen at top, position at top of screen
                if (dialogY < screenBounds.Top)
                {
                    dialogY = screenBounds.Top + 5;
                }
            }
            
            // Check if dialog would go off screen on the right
            if (dialogX + dialogForm.Width > screenBounds.Right)
            {
                dialogX = screenBounds.Right - dialogForm.Width - 5;
            }
            
            // Check if dialog would go off screen on the left
            if (dialogX < screenBounds.Left)
            {
                dialogX = screenBounds.Left + 5;
            }
            
            // Update dialog position
            dialogForm.Location = new Point(dialogX, dialogY);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            mouseTimer?.Stop();
            mouseTimer?.Dispose();
            screenshotTimer?.Stop();
            screenshotTimer?.Dispose();
            cursorFollowTimer?.Stop();
            cursorFollowTimer?.Dispose();
            dialogForm?.Close();
            
            // Clean up system tray icon
            notifyIcon?.Dispose();
            contextMenu?.Dispose();
            
            base.OnFormClosed(e);
        }
    }
}

