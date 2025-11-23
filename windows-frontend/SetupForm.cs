using System;
using System.Drawing;
using System.IO;
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
        private Button saveButton = null!;
        private Label infoLabel = null!;
        private GroupBox dialogTypeGroupBox = null!;
        private RadioButton nonBlockingRadio = null!;
        private RadioButton alwaysOnTopRadio = null!;
        private Label dialogTypeInfoLabel = null!;

        // Parent control components
        private GroupBox parentControlGroupBox = null!;
        private RadioButton parentControlEnabledRadio = null!;
        private RadioButton parentControlDisabledRadio = null!;
        private CheckBox sendEmailAlertsCheckBox = null!;
        private CheckBox sendPhoneAlertsCheckBox = null!;

        // New components for separate warning and critical dialog types
        private Panel warningPanel = null!;
        private Panel criticalPanel = null!;
        private RadioButton warningNonBlockingRadio = null!;
        private RadioButton warningBlockingRadio = null!;
        private RadioButton criticalNonBlockingRadio = null!;
        private RadioButton criticalBlockingRadio = null!;

        public UserConfig? UserConfiguration { get; private set; }

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Configuración Inicial - K0ra";
            this.Size = new Size(500, 860);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.Icon = LoadApplicationIcon(); // Set application icon

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

            // Dialog Type Group Box - Now with separate configurations for warning and critical
            dialogTypeGroupBox = new GroupBox
            {
                Text = "Configuración de Tipo de Alerta",
                Location = new Point(30, 270),
                Size = new Size(440, 260),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Dialog type info label
            dialogTypeInfoLabel = new Label
            {
                Text = "Configura cómo se mostrarán las alertas según el nivel de amenaza:",
                Location = new Point(10, 25),
                Size = new Size(420, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(100, 100, 110)
            };

            // Warning level (Scoring 4-6)
            Label warningLevelLabel = new Label
            {
                Text = "⚠️ Advertencia (Puntuación 4-6):",
                Location = new Point(10, 50),
                Size = new Size(420, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(255, 180, 60)
            };

            // Warning panel to group warning radio buttons
            warningPanel = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(400, 35),
                BackColor = Color.Transparent
            };

            // Warning non-blocking radio button
            warningNonBlockingRadio = new RadioButton
            {
                Text = "No Bloqueante",
                Location = new Point(10, 5),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Checked = true, // Default option for warnings
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Warning blocking radio button
            warningBlockingRadio = new RadioButton
            {
                Text = "Bloqueante",
                Location = new Point(200, 5),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Add radio buttons to warning panel
            warningPanel.Controls.Add(warningNonBlockingRadio);
            warningPanel.Controls.Add(warningBlockingRadio);

            // Critical level (Scoring 7-10)
            Label criticalLevelLabel = new Label
            {
                Text = "🚨 Peligro Crítico (Puntuación 7-10):",
                Location = new Point(10, 110),
                Size = new Size(420, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(255, 60, 60)
            };

            // Critical panel to group critical radio buttons
            criticalPanel = new Panel
            {
                Location = new Point(20, 130),
                Size = new Size(400, 35),
                BackColor = Color.Transparent
            };

            // Critical non-blocking radio button
            criticalNonBlockingRadio = new RadioButton
            {
                Text = "No Bloqueante",
                Location = new Point(10, 5),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Critical blocking radio button
            criticalBlockingRadio = new RadioButton
            {
                Text = "Bloqueante",
                Location = new Point(200, 5),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Checked = true, // Default option for critical
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Add radio buttons to critical panel
            criticalPanel.Controls.Add(criticalNonBlockingRadio);
            criticalPanel.Controls.Add(criticalBlockingRadio);

            // Add hint text for radio options
            Label hintLabel = new Label
            {
                Text = "• No Bloqueante: Alerta en el centro de la pantalla, puedes cerrarla y continuar\n• Bloqueante: Alerta siempre visible que fuerza tu atención hasta cerrarla\n\nRecomendamos usar alertas bloqueantes para amenazas críticas.",
                Location = new Point(20, 170),
                Size = new Size(400, 75),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(120, 120, 130)
            };

            // Keep old radio buttons for backward compatibility (hidden)
            nonBlockingRadio = new RadioButton
            {
                Visible = false,
                Checked = true
            };
            alwaysOnTopRadio = new RadioButton
            {
                Visible = false
            };

            // Add controls to group box
            dialogTypeGroupBox.Controls.Add(dialogTypeInfoLabel);
            dialogTypeGroupBox.Controls.Add(warningLevelLabel);
            dialogTypeGroupBox.Controls.Add(warningPanel);
            dialogTypeGroupBox.Controls.Add(criticalLevelLabel);
            dialogTypeGroupBox.Controls.Add(criticalPanel);
            dialogTypeGroupBox.Controls.Add(hintLabel);
            dialogTypeGroupBox.Controls.Add(nonBlockingRadio);
            dialogTypeGroupBox.Controls.Add(alwaysOnTopRadio);

            // Parent Control Group Box
            parentControlGroupBox = new GroupBox
            {
                Text = "Control Parental",
                Location = new Point(30, 540),
                Size = new Size(440, 180),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(60, 60, 70)
            };

            // Parent control enabled radio
            parentControlEnabledRadio = new RadioButton
            {
                Text = "Habilitar control parental",
                Location = new Point(20, 30),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Checked = true, // Default enabled
                ForeColor = Color.FromArgb(60, 60, 70)
            };
            parentControlEnabledRadio.CheckedChanged += ParentControlRadio_CheckedChanged;

            // Parent control disabled radio
            parentControlDisabledRadio = new RadioButton
            {
                Text = "Deshabilitar control parental (sin notificaciones)",
                Location = new Point(20, 60),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(60, 60, 70)
            };
            parentControlDisabledRadio.CheckedChanged += ParentControlRadio_CheckedChanged;

            // Send email alerts checkbox
            sendEmailAlertsCheckBox = new CheckBox
            {
                Text = "Enviar alertas por email cuando se detecte phishing",
                Location = new Point(40, 95),
                Size = new Size(380, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Checked = true,
                ForeColor = Color.FromArgb(60, 60, 70)
            };
            sendEmailAlertsCheckBox.CheckedChanged += SendAlertsCheckBox_CheckedChanged;

            // Send phone alerts checkbox
            sendPhoneAlertsCheckBox = new CheckBox
            {
                Text = "Enviar alertas por WhatsApp cuando se detecte phishing",
                Location = new Point(40, 125),
                Size = new Size(380, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Checked = true,
                ForeColor = Color.FromArgb(60, 60, 70)
            };
            sendPhoneAlertsCheckBox.CheckedChanged += SendAlertsCheckBox_CheckedChanged;

            // Add controls to parent control group box
            parentControlGroupBox.Controls.Add(parentControlEnabledRadio);
            parentControlGroupBox.Controls.Add(parentControlDisabledRadio);
            parentControlGroupBox.Controls.Add(sendEmailAlertsCheckBox);
            parentControlGroupBox.Controls.Add(sendPhoneAlertsCheckBox);

            // Save button
            saveButton = new Button
            {
                Text = "Guardar y Continuar",
                Location = new Point(30, 740),
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
            this.Controls.Add(dialogTypeGroupBox);
            this.Controls.Add(parentControlGroupBox);
            this.Controls.Add(saveButton);

            this.ResumeLayout(false);
        }

        private void ParentControlRadio_CheckedChanged(object? sender, EventArgs e)
        {
            bool parentControlEnabled = parentControlEnabledRadio.Checked;

            // Enable/disable email and phone fields based on parent control
            sendEmailAlertsCheckBox.Enabled = parentControlEnabled;
            sendPhoneAlertsCheckBox.Enabled = parentControlEnabled;

            UpdateFieldStates();
        }

        private void SendAlertsCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateFieldStates();
        }

        private void UpdateFieldStates()
        {
            bool parentControlEnabled = parentControlEnabledRadio.Checked;

            // Enable email field only if parent control is enabled and email alerts are enabled
            bool emailFieldEnabled = parentControlEnabled && sendEmailAlertsCheckBox.Checked;
            emailTextBox.Enabled = emailFieldEnabled;
            emailLabel.ForeColor = emailFieldEnabled ? Color.FromArgb(60, 60, 70) : Color.FromArgb(150, 150, 160);

            // Enable phone field only if parent control is enabled and phone alerts are enabled
            bool phoneFieldEnabled = parentControlEnabled && sendPhoneAlertsCheckBox.Checked;
            phoneTextBox.Enabled = phoneFieldEnabled;
            phoneLabel.ForeColor = phoneFieldEnabled ? Color.FromArgb(60, 60, 70) : Color.FromArgb(150, 150, 160);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            string email = emailTextBox.Text.Trim();
            string phone = phoneTextBox.Text.Trim();
            bool parentControlEnabled = parentControlEnabledRadio.Checked;
            bool sendEmailAlerts = sendEmailAlertsCheckBox.Checked;
            bool sendPhoneAlerts = sendPhoneAlertsCheckBox.Checked;

            // Validate based on parent control settings
            if (parentControlEnabled)
            {
                // If email alerts are enabled, validate email
                if (sendEmailAlerts)
                {
                    if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                    {
                        MessageBox.Show("Por favor, ingresa un email válido para recibir alertas.",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        emailTextBox.Focus();
                        return;
                    }
                }

                // If phone alerts are enabled, validate phone
                if (sendPhoneAlerts)
                {
                    if (string.IsNullOrWhiteSpace(phone) || !IsValidPhone(phone))
                    {
                        MessageBox.Show("Por favor, ingresa un número de celular válido para recibir alertas (ej: +56912345678).",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        phoneTextBox.Focus();
                        return;
                    }
                }

                // At least one alert method must be enabled
                if (!sendEmailAlerts && !sendPhoneAlerts)
                {
                    MessageBox.Show("Por favor, habilita al menos un método de notificación (email o WhatsApp).",
                        "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                // Determine selected dialog types for warning and critical alerts
                DialogDisplayType warningType = warningNonBlockingRadio.Checked
                    ? DialogDisplayType.NonBlockingCentered
                    : DialogDisplayType.AlwaysOnTopBlocking;

                DialogDisplayType criticalType = criticalNonBlockingRadio.Checked
                    ? DialogDisplayType.NonBlockingCentered
                    : DialogDisplayType.AlwaysOnTopBlocking;

                // Legacy dialog type (use warning type as default for backward compatibility)
                DialogDisplayType selectedType = warningType;

                // Create and save configuration
                UserConfiguration = new UserConfig
                {
                    Email = sendEmailAlerts ? email : string.Empty,
                    PhoneNumber = sendPhoneAlerts ? phone : string.Empty,
                    DialogType = selectedType, // Legacy property for backward compatibility
                    WarningDialogType = warningType,
                    CriticalDialogType = criticalType,
                    ParentControlEnabled = parentControlEnabled,
                    SendEmailAlerts = sendEmailAlerts,
                    SendPhoneAlerts = sendPhoneAlerts
                };

                UserConfiguration.Save();

                string message = parentControlEnabled
                    ? "Configuración guardada exitosamente.\n\nK0ra comenzará a monitorear tu navegación con control parental activado."
                    : "Configuración guardada exitosamente.\n\nK0ra comenzará a monitorear tu navegación sin notificaciones parentales.";

                MessageBox.Show(message, "Configuración Completa", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        /// <summary>
        /// Loads an existing configuration into the form for editing
        /// </summary>
        public void LoadExistingConfiguration(UserConfig config)
        {
            if (config == null) return;

            // Load email and phone
            emailTextBox.Text = config.Email;
            phoneTextBox.Text = config.PhoneNumber;

            // Load parent control settings
            parentControlEnabledRadio.Checked = config.ParentControlEnabled;
            parentControlDisabledRadio.Checked = !config.ParentControlEnabled;
            sendEmailAlertsCheckBox.Checked = config.SendEmailAlerts;
            sendPhoneAlertsCheckBox.Checked = config.SendPhoneAlerts;

            // Load warning dialog type
            if (config.WarningDialogType == DialogDisplayType.NonBlockingCentered)
            {
                warningNonBlockingRadio.Checked = true;
                warningBlockingRadio.Checked = false;
            }
            else
            {
                warningNonBlockingRadio.Checked = false;
                warningBlockingRadio.Checked = true;
            }

            // Load critical dialog type
            if (config.CriticalDialogType == DialogDisplayType.NonBlockingCentered)
            {
                criticalNonBlockingRadio.Checked = true;
                criticalBlockingRadio.Checked = false;
            }
            else
            {
                criticalNonBlockingRadio.Checked = false;
                criticalBlockingRadio.Checked = true;
            }

            // Update field states based on loaded configuration
            UpdateFieldStates();
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
