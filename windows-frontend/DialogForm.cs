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
        private Color alertColor = Color.FromArgb(120, 120, 135); // Default border color
        private Color alertGlowColor = Color.FromArgb(70, 70, 80); // Default glow color

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
                // Try Segoe UI Variable (Windows 11) or fallback to Segoe UI
                var fontFamily = new FontFamily("Segoe UI Variable");
                titleFont = new Font(fontFamily, 10.5F, FontStyle.Bold, GraphicsUnit.Pixel);
                contentFont = new Font(fontFamily, 9F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            catch
            {
                // Fallback to Segoe UI if Variable is not available
                var fontFamily = new FontFamily("Segoe UI");
                titleFont = new Font(fontFamily, 10.5F, FontStyle.Bold, GraphicsUnit.Pixel);
                contentFont = new Font(fontFamily, 9F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            
            // Form properties - more compact size, but allow for longer text
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(40, 40, 45);
            this.Size = new Size(320, 120);
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.97;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.Padding = new Padding(16, 14, 16, 14);
            
            // Add rounded corners effect
            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, this.Width, this.Height, CornerRadius, CornerRadius)
            );
            
            // Title label - compact and modern
            titleLabel = new Label
            {
                Text = "🌐 Browser Detected",
                ForeColor = Color.FromArgb(255, 255, 255),
                Font = titleFont,
                AutoSize = true,
                Location = new Point(16, 14),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            
            // Content label - compact text, allow wrapping
            contentLabel = new Label
            {
                Text = "Monitoring active",
                ForeColor = Color.FromArgb(200, 200, 205),
                Font = contentFont,
                AutoSize = false,
                Location = new Point(16, 36),
                Size = new Size(288, 60),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            
            this.Controls.Add(titleLabel);
            this.Controls.Add(contentLabel);
            
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Draw modern gradient background - brighter and more visible
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, this.Height),
                Color.FromArgb(48, 48, 54),
                Color.FromArgb(38, 38, 44),
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
                LinearGradientMode.Vertical), 1.2f))
            {
                var borderPath = GetRoundedRectanglePath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), CornerRadius);
                g.DrawPath(pen, borderPath);
            }
            
            // Draw subtle inner highlight
            using (var pen = new Pen(Color.FromArgb(70, 70, 80), 1))
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
                contentLabel.Width = this.Width - 32; // Account for padding
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
            Color textColor;
            Color borderColor;
            Color glowColor;

            if (response.Scoring <= 3)
            {
                levelText = "SEGURO";
                textColor = Color.FromArgb(100, 200, 100);
                borderColor = Color.FromArgb(80, 180, 80);
                glowColor = Color.FromArgb(60, 160, 60);
            }
            else if (response.Scoring <= 6)
            {
                levelText = "ADVERTENCIA";
                textColor = Color.FromArgb(255, 200, 100);
                borderColor = Color.FromArgb(255, 180, 60);
                glowColor = Color.FromArgb(255, 160, 40);
            }
            else
            {
                levelText = "PELIGRO";
                textColor = Color.FromArgb(255, 100, 100);
                borderColor = Color.FromArgb(255, 80, 80);
                glowColor = Color.FromArgb(255, 60, 60);
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
            contentLabel.ForeColor = textColor;

            // Adjust form size based on content
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(displayText, contentFont, contentLabel.Width);
                int newHeight = (int)textSize.Height + 60; // Add padding
                this.Size = new Size(this.Width, Math.Max(120, Math.Min(300, newHeight)));
                contentLabel.Height = (int)textSize.Height + 10;
            }

            this.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                titleFont?.Dispose();
                contentFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

