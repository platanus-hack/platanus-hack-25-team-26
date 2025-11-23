using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    internal static class Program
    {
        private static MainForm? mainForm;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Allocate a console window so Console.WriteLine works
            AllocConsole();
            Console.WriteLine("K0ra - Console logging enabled");
            Console.WriteLine("==========================================");

            // Set up application-wide exception handling
            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if user configuration exists
            if (!UserConfig.ConfigExists())
            {
                // Show setup form for first-time configuration
                using (var setupForm = new SetupForm())
                {
                    var result = setupForm.ShowDialog();

                    // If user cancels setup, exit application
                    if (result != DialogResult.OK)
                    {
                        MessageBox.Show("La configuración es requerida para usar K0ra.",
                            "Configuración Cancelada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            mainForm = new MainForm();

            // Handle form closing to ensure clean exit
            mainForm.FormClosing += MainForm_FormClosing;

            Application.Run(mainForm);
        }

        private static void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // You can add cleanup logic here if needed
            // e.Cancel = true; // Uncomment to prevent closing under certain conditions
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // Log or handle UI thread exceptions
            MessageBox.Show($"An error occurred: {e.Exception.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log or handle non-UI thread exceptions
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred: {ex.Message}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}