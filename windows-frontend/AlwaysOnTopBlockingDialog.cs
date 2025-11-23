using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace PhishingFinder_v2
{
    /// <summary>
    /// A highly aggressive always-on-top blocking dialog that forces user attention
    /// This dialog will stay on top of all windows including fullscreen applications
    /// </summary>
    public class AlwaysOnTopBlockingDialog : Form
    {
        // Win32 API imports for window management
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        // Window position constants
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        // UI Controls
        private Label titleLabel = null!;
        private Label contentLabel = null!;
        private Button closeButton = null!;
        private Label dangerIconLabel = null!;
        private Panel borderPanel = null!;

        // Animation timers
        private Timer? fadeTimer;
        private Timer? forceTopTimer;
        private Timer? pulseTimer;

        // Animation state
        private bool isFadingIn = false;
        private bool isFadingOut = false;
        private const double fadeStep = 0.08;
        private const double targetOpacity = 1.0;
        private int pulseDirection = 1;
        private float currentPulse = 1.0f;

        // Dialog state
        private Color alertColor = Color.FromArgb(255, 100, 100); // Default to danger red
        private PhishingResponse? currentResponse;

        // Event for dialog closed
        public event EventHandler? DialogClosed;

        // Overlay form for blocking background
        private Form? overlayForm;

        public AlwaysOnTopBlockingDialog()
        {
            InitializeComponent();
            SetupTimers();
            CreateOverlayForm();
        }

        private void CreateOverlayForm()
        {
            overlayForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.5, // Semi-transparent
                TopMost = true,
                ShowInTaskbar = false,
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.Manual
            };

            // Cover all screens
            var allScreensBounds = Screen.AllScreens
                .Select(s => s.Bounds)
                .Aggregate(Rectangle.Empty, Rectangle.Union);

            overlayForm.Bounds = allScreensBounds;

            // Prevent Alt+Tab and other interactions
            overlayForm.KeyPreview = true;
            overlayForm.KeyDown += (s, e) => e.Handled = true;

            // Click on overlay focuses the alert dialog
            overlayForm.Click += (s, e) => {
                this.BringToFront();
                this.Activate();
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties - More aggressive settings
            this.Text = "⚠️ ALERTA DE SEGURIDAD CRÍTICA";
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(500, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 30);
            this.TopMost = true;
            this.ShowInTaskbar = true; // Show in taskbar for visibility
            this.ShowIcon = true;
            this.Icon = LoadApplicationIcon(); // Set application icon
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ControlBox = false; // Remove system controls
            this.Opacity = 0; // Start invisible for fade-in

            // Set form to stay on top aggressively
            this.Load += (s, e) => {
                ForceWindowToTop();
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));
            };

            // Border Panel for visual emphasis
            borderPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = this.Size,
                BackColor = Color.Transparent
            };
            borderPanel.Paint += BorderPanel_Paint;

            // Danger icon
            dangerIconLabel = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 36F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 100, 100),
                Location = new Point(20, 20),
                Size = new Size(60, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Title label
            titleLabel = new Label
            {
                Text = "🛡️ AMENAZA DETECTADA",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(90, 20),
                Size = new Size(350, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Content label
            contentLabel = new Label
            {
                Text = "Analizando amenaza...",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(90, 60),
                Size = new Size(380, 100),
                AutoSize = false,
                AutoEllipsis = true
            };

            // Close button - Large and prominent
            closeButton = new Button
            {
                Text = "CERRAR ALERTA",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(150, 40),
                Location = new Point(175, 165),
                BackColor = Color.FromArgb(255, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 100, 100);
            closeButton.Click += CloseButton_Click;

            // Emergency close X button
            Button emergencyClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(this.Width - 35, 5),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            emergencyClose.FlatAppearance.BorderSize = 0;
            emergencyClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 70);
            emergencyClose.Click += CloseButton_Click;

            // Add controls
            this.Controls.Add(dangerIconLabel);
            this.Controls.Add(titleLabel);
            this.Controls.Add(contentLabel);
            this.Controls.Add(closeButton);
            this.Controls.Add(emergencyClose);
            this.Controls.Add(borderPanel);
            borderPanel.SendToBack();

            this.ResumeLayout(false);
        }

        private void SetupTimers()
        {
            // Fade animation timer
            fadeTimer = new Timer { Interval = 10 };
            fadeTimer.Tick += FadeTimer_Tick;

            // Force top position timer - Aggressive!
            forceTopTimer = new Timer { Interval = 100 }; // Check every 100ms
            forceTopTimer.Tick += (s, e) => ForceWindowToTop();

            // Pulse animation timer for border
            pulseTimer = new Timer { Interval = 30 };
            pulseTimer.Tick += PulseTimer_Tick;
        }

        private void BorderPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw pulsing border
            using (var borderPen = new Pen(alertColor, 3.0f * currentPulse))
            {
                g.DrawRectangle(borderPen, 1, 1, this.Width - 3, this.Height - 3);
            }

            // Draw glow effect
            using (var glowBrush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, 60),
                Color.FromArgb((int)(40 * currentPulse), alertColor.R, alertColor.G, alertColor.B),
                Color.Transparent,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(glowBrush, 0, 0, this.Width, 60);
            }
        }

        private void PulseTimer_Tick(object? sender, EventArgs e)
        {
            currentPulse += 0.02f * pulseDirection;
            if (currentPulse >= 1.3f || currentPulse <= 0.7f)
            {
                pulseDirection *= -1;
            }
            borderPanel?.Invalidate();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (isFadingIn)
            {
                if (this.Opacity < targetOpacity)
                {
                    this.Opacity = Math.Min(this.Opacity + fadeStep, targetOpacity);
                }
                else
                {
                    isFadingIn = false;
                    fadeTimer?.Stop();
                }
            }
            else if (isFadingOut)
            {
                if (this.Opacity > 0)
                {
                    this.Opacity = Math.Max(this.Opacity - fadeStep, 0);
                }
                else
                {
                    isFadingOut = false;
                    fadeTimer?.Stop();
                    this.Hide();
                    overlayForm?.Hide(); // Hide overlay when dialog is hidden
                    DialogClosed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Force window to stay on top of all other windows
        /// </summary>
        private void ForceWindowToTop()
        {
            if (!this.Visible) return;

            // Force window to topmost position
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Try to steal focus (aggressive)
            if (GetForegroundWindow() != this.Handle)
            {
                uint currentThread = GetCurrentThreadId();
                uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);

                if (currentThread != foregroundThread)
                {
                    AttachThreadInput(currentThread, foregroundThread, true);
                    SetForegroundWindow(this.Handle);
                    AttachThreadInput(currentThread, foregroundThread, false);
                }
                else
                {
                    SetForegroundWindow(this.Handle);
                }
            }

            // Remove the problematic window state changes that cause flickering
            // Just bring to front and activate without minimizing/restoring
            this.BringToFront();
            this.Activate();
        }

        public void UpdatePhishingResult(PhishingResponse? response)
        {
            if (response == null) return;

            currentResponse = response;

            // Determine alert level and color
            string alertLevel;
            if (response.Scoring >= 7)
            {
                alertLevel = "🚨 PELIGRO CRÍTICO";
                alertColor = Color.FromArgb(255, 60, 60);
                titleLabel.Text = "🚨 AMENAZA CRÍTICA DETECTADA";
                dangerIconLabel.ForeColor = alertColor;
            }
            else if (response.Scoring >= 4)
            {
                alertLevel = "⚠️ ADVERTENCIA";
                alertColor = Color.FromArgb(255, 180, 60);
                titleLabel.Text = "⚠️ ADVERTENCIA DE SEGURIDAD";
                dangerIconLabel.ForeColor = alertColor;
            }
            else
            {
                alertLevel = "✓ SEGURO";
                alertColor = Color.FromArgb(100, 220, 100);
                titleLabel.Text = "✓ NAVEGACIÓN SEGURA";
                dangerIconLabel.ForeColor = alertColor;
            }

            // Update content
            string content = $"{alertLevel} - Puntuación: {response.Scoring}/10\n\n";
            content += $"Tipo de Amenaza: {response.Type ?? "Desconocido"}\n";

            if (!string.IsNullOrEmpty(response.Reason))
            {
                string reason = response.Reason.Length > 150
                    ? response.Reason.Substring(0, 150) + "..."
                    : response.Reason;
                content += $"\nDetalles: {reason}";
            }

            contentLabel.Text = content;

            // Update close button color
            if (response.Scoring >= 7)
            {
                closeButton.BackColor = Color.FromArgb(255, 60, 60);
                closeButton.Text = "ENTENDIDO - CERRAR";
            }
            else
            {
                closeButton.BackColor = Color.FromArgb(100, 150, 255);
                closeButton.Text = "CERRAR ALERTA";
            }

            // Force redraw
            borderPanel?.Invalidate();
            this.Refresh();
        }

        public new void Show()
        {
            // Show overlay first
            overlayForm?.Show();

            // Position at center of screen
            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 800, 600);
            this.Location = new Point(
                (screen.Width - this.Width) / 2,
                (screen.Height - this.Height) / 2
            );

            // Start animations
            base.Show();
            ForceWindowToTop();
            FadeIn();

            // Start aggressive top-most timer
            forceTopTimer?.Start();
            pulseTimer?.Start();

            // Play system alert sound
            System.Media.SystemSounds.Exclamation.Play();
        }

        public void FadeIn()
        {
            isFadingIn = true;
            isFadingOut = false;
            fadeTimer?.Start();
        }

        public void FadeOut()
        {
            isFadingOut = true;
            isFadingIn = false;
            fadeTimer?.Start();
            forceTopTimer?.Stop();
            pulseTimer?.Stop();
        }

        private void CloseButton_Click(object? sender, EventArgs e)
        {
            FadeOut();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                FadeOut();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fadeTimer?.Dispose();
                forceTopTimer?.Dispose();
                pulseTimer?.Dispose();
                overlayForm?.Dispose();
            }
            base.Dispose(disposing);
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
    }
}