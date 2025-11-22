using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    public partial class DialogForm : Form
    {
        private Label titleLabel = null!;
        private Label contentLabel = null!;
        private const int CornerRadius = 12;
        private Font titleFont = null!;
        private Font contentFont = null!;
        private Color alertColor = Color.FromArgb(180, 180, 195); // Default border color - más brillante
        private Color alertGlowColor = Color.FromArgb(100, 100, 115); // Default glow color - más brillante
        
        // Animation properties
        private Timer? fadeTimer;
        private bool isFadingIn = false;
        private bool isFadingOut = false;
        private const double fadeStep = 0.05;
        private const double targetOpacity = 0.97;

        public DialogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Create modern fonts with better rendering
            try
            {
                // Try Roboto or fallback to Segoe UI
                var fontFamily = new FontFamily("Roboto");
                titleFont = new Font(fontFamily, 14F, FontStyle.Bold, GraphicsUnit.Pixel);
                contentFont = new Font(fontFamily, 12F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            catch
            {
                // Fallback to Segoe UI if Roboto is not available
                var fontFamily = new FontFamily("Segoe UI");
                titleFont = new Font(fontFamily, 14F, FontStyle.Bold, GraphicsUnit.Pixel);
                contentFont = new Font(fontFamily, 12F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            
            // Form properties - larger size for better visibility
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(30, 30, 35);
            this.Size = new Size(420, 160);
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0; // Start invisible for fade in animation
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.Padding = new Padding(12, 8, 12, 8);
            
            // Add rounded corners effect
            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, this.Width, this.Height, CornerRadius, CornerRadius)
            );
            
            // Title label - larger and more visible
            titleLabel = new Label
            {
                Text = "🌐 Browser Detected",
                ForeColor = Color.FromArgb(255, 255, 255),
                Font = titleFont,
                AutoSize = true,
                Location = new Point(12, 8),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            
            // Content label - larger text with more space
            contentLabel = new Label
            {
                Text = "Monitoring active",
                ForeColor = Color.FromArgb(204, 255, 255, 255), // Blanco con 80% opacidad
                Font = contentFont,
                AutoSize = false,
                Location = new Point(12, 34),
                Size = new Size(396, 90),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            
            this.Controls.Add(titleLabel);
            this.Controls.Add(contentLabel);
            
            // Initialize fade timer
            fadeTimer = new Timer();
            fadeTimer.Interval = 15; // ~60fps
            fadeTimer.Tick += FadeTimer_Tick;
            
            this.ResumeLayout(false);
        }

        // Import Windows API for rounded corners
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern System.IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        public void FadeIn()
        {
            if (fadeTimer == null) return;
            
            isFadingIn = true;
            isFadingOut = false;
            fadeTimer.Start();
        }

        public void FadeOut()
        {
            if (fadeTimer == null) return;
            
            isFadingIn = false;
            isFadingOut = true;
            fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (isFadingIn)
            {
                if (this.Opacity < targetOpacity)
                {
                    this.Opacity += fadeStep;
                    if (this.Opacity >= targetOpacity)
                    {
                        this.Opacity = targetOpacity;
                        fadeTimer?.Stop();
                        isFadingIn = false;
                    }
                }
            }
            else if (isFadingOut)
            {
                if (this.Opacity > 0)
                {
                    this.Opacity -= fadeStep;
                    if (this.Opacity <= 0)
                    {
                        this.Opacity = 0;
                        fadeTimer?.Stop();
                        isFadingOut = false;
                        this.Hide();
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Draw modern gradient background - darker with better contrast
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, this.Height),
                Color.FromArgb(45, 45, 52),
                Color.FromArgb(32, 32, 38),
                LinearGradientMode.Vertical))
            {
                var path = GetRoundedRectanglePath(new Rectangle(0, 0, this.Width, this.Height), CornerRadius);
                g.FillPath(brush, path);
            }
            
            // Draw subtle glow effect at top with alert color
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, 30),
                alertGlowColor,
                Color.Transparent,
                LinearGradientMode.Vertical))
            {
                var glowPath = GetRoundedRectanglePath(new Rectangle(0, 0, this.Width, 30), CornerRadius, true, false);
                g.FillPath(brush, glowPath);
            }
            
            // Draw modern border - more visible with alert color
            using (var pen = new Pen(new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, this.Height),
                alertColor,
                Color.FromArgb(Math.Max(0, alertColor.R - 40), Math.Max(0, alertColor.G - 40), Math.Max(0, alertColor.B - 40)),
                LinearGradientMode.Vertical), 2f))
            {
                var borderPath = GetRoundedRectanglePath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), CornerRadius);
                g.DrawPath(pen, borderPath);
            }
            
            // Draw subtle inner highlight
            using (var pen = new Pen(Color.FromArgb(90, 90, 100), 1))
            {
                var innerPath = GetRoundedRectanglePath(new Rectangle(1, 1, this.Width - 3, this.Height - 3), CornerRadius - 1);
                g.DrawPath(pen, innerPath);
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius, bool topOnly = false, bool bottomOnly = false)
        {
            var path = new GraphicsPath();
            
            if (topOnly)
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddLine(rect.X + radius, rect.Y, rect.Right - radius, rect.Y);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom);
                path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
                path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius);
            }
            else if (bottomOnly)
            {
                path.AddLine(rect.X, rect.Y, rect.Right, rect.Y);
                path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom - radius);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddLine(rect.Right - radius, rect.Bottom, rect.X + radius, rect.Bottom);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.AddLine(rect.X, rect.Bottom - radius, rect.X, rect.Y);
            }
            else
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddLine(rect.X + radius, rect.Y, rect.Right - radius, rect.Y);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom - radius);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddLine(rect.Right - radius, rect.Bottom, rect.X + radius, rect.Bottom);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.AddLine(rect.X, rect.Bottom - radius, rect.X, rect.Y + radius);
            }
            
            path.CloseFigure();
            return path;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Update rounded corners on resize
            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, this.Width, this.Height, CornerRadius, CornerRadius)
            );
            // Update content label size when form resizes
            if (contentLabel != null && !contentLabel.IsDisposed)
            {
                contentLabel.Width = this.Width - 24; // Account for padding
            }
        }

        public void UpdatePhishingStatus(string status, Color statusColor)
        {
            if (contentLabel != null && !contentLabel.IsDisposed)
            {
                contentLabel.Text = status;
                contentLabel.ForeColor = statusColor;
                this.Invalidate();
            }
        }

        public void UpdatePhishingResult(PhishingResponse response)
        {
            if (contentLabel == null || contentLabel.IsDisposed)
                return;

            // Determine alert level and colors based on scoring
            // 1-3: Safe (Green)
            // 4-6: Warning (Yellow/Orange)
            // 7-10: Danger (Red)
            
            string levelText;
            Color borderColor;
            Color glowColor;

            if (response.Scoring <= 3)
            {
                levelText = "SEGURO";
                borderColor = Color.FromArgb(100, 220, 100);
                glowColor = Color.FromArgb(80, 200, 80);
            }
            else if (response.Scoring <= 6)
            {
                levelText = "ADVERTENCIA";
                borderColor = Color.FromArgb(255, 200, 90);
                glowColor = Color.FromArgb(255, 180, 70);
            }
            else
            {
                levelText = "PELIGRO";
                borderColor = Color.FromArgb(255, 100, 100);
                glowColor = Color.FromArgb(255, 80, 80);
            }

            // Update colors
            alertColor = borderColor;
            alertGlowColor = glowColor;

            // Build display text
            string displayText = $"{levelText} - Puntuación: {response.Scoring}/10\n";
            if (!string.IsNullOrEmpty(response.Type))
            {
                displayText += $"Tipo: {response.Type}\n";
            }
            if (!string.IsNullOrEmpty(response.Reason))
            {
                // Truncate reason if too long
                string reason = response.Reason;
                if (reason.Length > 100)
                {
                    reason = reason.Substring(0, 97) + "...";
                }
                displayText += reason;
            }

            contentLabel.Text = displayText;
            contentLabel.ForeColor = Color.FromArgb(204, 255, 255, 255); // Blanco con 80% opacidad

            // Adjust form size based on content
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(displayText, contentFont, contentLabel.Width);
                int newHeight = (int)textSize.Height + 80; // Add padding
                this.Size = new Size(this.Width, Math.Max(160, Math.Min(380, newHeight)));
                contentLabel.Height = (int)textSize.Height + 12;
            }

            this.Invalidate();
        }

        public new void Show()
        {
            base.Show();
            FadeIn();
        }

        public void HideWithFade()
        {
            FadeOut();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                titleFont?.Dispose();
                contentFont?.Dispose();
                fadeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

