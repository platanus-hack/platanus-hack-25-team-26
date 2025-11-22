using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

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
        private UserConfig? userConfig;

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
            Console.WriteLine("[Dialog] Inicializando diálogo...");
            dialogForm = new DialogForm();
            dialogForm.Hide(); // Always keep hidden - only show on threats
            dialogForm.DialogClosed += DialogForm_DialogClosed; // Handle dialog close event

            // Set up timer to check if we're over allowed apps
            mouseTimer = new Timer();
            mouseTimer.Interval = 1000; // Check every second
            mouseTimer.Tick += MouseTimer_Tick;
            mouseTimer.Start();
            Console.WriteLine("[Dialog] MouseTimer iniciado (intervalo: 1000ms)");

            // Set up timer to follow cursor when dialog is visible
            cursorFollowTimer = new Timer();
            cursorFollowTimer.Interval = 50; // Update every 50ms for smooth following
            cursorFollowTimer.Tick += CursorFollowTimer_Tick;
            Console.WriteLine("[Dialog] CursorFollowTimer configurado");
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
            Console.WriteLine("[Screenshot] Configurando ScreenshotTimer...");
            screenshotTimer = new Timer();
            screenshotTimer.Interval = 5000; // 5 seconds
            screenshotTimer.Tick += ScreenshotTimer_Tick;
            Console.WriteLine("[Screenshot] ScreenshotTimer configurado (intervalo: 5000ms)");
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

            // Get the browser window handle
            IntPtr browserHandle = WindowDetector.GetBrowserWindowHandle();
            Console.WriteLine($"[Screenshot] Browser Handle: {browserHandle}");

            if (browserHandle != IntPtr.Zero)
            {
                isProcessingScreenshot = true;
                Console.WriteLine("[Screenshot] Iniciando captura de screenshot...");

                try
                {
                    // Generate filename and capture screenshot
                    string filePath = ScreenshotCapture.GenerateScreenshotFileName();
                    Console.WriteLine($"[Screenshot] Archivo destino: {filePath}");

                    bool captured = ScreenshotCapture.CaptureWindow(browserHandle, filePath);
                    Console.WriteLine($"[Screenshot] Captura exitosa: {captured}");

                    if (captured)
                    {
                        Console.WriteLine("[Screenshot] Enviando screenshot a la API...");
                        // Send screenshot to API (silently, no dialog update)
                        PhishingResponse? response = await PhishingApiClient.EvaluateScreenshotAsync(filePath);

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
                        if (response != null && response.Scoring >= 4)
                        {
                            Console.WriteLine($"[Screenshot] ⚠ ALERTA DETECTADA - Nivel: {response.Scoring}");

                            // Stop screenshot timer while dialog is showing
                            screenshotTimer?.Stop();

                            // Update dialog with threat information
                            dialogForm?.UpdatePhishingResult(response);

                            // Show dialog and position it near mouse cursor
                            ShowThreatDialog();

                            // If scoring is 7 or higher (DANGER level), send email and WhatsApp alerts immediately
                            if (response.Scoring >= 7 && userConfig != null && !string.IsNullOrWhiteSpace(userConfig.RefreshToken))
                            {
                                Console.WriteLine("[Screenshot] 🚨 PELIGRO - Enviando alertas por email y WhatsApp...");
                                _ = SendEmailAlertAsync(userConfig.RefreshToken, response.Scoring, response.Reason, response.Type);
                                _ = SendWhatsAppAlertAsync(userConfig.PhoneNumber, response.Reason);
                            }
                        }
                        else if (response != null)
                        {
                            Console.WriteLine($"[Screenshot] ✓ Seguro - Scoring: {response.Scoring} (< 4)");
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
                }
            }
            else
            {
                Console.WriteLine("[Screenshot] ✗ No se encontró ventana de navegador");
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
                    Console.WriteLine("[Mouse] ✓ Mouse sobre navegador - Iniciando ScreenshotTimer");
                    Console.WriteLine($"[Mouse] Timer configurado para ejecutarse cada {screenshotTimer.Interval}ms (cada {screenshotTimer.Interval / 1000.0}s)");
                    screenshotTimer.Start();
                }
            }
            else
            {
                // Stop screenshot timer when not over allowed app
                if (screenshotTimer != null && screenshotTimer.Enabled)
                {
                    Console.WriteLine("[Mouse] ✗ Mouse NO sobre navegador - Deteniendo ScreenshotTimer");
                    screenshotTimer.Stop();
                }
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

        private async Task SendEmailAlertAsync(string refreshToken, double scoring, string reason, string type)
        {
            Console.WriteLine("[Email] Iniciando envío de alerta por email...");
            try
            {
                bool success = await PhishingApiClient.SendPhishingAlertAsync(refreshToken, scoring, reason, type);

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

            // Clean up system tray icon
            notifyIcon?.Dispose();
            contextMenu?.Dispose();

            base.OnFormClosed(e);
        }
    }
}

