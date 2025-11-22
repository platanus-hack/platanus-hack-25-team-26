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
        private Timer? hideDialogTimer;
        private bool isProcessingScreenshot = false;

        public MainForm()
        {
            InitializeComponent();
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
            
            // Set up timer to check if we're over allowed apps
            mouseTimer = new Timer();
            mouseTimer.Interval = 1000; // Check every second
            mouseTimer.Tick += MouseTimer_Tick;
            mouseTimer.Start();
            
            // Set up timer to auto-hide dialog after 5 seconds
            hideDialogTimer = new Timer();
            hideDialogTimer.Interval = 5000; // 5 seconds
            hideDialogTimer.Tick += HideDialogTimer_Tick;
        }

        private void InitializeScreenshotTimer()
        {
            screenshotTimer = new Timer();
            screenshotTimer.Interval = 20000; // 20 seconds
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
                            // Update dialog with threat information
                            dialogForm?.UpdatePhishingResult(response);
                            
                            // Show dialog and position it near mouse cursor
                            ShowThreatDialog();
                            
                            // Start timer to hide dialog after 5 seconds
                            hideDialogTimer?.Stop();
                            hideDialogTimer?.Start();
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
            
            // Update dialog position and show it
            dialogForm.Location = new Point(dialogX, dialogY);
            dialogForm.Show();
        }
        
        private void HideDialogTimer_Tick(object? sender, EventArgs e)
        {
            // Hide dialog after 5 seconds
            hideDialogTimer?.Stop();
            dialogForm?.Hide();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            mouseTimer?.Stop();
            mouseTimer?.Dispose();
            screenshotTimer?.Stop();
            screenshotTimer?.Dispose();
            hideDialogTimer?.Stop();
            hideDialogTimer?.Dispose();
            dialogForm?.Close();
            base.OnFormClosed(e);
        }
    }
}

