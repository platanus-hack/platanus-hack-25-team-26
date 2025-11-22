using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    public partial class SetupForm : Form
    {
        private Label titleLabel = null!;
        private Label emailLabel = null!;
        private TextBox emailTextBox = null!;
        private Label phoneLabel = null!;
        private TextBox phoneTextBox = null!;
        private Label refreshTokenLabel = null!;
        private TextBox refreshTokenTextBox = null!;
        private Button saveButton = null!;
        private Label infoLabel = null!;

        public UserConfig? UserConfiguration { get; private set; }

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Configuración Inicial - Phishing Finder";
            this.Size = new Size(500, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Title label
            titleLabel = new Label
            {
                Text = "Configuración de Control Parental",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(40, 40, 50),
                Location = new Point(30, 30),
                Size = new Size(440, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Info label
            infoLabel = new Label
            {
                Text = "Por favor, ingresa tu información de contacto.\nRecibirás alertas cuando se detecte phishing.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(100, 100, 110),
                Location = new Point(30, 75),
                Size = new Size(440, 40),
                TextAlign = ContentAlignment.TopCenter
            };

            // Email label
            emailLabel = new Label
            {
                Text = "Email del padre/tutor:",
                Location = new Point(30, 130),
                Size = new Size(440, 20),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Email textbox
            emailTextBox = new TextBox
            {
                Location = new Point(30, 155),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                PlaceholderText = "ejemplo@correo.com"
            };

            // Phone label
            phoneLabel = new Label
            {
                Text = "Número de celular:",
                Location = new Point(30, 200),
                Size = new Size(440, 20),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Phone textbox
            phoneTextBox = new TextBox
            {
                Location = new Point(30, 225),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                PlaceholderText = "+56912345678"
            };

            // Refresh Token label
            refreshTokenLabel = new Label
            {
                Text = "Refresh Token de Gmail (OAuth2):",
                Location = new Point(30, 270),
                Size = new Size(440, 20),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Refresh Token textbox
            refreshTokenTextBox = new TextBox
            {
                Location = new Point(30, 295),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                PlaceholderText = "Token de refresco de Gmail"
            };

            // Save button
            saveButton = new Button
            {
                Text = "Guardar y Continuar",
                Location = new Point(30, 360),
                Size = new Size(440, 45),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;

            // Add controls to form
            this.Controls.Add(titleLabel);
            this.Controls.Add(infoLabel);
            this.Controls.Add(emailLabel);
            this.Controls.Add(emailTextBox);
            this.Controls.Add(phoneLabel);
            this.Controls.Add(phoneTextBox);
            this.Controls.Add(refreshTokenLabel);
            this.Controls.Add(refreshTokenTextBox);
            this.Controls.Add(saveButton);

            this.ResumeLayout(false);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validate email
            string email = emailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                MessageBox.Show("Por favor, ingresa un email válido.", "Error de Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                emailTextBox.Focus();
                return;
            }

            // Validate phone
            string phone = phoneTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone) || !IsValidPhone(phone))
            {
                MessageBox.Show("Por favor, ingresa un número de celular válido (ej: +56912345678).",
                    "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                phoneTextBox.Focus();
                return;
            }

            // Refresh token is optional for now
            string refreshToken = refreshTokenTextBox.Text.Trim();

            try
            {
                // Create and save configuration
                UserConfiguration = new UserConfig
                {
                    Email = email,
                    PhoneNumber = phone,
                    RefreshToken = refreshToken
                };

                UserConfiguration.Save();

                MessageBox.Show("Configuración guardada exitosamente.\n\nPhishing Finder comenzará a monitorear tu navegación.",
                    "Configuración Completa", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Simple validation for international phone numbers
            // Accepts formats like: +56912345678, 912345678, etc.
            try
            {
                var regex = new Regex(@"^\+?\d{8,15}$");
                return regex.IsMatch(phone.Replace(" ", "").Replace("-", ""));
            }
            catch
            {
                return false;
            }
        }
    }
}
