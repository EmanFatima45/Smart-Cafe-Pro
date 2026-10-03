// ============================================================
// SmartCafe Pro - Program.cs (Entry Point)
// ============================================================
using System;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.DataAccessLayer;
using SmartCafePro.Services;
using SmartCafePro.Utilities;

namespace SmartCafePro
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Global exception handler
            Application.ThreadException += (s, e) =>
                MessageHelper.ShowError($"Unexpected error:\n{e.Exception.Message}", "Application Error");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageHelper.ShowError($"Fatal error:\n{((Exception)e.ExceptionObject).Message}", "Fatal Error");

            // Initialize DI container and services
            try
            {
                ServiceContainer.Initialize();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError(
                    $"Failed to initialize services.\n\nCheck your database connection.\n\n{ex.Message}",
                    "Startup Error");
                return;
            }

            // Check DB connection
            if (!DatabaseManager.Instance.TestConnection())
            {
                var dlg = new PresentationLayer.Forms.DatabaseSetupForm();
                if (dlg.ShowDialog() != DialogResult.OK) return;
            }

            InitialSetupService.EnsurePasswordsHashed();
            ThemeManager.SetTheme(string.Equals(
                ServiceContainer.Settings.Get("Theme", "Light"),
                "Dark",
                StringComparison.OrdinalIgnoreCase));

            Application.Run(new PresentationLayer.Forms.LoginForm());
        }
    }
}
