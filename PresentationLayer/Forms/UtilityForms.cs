// ============================================================
// SmartCafe Pro - Additional Forms & Utilities
// SetupProgressForm, ChangePasswordForm, GlobalExceptionHandler
// ============================================================
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.Services;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    // ==========================================================
    // Setup Progress Form (first-run database setup)
    // ==========================================================
    public class SetupProgressForm : Form
    {
        private readonly string _scriptFolder;
        private ProgressBar  _pb;
        private Label        _lblStatus;
        private RichTextBox  _log;

        public SetupProgressForm(string scriptFolder)
        {
            _scriptFolder = scriptFolder;
            Text          = "SmartCafe Pro — First Time Setup";
            Size          = new Size(560, 380);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox   = false;
            ControlBox    = false;
            BackColor     = ThemeManager.DarkCard;
            BuildLayout();
            Shown += async (s, e) => await RunSetup();
        }

        private void BuildLayout()
        {
            Controls.Add(new Label
            {
                Text = "☕  SmartCafe Pro — Initial Setup",
                Location = new Point(20, 20), Size = new Size(520, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            Controls.Add(new Label
            {
                Text = "Setting up your database. This only runs once...",
                Location = new Point(20, 55), Size = new Size(520, 20),
                ForeColor = ThemeManager.TextSecondary, Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            });

            _pb = new ProgressBar
            {
                Location = new Point(20, 85), Size = new Size(516, 18),
                Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30
            };
            Controls.Add(_pb);

            _lblStatus = new Label
            {
                Location = new Point(20, 110), Size = new Size(516, 20),
                ForeColor = ThemeManager.AccentPrimary, Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent, Text = "Initializing..."
            };
            Controls.Add(_lblStatus);

            _log = new RichTextBox
            {
                Location = new Point(20, 138), Size = new Size(516, 175),
                BackColor = ThemeManager.DarkBackground, ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Consolas", 9), ReadOnly = true, BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(_log);
        }

        private async Task RunSetup()
        {
            await Task.Run(() =>
            {
                Log("Starting database initialization...");

                var scripts = System.IO.Directory.GetFiles(_scriptFolder, "*.sql");
                Array.Sort(scripts);

                foreach (var script in scripts)
                {
                    var name = System.IO.Path.GetFileName(script);
                    Invoke(() =>
                    {
                        _lblStatus.Text = $"Running: {name}";
                        Log($"  ▶ {name}");
                    });

                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    var sql = System.IO.File.ReadAllText(script);
                    foreach (var batch in sql.Split(new[] { "\nGO", "\r\nGO", "\ngo", "\r\ngo" },
                        StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = batch.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;
                        try
                        {
                            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(trimmed, conn)
                                { CommandTimeout = 120 };
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Invoke(() => Log($"    ⚠ {ex.Message[..Math.Min(80,ex.Message.Length)]}",
                                ThemeManager.AccentOrange));
                        }
                    }
                    Invoke(() => Log($"    ✓ Complete", ThemeManager.AccentGreen));
                }

                // Hash passwords
                Invoke(() =>
                {
                    _lblStatus.Text = "Initializing user credentials...";
                    Log("Setting up user passwords...");
                });
                InitialSetupService.EnsurePasswordsHashed();
                Invoke(() => Log("  ✓ Passwords initialized", ThemeManager.AccentGreen));
            });

            _pb.Style = ProgressBarStyle.Continuous;
            _pb.Value = 100;
            _lblStatus.Text = "✓ Setup complete!";
            _lblStatus.ForeColor = ThemeManager.AccentGreen;
            Log("\n✓ SmartCafe Pro database is ready!", ThemeManager.AccentGreen);
            Log("  Default login: admin / Admin@123", ThemeManager.TextSecondary);

            await Task.Delay(1800);
            DialogResult = DialogResult.OK;
        }

        private void Log(string msg, Color? color = null)
        {
            if (IsDisposed) return;
            _log.SelectionColor = color ?? ThemeManager.TextSecondary;
            _log.AppendText(msg + "\n");
            _log.ScrollToCaret();
        }
    }

    // ==========================================================
    // Change Password Form
    // ==========================================================
    public class ChangePasswordForm : Form
    {
        private TextBox txCurrent, txNew, txConfirm;
        private Label   lblError;
        private Button  btnSave, btnCancel;

        public ChangePasswordForm()
        {
            Text            = "Change Password";
            Size            = new Size(420, 310);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = ThemeManager.DarkCard;
            ForeColor       = ThemeManager.TextPrimary;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Controls.Add(new Label { Text = "🔒  Change Password",
                Location = new Point(20, 16), Size = new Size(380, 28),
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = Color.Transparent });

            int y = 60;
            AddField("Current Password:", out txCurrent, ref y, true);
            AddField("New Password:",     out txNew,     ref y, true);
            AddField("Confirm New:",      out txConfirm, ref y, true);

            lblError = new Label { Location = new Point(20, y), Size = new Size(380, 20),
                ForeColor = ThemeManager.AccentRed, Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent };
            Controls.Add(lblError); y += 30;

            btnSave = new Button { Text = "💾  Change Password",
                Location = new Point(20, y), Size = new Size(180, 38),
                BackColor = ThemeManager.AccentGreen, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Cancel",
                Location = new Point(210, y), Size = new Size(100, 38),
                BackColor = ThemeManager.AccentRed, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddField(string label, out TextBox tb, ref int y, bool password)
        {
            Controls.Add(new Label { Text = label, Location = new Point(20, y),
                Size = new Size(150, 20), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            tb = new TextBox { Location = new Point(175, y - 2), Size = new Size(220, 28),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            if (password) tb.UseSystemPasswordChar = true;
            Controls.Add(tb);
            y += 40;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrEmpty(txCurrent.Text))
            { lblError.Text = "Please enter your current password."; return; }
            if (string.IsNullOrEmpty(txNew.Text) || txNew.Text.Length < 6)
            { lblError.Text = "New password must be at least 6 characters."; return; }
            if (txNew.Text != txConfirm.Text)
            { lblError.Text = "New passwords do not match."; return; }

            var userID = SessionManager.Instance.CurrentUserID;
            bool ok = ServiceContainer.Auth.ChangePassword(userID, txCurrent.Text, txNew.Text);

            if (ok)
            {
                MessageHelper.ShowSuccess("Password changed successfully!");
                DialogResult = DialogResult.OK;
            }
            else
            {
                lblError.Text = "Current password is incorrect.";
            }
        }
    }

    // ==========================================================
    // About Form
    // ==========================================================
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text            = "About SmartCafe Pro";
            Size            = new Size(460, 340);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = ThemeManager.DarkCard;
            DoubleBuffered  = true;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, Width, 100),
                    Color.FromArgb(30, 30, 55), Color.FromArgb(15, 15, 30),
                    LinearGradientMode.ForwardDiagonal);
                g.FillRectangle(brush, 0, 0, Width, 100);
                using var acBrush = new SolidBrush(ThemeManager.AccentPrimary);
                g.FillRectangle(acBrush, 0, 0, Width, 4);
            };

            Controls.Add(new Label { Text = "☕", Location = new Point(195, 18),
                Size = new Size(70, 60), Font = new Font("Segoe UI", 36),
                ForeColor = ThemeManager.AccentPrimary, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter });

            Controls.Add(new Label { Text = "SmartCafe Pro",
                Location = new Point(0, 100), Size = new Size(458, 35),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

            Controls.Add(new Label { Text = "Advanced Cafe Management System  v1.0.0",
                Location = new Point(0, 135), Size = new Size(458, 22),
                ForeColor = ThemeManager.TextSecondary, Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

            Controls.Add(new Label { Text = "Built with .NET 10 • WinForms • SQL Server",
                Location = new Point(0, 160), Size = new Size(458, 20),
                ForeColor = ThemeManager.TextMuted, Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

            var sep = new Panel { Location = new Point(80, 190), Size = new Size(300, 1),
                BackColor = ThemeManager.BorderColor };
            Controls.Add(sep);

            var features = new[]
            {
                "📊 Real-time Dashboard       🛒 Full POS System",
                "👥 Customer Loyalty CRM     📦 Inventory Management",
                "👨‍🍳 Kitchen Display System    📈 Reports & Analytics",
                "🪑 Table Management         💳 Multi-payment Support"
            };
            int y = 202;
            foreach (var f in features)
            {
                Controls.Add(new Label { Text = f, Location = new Point(40, y),
                    Size = new Size(380, 20), ForeColor = ThemeManager.TextSecondary,
                    Font = new Font("Segoe UI", 8), BackColor = Color.Transparent });
                y += 19;
            }

            Controls.Add(new Label { Text = "© 2024 SmartCafe Technologies. University FYP Project.",
                Location = new Point(0, y + 8), Size = new Size(458, 18),
                ForeColor = ThemeManager.TextMuted, Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

            var btnClose = new Button { Text = "Close",
                Location = new Point(180, y + 30), Size = new Size(100, 34),
                BackColor = ThemeManager.AccentPrimary, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }
    }
}

// ==========================================================
// Global Exception Logger
// ==========================================================
namespace SmartCafePro.Utilities
{
    public static class ExceptionLogger
    {
        private static readonly string LogFolder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartCafePro", "Logs");

        public static void Log(Exception ex, string context = "")
        {
            try
            {
                System.IO.Directory.CreateDirectory(LogFolder);
                var logFile = System.IO.Path.Combine(LogFolder,
                    $"error_{DateTime.Now:yyyy-MM-dd}.log");
                var entry   = $"[{DateTime.Now:HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}\n" +
                              $"  Stack: {ex.StackTrace?.Split('\n')[0]}\n";
                System.IO.File.AppendAllText(logFile, entry);
            }
            catch { /* logging must never crash the app */ }
        }

        public static void LogAndShow(Exception ex, string context = "")
        {
            Log(ex, context);
            MessageHelper.ShowError($"{ex.Message}\n\nDetails logged to: {LogFolder}", "Error");
        }
    }
}
