using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace PhishingFinder_v2
{
    public partial class MainForm : Form
    {
        // Load configuration from AppSettings
        private readonly AppSettings _settings = AppSettings.Instance;

        // Use configuration values from AppSettings
        private int SCREENSHOT_INTERVAL_MS => _settings.ScreenshotIntervalMs;
        private int MOUSE_CHECK_INTERVAL_MS => _settings.MouseCheckIntervalMs;
        private int CURSOR_FOLLOW_INTERVAL_MS => _settings.CursorFollowIntervalMs;
        private int MIN_THREAT_SCORE => _settings.MinThreatScore;
        private int DANGER_THREAT_SCORE => _settings.DangerThreatScore;
        private double DIFFERENCE_THRESHOLD => _settings.FrameDifferenceThreshold;
        private int MIN_API_CALL_INTERVAL_MS => _settings.MinApiCallIntervalMs;

        private DialogForm? dialogForm;
        private AlwaysOnTopBlockingDialog? blockingDialog;
        private Timer? mouseTimer;
        private Timer? screenshotTimer;
        private Timer? cursorFollowTimer;
        private bool isProcessingScreenshot = false;
        private NotifyIcon? notifyIcon;
        private ContextMenuStrip? contextMenu;
        private UserConfig? userConfig;
        private Bitmap? lastScreenshot;
        private DateTime lastApiCallTime = DateTime.MinValue; // Track last API call time for rate limiting

        // Dialog timer management
        private DateTime lastDialogShownTime = DateTime.MinValue;
        private const int DIALOG_COOLDOWN_MS = 60000; // 1 minute cooldown between dialogs

        public MainForm()
        {
            Console.WriteLine("[MainForm] Constructor iniciado");
            InitializeComponent();
            Console.WriteLine("[MainForm] InitializeComponent completado");
            LoadUserConfiguration();
            Console.WriteLine("[MainForm] LoadUserConfiguration completado");
            InitializeSystemTray();
            Console.WriteLine("[MainForm] InitializeSystemTray completado");
            InitializeDialog();
            Console.WriteLine("[MainForm] InitializeDialog completado");
            InitializeScreenshotTimer();
            Console.WriteLine("[MainForm] InitializeScreenshotTimer completado");
            Console.WriteLine("[MainForm] Constructor finalizado - Aplicación lista");
        }

        private void LoadUserConfiguration()
        {
            Console.WriteLine("[Config] Intentando cargar configuración...");
            userConfig = UserConfig.Load();

            if (userConfig == null)
            {
                Console.WriteLine("[Config] ERROR: No se pudo cargar la configuración (null)");
                // Don't exit immediately - let the setup form handle this
                userConfig = new UserConfig(); // Create empty config
                Console.WriteLine("[Config] Configuración vacía creada temporalmente");
            }
            else if (!userConfig.IsValid())
            {
                Console.WriteLine($"[Config] ERROR: Configuración inválida - Email: {userConfig.Email}, Phone: {userConfig.PhoneNumber}");
                // Don't exit immediately - let the setup form handle this
            }
            else
            {
                Console.WriteLine($"[Config] Configuración cargada exitosamente - Email: {userConfig.Email}");
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties - hide the main window
            this.Text = "K0ra";
            this.Size = new Size(1, 1);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-2000, -2000); // Move off-screen
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0;
            this.Icon = LoadApplicationIcon(); // Set application icon
            
            this.ResumeLayout(false);
        }

        private void InitializeDialog()
        {
            Console.WriteLine("[Dialog] Inicializando diálogo...");

            // Initialize both dialog types
            dialogForm = new DialogForm();
            dialogForm.Hide(); // Always keep hidden - only show on threats
            dialogForm.DialogClosed += DialogForm_DialogClosed; // Handle dialog close event

            blockingDialog = new AlwaysOnTopBlockingDialog();
            blockingDialog.Hide(); // Always keep hidden - only show on threats
            blockingDialog.DialogClosed += BlockingDialog_DialogClosed; // Handle dialog close event

            // Set up timer to check if we're over allowed apps
            mouseTimer = new Timer();
            mouseTimer.Interval = MOUSE_CHECK_INTERVAL_MS;
            mouseTimer.Tick += MouseTimer_Tick;
            mouseTimer.Start();
            Console.WriteLine($"[Dialog] MouseTimer iniciado (intervalo: {MOUSE_CHECK_INTERVAL_MS}ms)");

            // Set up timer to follow cursor when dialog is visible (only for non-blocking dialog if NOT centered)
            // Since we're now centering the non-blocking dialog, we don't need cursor following
            cursorFollowTimer = new Timer();
            cursorFollowTimer.Interval = CURSOR_FOLLOW_INTERVAL_MS;
            cursorFollowTimer.Tick += CursorFollowTimer_Tick;
            Console.WriteLine("[Dialog] CursorFollowTimer configurado (disabled for centered dialogs)");
        }

        private void InitializeSystemTray()
        {
            // Create context menu
            contextMenu = new ContextMenuStrip();

            // Add configuration menu item
            ToolStripMenuItem configMenuItem = new ToolStripMenuItem("Configuración");
            configMenuItem.Click += ConfigMenuItem_Click;
            contextMenu.Items.Add(configMenuItem);

            // Add separator
            contextMenu.Items.Add(new ToolStripSeparator());

            // Add close menu item
            ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
            closeMenuItem.Click += CloseMenuItem_Click;
            contextMenu.Items.Add(closeMenuItem);

            // Create notify icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = LoadApplicationIcon(); // Use application logo
            notifyIcon.Text = "K0ra";
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;
        }

        private static Icon? _cachedIcon = null;
        private static readonly object _iconLock = new object();

        private static Icon LoadApplicationIcon()
        {
            // Return cached icon if available
            if (_cachedIcon != null)
                return _cachedIcon;

            lock (_iconLock)
            {
                // Double-check after acquiring lock
                if (_cachedIcon != null)
                    return _cachedIcon;

                try
                {
                    // Try to load from base directory (where exe is located)
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                    if (!File.Exists(iconPath))
                    {
                        // Try parent directory
                        iconPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName ?? "", "logo.png");
                    }
                    if (!File.Exists(iconPath))
                    {
                        // Try current directory
                        iconPath = Path.Combine(Directory.GetCurrentDirectory(), "logo.png");
                    }
                    if (!File.Exists(iconPath))
                    {
                        // Try windows-frontend directory
                        iconPath = Path.Combine(Directory.GetCurrentDirectory(), "windows-frontend", "logo.png");
                    }

                    if (File.Exists(iconPath))
                    {
                        using (var bitmap = new Bitmap(iconPath))
                        {
                            _cachedIcon = Icon.FromHandle(bitmap.GetHicon());
                            return _cachedIcon;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Icon] Error loading icon: {ex.Message}");
                }

                // Fallback to default icon
                _cachedIcon = SystemIcons.Application;
                return _cachedIcon;
            }
        }

        private void ConfigMenuItem_Click(object? sender, EventArgs e)
        {
            // Open the configuration form
            var setupForm = new SetupForm();

            // Load existing configuration into the form
            if (userConfig != null)
            {
                setupForm.LoadExistingConfiguration(userConfig);
            }

            if (setupForm.ShowDialog() == DialogResult.OK)
            {
                // Reload the configuration after saving
                LoadUserConfiguration();
                Console.WriteLine("[Config] Configuration updated from tray menu");
            }
        }

        private void CloseMenuItem_Click(object? sender, EventArgs e)
        {
            // Close the application
            Application.Exit();
        }

        private void InitializeScreenshotTimer()
        {
            Console.WriteLine("[Screenshot] Configurando ScreenshotTimer...");
            screenshotTimer = new Timer();
            screenshotTimer.Interval = SCREENSHOT_INTERVAL_MS;
            screenshotTimer.Tick += ScreenshotTimer_Tick;
            Console.WriteLine($"[Screenshot] ScreenshotTimer configurado (intervalo: {SCREENSHOT_INTERVAL_MS}ms)");
            Console.WriteLine("[Screenshot] Timer se iniciará cuando el mouse esté sobre un navegador");
        }

        private async void ScreenshotTimer_Tick(object? sender, EventArgs e)
        {
            Console.WriteLine($"[Screenshot] ► Timer Tick - Hora: {DateTime.Now:HH:mm:ss}");

            // Prevent concurrent screenshot processing
            if (isProcessingScreenshot)
            {
                Console.WriteLine("[Screenshot] ⚠ Ya hay un screenshot en proceso, saltando...");
                return;
            }

            // Don't process screenshots if any dialog is currently visible
            if ((dialogForm != null && !dialogForm.IsDisposed && dialogForm.Visible) ||
                (blockingDialog != null && !blockingDialog.IsDisposed && blockingDialog.Visible))
                return;

            // Get the browser window handle
            IntPtr browserHandle = WindowDetector.GetBrowserWindowHandle();
            Console.WriteLine($"[Screenshot] Browser Handle: {browserHandle}");

            if (browserHandle != IntPtr.Zero)
            {
                isProcessingScreenshot = true;
                Console.WriteLine("[Screenshot] Iniciando captura de screenshot...");
                
                // Stop the timer to prevent queuing multiple analyses while processing
                screenshotTimer?.Stop();
                try
                {
                    // Generate filename and capture screenshot
                    string filePath = ScreenshotCapture.GenerateScreenshotFileName();
                    Console.WriteLine($"[Screenshot] Archivo destino: {filePath}");

                    bool captured = ScreenshotCapture.CaptureWindow(browserHandle, filePath);
                    Console.WriteLine($"[Screenshot] Captura exitosa: {captured}");

                    if (captured)
                    {
                        // Load the captured screenshot for comparison
                        Bitmap? currentScreenshot = null;
                        bool shouldProcess = true;
                        
                        try
                        {
                            currentScreenshot = new Bitmap(filePath);
                            
                            // Compare with last screenshot if available
                            if (lastScreenshot != null)
                            {
                                double difference = CalculateFrameDifference(lastScreenshot, currentScreenshot);

                                if (difference < DIFFERENCE_THRESHOLD)
                                {
                                    // Frame is basically the same, skip processing
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Frame skipped - difference too small ({difference:P2} < {DIFFERENCE_THRESHOLD:P2})");
                                    shouldProcess = false;
                                }
                                else
                                {
                                    // Frame is different enough, proceed with analysis
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Frame different enough - proceeding with analysis (difference: {difference:P2})");
                                }
                            }
                            else
                            {
                                // First screenshot, no previous to compare
                                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] First frame captured - proceeding with analysis");
                            }

                            // Only process if frame is different enough
                            if (shouldProcess)
                            {
                                // Update last screenshot reference ONLY when processing
                                // Create a copy before disposing the old one
                                Bitmap? newLastScreenshot = new Bitmap(currentScreenshot);
                                lastScreenshot?.Dispose();
                                lastScreenshot = newLastScreenshot;

                                // Check rate limiting
                                var timeSinceLastCall = DateTime.Now - lastApiCallTime;
                                if (timeSinceLastCall.TotalMilliseconds < MIN_API_CALL_INTERVAL_MS)
                                {
                                    double secondsToWait = (MIN_API_CALL_INTERVAL_MS - timeSinceLastCall.TotalMilliseconds) / 1000.0;
                                    Console.WriteLine($"[Screenshot] Rate limited - skipping API call (last call {timeSinceLastCall.TotalSeconds:F1}s ago, need to wait {secondsToWait:F1}s more)");

                                    // Dispose currentScreenshot since we're not processing it
                                    currentScreenshot?.Dispose();
                                    currentScreenshot = null;

                                    // Delete the screenshot file since we're not processing it
                                    try
                                    {
                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                            Console.WriteLine($"[Screenshot] Rate-limited screenshot deleted: {Path.GetFileName(filePath)}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Screenshot] Warning: Failed to delete rate-limited screenshot: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("[Screenshot] Enviando screenshot a la API...");

                                    // Update last API call time
                                    lastApiCallTime = DateTime.Now;

                                    // Start timing the API call
                                    Stopwatch stopwatch = Stopwatch.StartNew();

                                    // Dispose currentScreenshot to release file lock before API processing
                                    currentScreenshot?.Dispose();
                                    currentScreenshot = null;

                                    // Send screenshot to API (silently, no dialog update)
                                    PhishingResponse? response = await PhishingApiClient.EvaluateScreenshotAsync(filePath);
                                
                                // Stop timing and log the elapsed time
                                stopwatch.Stop();
                                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Photo sent to API and response received in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F3}s)");

                                if (response == null)
                                {
                                    Console.WriteLine("[Screenshot] ✗ API retornó null");
                                }
                                else
                                {
                                    Console.WriteLine($"[Screenshot] ✓ API Response - Scoring: {response.Scoring}, Type: {response.Type}");
                                    Console.WriteLine($"[Screenshot] Reason: {response.Reason}");
                                }

                                // Only show dialog if threat level is Warning (4-6) or Alert (7-10)
                                if (response != null && response.Scoring >= MIN_THREAT_SCORE)
                                {
                                    Console.WriteLine($"[Screenshot] ⚠ ALERTA DETECTADA - Nivel: {response.Scoring}");

                                    // Stop screenshot timer while dialog is showing
                                    screenshotTimer?.Stop();

                                    // Update both dialogs with threat information
                                    dialogForm?.UpdatePhishingResult(response);
                                    blockingDialog?.UpdatePhishingResult(response);

                                    // Show the appropriate dialog based on user preference and threat level
                                    ShowThreatDialog(response.Scoring);

                                    // If scoring is 7 or higher (DANGER level), send email and WhatsApp alerts based on parent control settings
                                    if (response.Scoring >= DANGER_THREAT_SCORE && userConfig != null && userConfig.ParentControlEnabled)
                                    {
                                        Console.WriteLine("[Screenshot] 🚨 PELIGRO - Verificando configuración de alertas...");

                                        // Send email alert if enabled and email is configured
                                        if (userConfig.SendEmailAlerts && !string.IsNullOrWhiteSpace(userConfig.Email))
                                        {
                                            Console.WriteLine("[Screenshot] Enviando alerta por email...");
                                            _ = SendEmailAlertAsync(userConfig.Email, response.Scoring, response.Reason, response.Type);
                                        }

                                        // Send WhatsApp alert if enabled and phone is configured
                                        if (userConfig.SendPhoneAlerts && !string.IsNullOrWhiteSpace(userConfig.PhoneNumber))
                                        {
                                            Console.WriteLine("[Screenshot] Enviando alerta por WhatsApp...");
                                            _ = SendWhatsAppAlertAsync(userConfig.PhoneNumber, response.Reason);
                                        }
                                    }
                                }
                                else if (response != null)
                                {
                                    Console.WriteLine($"[Screenshot] ✓ Seguro - Scoring: {response.Scoring} (< 4)");
                                }

                                    // Delete the processed screenshot file to save disk space
                                    try
                                    {
                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                            Console.WriteLine($"[Screenshot] Processed screenshot deleted: {Path.GetFileName(filePath)}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Screenshot] Warning: Failed to delete processed screenshot: {ex.Message}");
                                    }
                                } // End of else block for rate limiting
                            }
                            else
                            {
                                // Frame is being skipped - delete the file to save disk space
                                // Dispose the current screenshot since we're not using it
                                currentScreenshot?.Dispose();
                                currentScreenshot = null;

                                try
                                {
                                    if (File.Exists(filePath))
                                    {
                                        File.Delete(filePath);
                                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Skipped frame file deleted: {Path.GetFileName(filePath)}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log but don't fail if file deletion fails
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Warning: Failed to delete skipped frame file: {ex.Message}");
                                }
                            }
                        }
                        finally
                        {
                            // Ensure currentScreenshot is disposed if something went wrong
                            currentScreenshot?.Dispose();
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Screenshot] ✗ No se pudo capturar el screenshot");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Screenshot] ✗ ERROR: {ex.Message}");
                    Console.WriteLine($"[Screenshot] Stack: {ex.StackTrace}");
                }
                finally
                {
                    isProcessingScreenshot = false;
                    Console.WriteLine("[Screenshot] ◄ Procesamiento finalizado");
                    
                    // Restart the timer after processing completes (unless any dialog is visible or not over browser)
                    if ((dialogForm == null || dialogForm.IsDisposed || !dialogForm.Visible) &&
                        (blockingDialog == null || blockingDialog.IsDisposed || !blockingDialog.Visible))
                    {
                        if (WindowDetector.IsMouseOverBrowser())
                        {
                            screenshotTimer?.Start();
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[Screenshot] ✗ No se encontró ventana de navegador");
            }
        }
        
        private double CalculateFrameDifference(Bitmap previous, Bitmap current)
        {
            // Ensure both bitmaps have the same dimensions
            if (previous.Width != current.Width || previous.Height != current.Height)
            {
                // If dimensions differ, consider it 100% different
                return 1.0;
            }

            int sampleStep = Math.Max(1, Math.Min(previous.Width, previous.Height) / 50);
            long totalDifference = 0;
            int sampleCount = 0;

            // Lock bits for fast pixel access
            var prevData = previous.LockBits(
                new Rectangle(0, 0, previous.Width, previous.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                previous.PixelFormat);
            var currData = current.LockBits(
                new Rectangle(0, 0, current.Width, current.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                current.PixelFormat);

            try
            {
                int bytesPerPixel = Image.GetPixelFormatSize(previous.PixelFormat) / 8;
                int prevStride = prevData.Stride;
                int currStride = currData.Stride;

                unsafe
                {
                    byte* prevPtr = (byte*)prevData.Scan0;
                    byte* currPtr = (byte*)currData.Scan0;

                    for (int y = 0; y < previous.Height; y += sampleStep)
                    {
                        for (int x = 0; x < previous.Width; x += sampleStep)
                        {
                            int prevIndex = y * prevStride + x * bytesPerPixel;
                            int currIndex = y * currStride + x * bytesPerPixel;

                            // Handle different pixel formats (BGR/BGRA)
                            if (bytesPerPixel >= 3)
                            {
                                long diffB = Math.Abs(prevPtr[prevIndex] - currPtr[currIndex]);
                                long diffG = Math.Abs(prevPtr[prevIndex + 1] - currPtr[currIndex + 1]);
                                long diffR = Math.Abs(prevPtr[prevIndex + 2] - currPtr[currIndex + 2]);

                                totalDifference += diffR + diffG + diffB;
                                sampleCount++;
                            }
                        }
                    }
                }
            }
            finally
            {
                previous.UnlockBits(prevData);
                current.UnlockBits(currData);
            }

            if (sampleCount == 0)
                return 0.0;

            // Normalize: average difference per pixel / max possible difference (255*3 = 765)
            double averageDifference = (double)totalDifference / sampleCount;
            return averageDifference / 765.0;
        }

        private void MouseTimer_Tick(object? sender, EventArgs e)
        {
            // Don't start screenshot timer if any dialog is currently visible
            if ((dialogForm != null && !dialogForm.IsDisposed && dialogForm.Visible) ||
                (blockingDialog != null && !blockingDialog.IsDisposed && blockingDialog.Visible))
            {
                // Ensure screenshot timer is stopped while any dialog is visible
                screenshotTimer?.Stop();
                return;
            }

            // Check if mouse is over an allowed app (browser)
            bool isOverBrowser = WindowDetector.IsMouseOverBrowser();

            // Start/stop screenshot timer based on whether we're over an allowed app
            // BUT don't stop if we're currently processing a screenshot
            if (isOverBrowser)
            {
                // Start screenshot timer if not already running and not processing
                if (screenshotTimer != null && !screenshotTimer.Enabled && !isProcessingScreenshot)
                {
                    Console.WriteLine("[Mouse] ✓ Mouse sobre navegador - Iniciando ScreenshotTimer");
                    Console.WriteLine($"[Mouse] Timer configurado para ejecutarse cada {screenshotTimer.Interval}ms (cada {screenshotTimer.Interval / 1000.0}s)");
                    screenshotTimer.Start();
                }
            }
            else
            {
                // Stop screenshot timer when not over allowed app, but NOT if we're processing
                if (!isProcessingScreenshot && screenshotTimer != null && screenshotTimer.Enabled)
                {
                    Console.WriteLine("[Mouse] ✗ Mouse NO sobre navegador - Deteniendo ScreenshotTimer");
                    screenshotTimer.Stop();
                }
                // If we're processing, let the processing complete and handle timer in finally block
            }
        }
        
        private void ShowThreatDialog(double scoring)
        {
            // For critical threats (score >= 7), use a shorter cooldown or bypass entirely
            int cooldownMs = scoring >= DANGER_THREAT_SCORE ? 15000 : DIALOG_COOLDOWN_MS; // 15 seconds for critical threats, 60 seconds otherwise

            // Check if cooldown period has passed
            if (DateTime.Now - lastDialogShownTime < TimeSpan.FromMilliseconds(cooldownMs))
            {
                Console.WriteLine($"[Dialog] Skipping dialog - cooldown active (score: {scoring}). Time remaining: {(TimeSpan.FromMilliseconds(cooldownMs) - (DateTime.Now - lastDialogShownTime)).TotalSeconds:F1}s");

                // Important: Resume screenshot timer even if dialog is not shown due to cooldown
                if (WindowDetector.IsMouseOverBrowser())
                {
                    screenshotTimer?.Start();
                }
                return;
            }

            // Update last shown time
            lastDialogShownTime = DateTime.Now;

            // Determine which dialog type to show based on scoring and user configuration
            DialogDisplayType dialogTypeToShow = DialogDisplayType.NonBlockingCentered; // Default

            if (userConfig != null)
            {
                // Use the new method to get the appropriate dialog type based on scoring
                dialogTypeToShow = userConfig.GetDialogTypeForScoring(scoring);
                Console.WriteLine($"[Dialog] Scoring: {scoring}, Selected dialog type: {dialogTypeToShow}");
            }

            // Show the appropriate dialog based on the determined type
            if (dialogTypeToShow == DialogDisplayType.AlwaysOnTopBlocking)
            {
                // Use blocking dialog
                if (blockingDialog == null || blockingDialog.IsDisposed)
                    return;

                blockingDialog.Show();
                Console.WriteLine("[Dialog] Showing AlwaysOnTopBlocking dialog");
            }
            else
            {
                // Use non-blocking centered dialog (default)
                if (dialogForm == null || dialogForm.IsDisposed)
                    return;

                // Dialog will center itself in its Show() method
                dialogForm.Show();
                Console.WriteLine("[Dialog] Showing NonBlockingCentered dialog");

                // Don't start cursor following for centered dialog
                // cursorFollowTimer is disabled for centered dialogs
            }
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

        private void BlockingDialog_DialogClosed(object? sender, EventArgs e)
        {
            // Blocking dialog was closed by user - resume screenshot analysis

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

        private async Task SendEmailAlertAsync(string email, double scoring, string reason, string type)
        {
            Console.WriteLine("[Email] Iniciando envío de alerta por email...");
            try
            {
                bool success = await PhishingApiClient.SendPhishingAlertAsync(email, scoring, reason, type);

                if (success)
                {
                    Console.WriteLine("[Email] ✓ Alerta enviada exitosamente");
                }
                else
                {
                    Console.WriteLine("[Email] ✗ Fallo al enviar alerta");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] ✗ ERROR: {ex.Message}");
            }
        }

        private async Task SendWhatsAppAlertAsync(string phoneNumber, string reason)
        {
            Console.WriteLine("[WhatsApp] Iniciando envío de notificación por WhatsApp...");
            try
            {
                bool success = await PhishingApiClient.SendWhatsAppNotificationAsync(phoneNumber, reason);

                if (success)
                {
                    Console.WriteLine("[WhatsApp] ✓ Notificación enviada exitosamente");
                }
                else
                {
                    Console.WriteLine("[WhatsApp] ✗ Fallo al enviar notificación");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsApp] ✗ ERROR: {ex.Message}");
            }
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
            blockingDialog?.Close();

            // Clean up system tray icon
            notifyIcon?.Dispose();
            contextMenu?.Dispose();
            
            // Clean up last screenshot
            lastScreenshot?.Dispose();
            
            base.OnFormClosed(e);
        }
    }
}

