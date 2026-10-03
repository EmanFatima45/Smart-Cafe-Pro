// ============================================================
// SmartCafe Pro - InitialSetupService.cs
// Handles first-run password hashing and DB initialization
// ============================================================
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using SmartCafePro.BusinessLayer;
using SmartCafePro.DataAccessLayer;

namespace SmartCafePro.Services
{
    /// <summary>
    /// Run once on application startup to hash any placeholder passwords.
    /// Called from Program.Main() before showing LoginForm.
    /// </summary>
    public static class InitialSetupService
    {
        /// <summary>
        /// Finds all users with PasswordHash = 'NEEDS_SETUP' and applies
        /// a proper SHA-256 hash. Default password for such users is 'Admin@123'.
        /// </summary>
        public static void EnsurePasswordsHashed()
        {
            try
            {
                var auth = ServiceContainer.Auth;
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();

                // Find users needing password setup
                var findCmd = new SqlCommand(
                    "SELECT UserID, Username FROM Users WHERE PasswordHash = 'NEEDS_SETUP'",
                    conn) { CommandType = CommandType.Text };

                var users = new System.Collections.Generic.List<(int ID, string Name)>();
                using (var r = findCmd.ExecuteReader())
                    while (r.Read())
                        users.Add(((int)r["UserID"], r["Username"].ToString()));

                // Hash default password for each
                foreach (var (id, name) in users)
                {
                    var defaultPass = name == "admin" ? "Admin@123" : "Pass@123";
                    var salt        = auth.GenerateSalt();
                    var hash        = auth.HashPassword(defaultPass, salt);

                    using var updateCmd = new SqlCommand(
                        "UPDATE Users SET PasswordHash=@h, PasswordSalt=@s WHERE UserID=@id",
                        conn) { CommandType = CommandType.Text };
                    updateCmd.Parameters.AddWithValue("@h",  hash);
                    updateCmd.Parameters.AddWithValue("@s",  salt);
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine(
                        $"[Setup] Password initialized for user '{name}' (ID={id}).");
                }

                if (users.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Setup] {users.Count} user password(s) initialized.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Setup] Password init error: {ex.Message}");
                // Non-fatal — user can still log in if passwords were previously set.
            }
        }

        /// <summary>
        /// Checks whether the database has been set up (tables exist and seeded).
        /// </summary>
        public static bool IsDatabaseSeeded()
        {
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users", conn)
                    { CommandType = CommandType.Text };
                return (int)cmd.ExecuteScalar() > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Runs the SQL setup scripts programmatically if DB is empty.
        /// Reads .sql files from the Database\ folder next to the exe.
        /// </summary>
        public static bool RunSetupScripts(string scriptFolder)
        {
            try
            {
                var scripts = System.IO.Directory.GetFiles(scriptFolder, "*.sql");
                Array.Sort(scripts); // runs 01_, 02_, 03_, 04_ in order

                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();

                foreach (var script in scripts)
                {
                    var sql = System.IO.File.ReadAllText(script);

                    // Split on GO statements (SSMS batch separator)
                    foreach (var batch in sql.Split(
                        new[] { "\r\nGO", "\nGO", "\r\ngo", "\ngo" },
                        StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = batch.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;

                        using var cmd = new SqlCommand(trimmed, conn)
                        {
                            CommandType    = CommandType.Text,
                            CommandTimeout = 120
                        };
                        try { cmd.ExecuteNonQuery(); }
                        catch (Exception batchEx)
                        {
                            // Log and continue — some batches may fail if objects already exist
                            System.Diagnostics.Debug.WriteLine(
                                $"[Setup] Batch skipped ({batchEx.Message}): {trimmed[..Math.Min(80, trimmed.Length)]}");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Setup] Script error: {ex.Message}");
                return false;
            }
        }
    }
}


// ============================================================
// SmartCafe Pro - Updated Program.cs with InitialSetupService
// ============================================================
// Replace the existing Program.cs content with this version:

/*
using System;
using System.IO;
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

            // Global exception handlers
            Application.ThreadException += (s, e) =>
                MessageHelper.ShowError($"Unexpected error:\n{e.Exception.Message}","Application Error");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageHelper.ShowError($"Fatal:\n{((Exception)e.ExceptionObject).Message}","Fatal Error");

            // Initialize services
            try { ServiceContainer.Initialize(); }
            catch (Exception ex)
            {
                MessageHelper.ShowError($"Service initialization failed.\n\n{ex.Message}","Startup Error");
                return;
            }

            // Test DB connection
            if (!DatabaseManager.Instance.TestConnection())
            {
                var dlg = new PresentationLayer.Forms.DatabaseSetupForm();
                if (dlg.ShowDialog() != DialogResult.OK) return;
            }

            // Auto-run SQL setup scripts on first launch
            var scriptFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (Directory.Exists(scriptFolder) && !InitialSetupService.IsDatabaseSeeded())
            {
                var setupDlg = new PresentationLayer.Forms.SetupProgressForm(scriptFolder);
                setupDlg.ShowDialog();
            }

            // Hash any placeholder passwords
            InitialSetupService.EnsurePasswordsHashed();

            Application.Run(new PresentationLayer.Forms.LoginForm());
        }
    }
}
*/
