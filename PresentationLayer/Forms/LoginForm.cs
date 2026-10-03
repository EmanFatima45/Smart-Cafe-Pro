// ============================================================
// SmartCafe Pro - LoginForm.cs
// Modern animated login screen with role-based access
// ============================================================
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SmartCafePro.BusinessLayer;
using SmartCafePro.DataAccessLayer;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    public class LoginForm : Form
    {
        // Controls
        private Panel       pnlLeft, pnlRight, pnlCard;
        private Label       lblTitle, lblSubtitle, lblUser, lblPass, lblError, lblVersion;
        private TextBox     txtUsername, txtPassword;
        private CheckBox    chkRemember, chkShowPass;
        private Button      btnLogin, btnForgot, btnAdminMode, btnCustomerMode, btnSignUp;
        private PictureBox  picLogo;
        private Panel       pnlLoadingBar;

        // Animation
        private Timer  _fadeTimer;
        private double _opacity = 0;
        private bool   _isLoading;
        private bool   _customerLoginMode;

        public LoginForm()
        {
            InitializeComponent();
            SetupAnimations();
            ClearLoginFields();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form properties
            Text            = "SmartCafe Pro — Login";
            Size            = new Size(1040, 620);
            MinimumSize     = new Size(960, 580);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = ThemeManager.DarkBackground;
            Opacity         = 0;
            DoubleBuffered  = true;

            // ── Left decorative panel ─────────────────────────────
            pnlLeft = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(400, ClientSize.Height),
                BackColor = ThemeManager.DarkSidebar
            };
            pnlLeft.Paint += PnlLeft_Paint;

            // Logo placeholder
            picLogo = new PictureBox
            {
                Size      = new Size(80, 80),
                Location  = new Point(160, 120),
                BackColor = Color.Transparent,
                SizeMode  = PictureBoxSizeMode.Zoom
            };
            pnlLeft.Controls.Add(picLogo);

            var lblBrand = new Label
            {
                Text      = "SmartCafe Pro",
                Location  = new Point(90, 215),
                Size      = new Size(220, 35),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlLeft.Controls.Add(lblBrand);

            var lblTagline = new Label
            {
                Text      = "Advanced Cafe Management System",
                Location  = new Point(50, 255),
                Size      = new Size(300, 22),
                ForeColor = Color.FromArgb(218, 198, 174),
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlLeft.Controls.Add(lblTagline);

            // Feature badges on left panel
            AddFeatureBadge(pnlLeft, "📊  Real-time Dashboard",        60, 330);
            AddFeatureBadge(pnlLeft, "🛒  Smart Order Management",     60, 360);
            AddFeatureBadge(pnlLeft, "👥  Customer Loyalty System",    60, 390);
            AddFeatureBadge(pnlLeft, "📦  Inventory Tracking",         60, 420);
            AddFeatureBadge(pnlLeft, "📄  Reports & Analytics",        60, 450);

            Controls.Add(pnlLeft);

            // ── Right login panel ─────────────────────────────────
            pnlRight = new Panel
            {
                Location  = new Point(pnlLeft.Width, 0),
                Size      = new Size(ClientSize.Width - pnlLeft.Width, ClientSize.Height),
                BackColor = ThemeManager.DarkBackground
            };

            // Login card
            pnlCard = new Panel
            {
                Size      = new Size(390, 545),
                Location  = new Point(30, 30),
                BackColor = ThemeManager.DarkCard,
                Padding   = new Padding(0)
            };
            pnlCard.Paint += PnlCard_Paint;

            btnAdminMode = MakeLoginModeButton("Admin", 30, 24, true);
            btnCustomerMode = MakeLoginModeButton("Customer", 195, 24, false);
            btnAdminMode.Click += (s, e) => SetLoginMode(false);
            btnCustomerMode.Click += (s, e) => SetLoginMode(true);

            lblTitle = MakeLabel("Admin Login", 30, 70, 330, 48,
                ThemeManager.ForeText, new Font("Segoe UI", 20, FontStyle.Bold));
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.AutoEllipsis = false;

            lblSubtitle = MakeLabel("Sign in to manage SmartCafe", 30, 122, 330, 24,
                ThemeManager.TextSecondary, new Font("Segoe UI", 10));

            // Separator
            var sep = new Panel { Location = new Point(30, 154), Size = new Size(330, 1),
                BackColor = ThemeManager.BorderColor };

            // Username
            lblUser = MakeLabel("USERNAME", 30, 172, 330, 18,
                ThemeManager.AccentPrimary, new Font("Segoe UI", 8, FontStyle.Bold));

            txtUsername = MakeTextBox(30, 194, 330, 42, "Enter your username");

            // Password
            lblPass = MakeLabel("PASSWORD", 30, 251, 330, 18,
                ThemeManager.AccentPrimary, new Font("Segoe UI", 8, FontStyle.Bold));

            txtPassword = MakeTextBox(30, 273, 330, 42, "Enter your password");
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };

            // Show password checkbox
            chkShowPass = new CheckBox
            {
                Text      = "Show password",
                Location  = new Point(30, 324),
                Size      = new Size(135, 20),
                ForeColor = ThemeManager.TextSecondary,
                Font      = new Font("Segoe UI", 8),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };
            chkShowPass.CheckedChanged += (s, e) =>
                txtPassword.UseSystemPasswordChar = !chkShowPass.Checked;

            // Remember me
            chkRemember = new CheckBox
            {
                Text      = "Remember me",
                Location  = new Point(30, 351),
                Size      = new Size(140, 22),
                ForeColor = ThemeManager.TextSecondary,
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };

            // Forgot password link
            btnForgot = new Button
            {
                Text      = "Forgot password?",
                Location  = new Point(195, 349),
                Size      = new Size(165, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ThemeManager.AccentPrimary,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 9),
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight
            };
            btnForgot.FlatAppearance.BorderSize = 0;
            btnForgot.Click += BtnForgot_Click;

            // Error label
            lblError = MakeLabel("", 30, 380, 330, 20,
                ThemeManager.AccentRed, new Font("Segoe UI", 9));

            // Login button
            btnLogin = new Button
            {
                Text      = "SIGN IN",
                Location  = new Point(30, 410),
                Size      = new Size(330, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.AccentPrimary,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click  += BtnLogin_Click;
            btnLogin.Paint  += BtnLogin_Paint;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = ThemeManager.AccentSecondary;
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = ThemeManager.AccentPrimary;

            // Loading bar (hidden by default)
            pnlLoadingBar = new Panel
            {
                Location  = new Point(30, 456),
                Size      = new Size(0, 3),
                BackColor = ThemeManager.AccentGreen,
                Visible   = false
            };

            btnSignUp = new Button
            {
                Text      = "CREATE CUSTOMER ACCOUNT",
                Location  = new Point(30, 469),
                Size      = new Size(330, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.AccentPrimary,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Visible   = false
            };
            btnSignUp.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnSignUp.Click += (s, e) => ShowCustomerSignUpDialog();

            // Version label
            lblVersion = MakeLabel("v1.0.0 - SmartCafe Pro", 30, 518, 330, 18,
                ThemeManager.TextMuted, new Font("Segoe UI", 8));

            pnlCard.Controls.AddRange(new Control[]
            {
                btnAdminMode, btnCustomerMode,
                lblTitle, lblSubtitle, sep,
                lblUser, txtUsername,
                lblPass, txtPassword, chkShowPass,
                chkRemember, btnForgot,
                lblError, btnLogin, pnlLoadingBar, btnSignUp, lblVersion
            });

            pnlRight.Controls.Add(pnlCard);

            // Close button
            var btnClose = new Button
            {
                Text      = "✕",
                Location  = new Point(pnlRight.Width - 40, 10),
                Size      = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.TextSecondary,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click       += (s, e) => Application.Exit();
            btnClose.MouseEnter  += (s, e) => btnClose.ForeColor = ThemeManager.AccentRed;
            btnClose.MouseLeave  += (s, e) => btnClose.ForeColor = ThemeManager.TextSecondary;
            pnlRight.Controls.Add(btnClose);

            Controls.Add(pnlRight);
            ResumeLayout(false);

            // Center card on resize
            Resize += (s, e) => LayoutLoginPanels();
            LayoutLoginPanels();
        }

        private void LayoutLoginPanels()
        {
            int leftWidth = ClientSize.Width < 900 ? 340 : 400;

            pnlLeft.Location = new Point(0, 0);
            pnlLeft.Size = new Size(leftWidth, ClientSize.Height);

            pnlRight.Location = new Point(leftWidth, 0);
            pnlRight.Size = new Size(Math.Max(0, ClientSize.Width - leftWidth), ClientSize.Height);

            pnlCard.Location = new Point(
                Math.Max(24, (pnlRight.Width  - pnlCard.Width)  / 2),
                Math.Max(24, (pnlRight.Height - pnlCard.Height) / 2));
        }

        // ── Painters ────────────────────────────────────────────
        private void PnlLeft_Paint(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlLeft.Width, pnlLeft.Height);

            // Gradient background
            using var brush = new LinearGradientBrush(rect,
                Color.FromArgb(76, 48, 35), Color.FromArgb(45, 29, 22),
                LinearGradientMode.ForwardDiagonal);
            g.FillRectangle(brush, rect);

            // Decorative circles
            using var circlePen = new Pen(Color.FromArgb(30, ThemeManager.AccentPrimary), 1);
            g.DrawEllipse(circlePen, -50, -50, 250, 250);
            g.DrawEllipse(circlePen, pnlLeft.Width - 100, pnlLeft.Height - 150, 200, 200);

            // Accent top bar
            using var topBrush = new LinearGradientBrush(
                new Rectangle(0, 0, pnlLeft.Width, 4),
                ThemeManager.AccentPrimary, ThemeManager.AccentSecondary,
                LinearGradientMode.Horizontal);
            g.FillRectangle(topBrush, 0, 0, pnlLeft.Width, 4);

            // Coffee cup icon (simple vector)
            DrawCoffeeCup(g, 165, 130);
        }

        private void DrawCoffeeCup(Graphics g, int x, int y)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen   = new Pen(Color.FromArgb(200, ThemeManager.AccentPrimary), 2.5f);
            using var brush = new SolidBrush(Color.FromArgb(40, ThemeManager.AccentPrimary));

            // Cup body
            var cupPts = new Point[] {
                new(x+5, y+25), new(x+65, y+25),
                new(x+58, y+70), new(x+12, y+70)
            };
            g.FillPolygon(brush, cupPts);
            g.DrawPolygon(pen, cupPts);

            // Handle
            g.DrawArc(pen, x+55, y+35, 20, 20, -90, 180);

            // Saucer
            g.FillEllipse(brush, x, y+68, 72, 12);
            g.DrawEllipse(pen, x, y+68, 72, 12);

            // Steam
            using var steamPen = new Pen(Color.FromArgb(100, ThemeManager.AccentPrimary), 1.5f);
            g.DrawBezier(steamPen, x+25, y+20, x+20, y+10, x+30, y+5, x+25, y-5);
            g.DrawBezier(steamPen, x+35, y+20, x+30, y+10, x+40, y+5, x+35, y-5);
            g.DrawBezier(steamPen, x+45, y+20, x+40, y+10, x+50, y+5, x+45, y-5);
        }

        private void PnlCard_Paint(object sender, PaintEventArgs e)
        {
            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1);

            // Rounded rect
            using var path = RoundedRect(rect, 16);
            using var bg   = new SolidBrush(ThemeManager.DarkCard);
            g.FillPath(bg, path);

            // Top gradient accent
            using var topGrad = new LinearGradientBrush(
                new Rectangle(0, 0, pnlCard.Width, 5),
                ThemeManager.AccentPrimary, ThemeManager.AccentSecondary,
                LinearGradientMode.Horizontal);
            g.FillRectangle(topGrad, 0, 0, pnlCard.Width, 5);

            // Subtle border
            using var borderPen = new Pen(ThemeManager.BorderColor);
            g.DrawPath(borderPen, path);
        }

        private void BtnLogin_Paint(object sender, PaintEventArgs e)
        {
            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, btnLogin.Width - 1, btnLogin.Height - 1);

            using var path  = RoundedRect(rect, 10);
            using var brush = new LinearGradientBrush(rect,
                ThemeManager.AccentPrimary, ThemeManager.AccentSecondary,
                LinearGradientMode.Horizontal);
            g.FillPath(brush, path);

            var sf = new StringFormat { Alignment = StringAlignment.Center,
                                        LineAlignment = StringAlignment.Center };
            g.DrawString(btnLogin.Text, btnLogin.Font, Brushes.White,
                new RectangleF(0, 0, btnLogin.Width, btnLogin.Height), sf);
        }

        // ── Animations ──────────────────────────────────────────
        private void SetupAnimations()
        {
            _fadeTimer = new Timer { Interval = 20 };
            _fadeTimer.Tick += (s, e) =>
            {
                _opacity += 0.04;
                if (_opacity >= 1.0)
                {
                    _opacity = 1.0;
                    _fadeTimer.Stop();
                }
                Opacity = _opacity;
            };
            _fadeTimer.Start();
        }

        private async void AnimateLoadingBar()
        {
            pnlLoadingBar.Visible = true;
            pnlLoadingBar.Width   = 0;
            for (int w = 0; w <= 320; w += 8)
            {
                pnlLoadingBar.Width = w;
                await Task.Delay(10);
            }
        }

        // ── Event Handlers ──────────────────────────────────────
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_isLoading) return;

            lblError.Text = "";
            var username  = txtUsername.Text.Trim();
            var password  = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password.");
                return;
            }

            _isLoading       = true;
            btnLogin.Enabled = false;
            btnLogin.Text    = "SIGNING IN...";
            AnimateLoadingBar();

            await Task.Run(() => System.Threading.Thread.Sleep(600)); // simulate

            var user = ServiceContainer.Auth.Login(username, password);

            if (user == null)
            {
                _isLoading       = false;
                btnLogin.Enabled = true;
                btnLogin.Text    = "SIGN IN";
                pnlLoadingBar.Visible = false;
                ShowError("Invalid username or password.");
                ShakeForm();
                return;
            }

            bool isCustomerUser = string.Equals(user.RoleName, "Customer", StringComparison.OrdinalIgnoreCase);
            if (_customerLoginMode != isCustomerUser)
            {
                _isLoading = false;
                btnLogin.Enabled = true;
                btnLogin.Text = "SIGN IN";
                pnlLoadingBar.Visible = false;
                ShowError(_customerLoginMode
                    ? "Please use a customer account or create one first."
                    : "Please use an admin or staff account.");
                ShakeForm();
                return;
            }

            // Handle remember me
            if (chkRemember.Checked)
            {
                Properties.Settings.Default.RememberedUser = username;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.RememberedUser = "";
                Properties.Settings.Default.Save();
            }

            // Set session
            SessionManager.Instance.Login(user);

            // Fade out and open main form
            for (double op = 1.0; op >= 0; op -= 0.05)
            {
                Opacity = op;
                await Task.Delay(15);
            }

            var mainForm = new MainForm();
            mainForm.Show();
            Hide();
            mainForm.FormClosed += (ss, ee) => Close();
        }

        private void BtnForgot_Click(object sender, EventArgs e)
        {
            MessageHelper.ShowWarning(
                "Please contact your system administrator to reset your password.",
                "Forgot Password");
        }

        private void LoadRememberedUser()
        {
            ClearLoginFields();
        }

        private void ClearLoginFields()
        {
            txtUsername.Text = "";
            txtPassword.Text = "";
            chkRemember.Checked = false;
            chkShowPass.Checked = false;
            txtPassword.UseSystemPasswordChar = true;
            lblError.Text = "";
            try
            {
                Properties.Settings.Default.RememberedUser = "";
                Properties.Settings.Default.Save();
            }
            catch { /* ignore */ }
        }

        private void ShowError(string msg)
        {
            lblError.Text = "⚠ " + msg;
        }

        private async void ShakeForm()
        {
            var orig = Location;
            int[] offsets = { -8, 8, -6, 6, -4, 4, -2, 2, 0 };
            foreach (var off in offsets)
            {
                Location = new Point(orig.X + off, orig.Y);
                await Task.Delay(30);
            }
            Location = orig;
        }

        // ── Helpers ─────────────────────────────────────────────
        private void ShowCustomerSignUpDialog()
        {
            using var dlg = new Form
            {
                Text = "Customer Sign Up",
                Size = new Size(430, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.DarkBackground
            };

            dlg.Controls.Add(MakeDialogLabel("Create Customer Account", 24, 22, 360, 34,
                ThemeManager.AccentPrimary, new Font("Segoe UI", 16, FontStyle.Bold)));

            AddDialogCaption(dlg, "Name", 24, 72);
            var name = MakeDialogTextBox(24, 92, 360, "Full name");
            dlg.Controls.Add(name);

            AddDialogCaption(dlg, "Phone", 24, 132);
            var phone = MakeDialogTextBox(24, 152, 360, "Phone number");
            dlg.Controls.Add(phone);

            AddDialogCaption(dlg, "Username", 24, 192);
            var username = MakeDialogTextBox(24, 212, 360, "Username");
            dlg.Controls.Add(username);

            AddDialogCaption(dlg, "Password", 24, 252);
            var password = MakeDialogTextBox(24, 272, 360, "Password");
            password.UseSystemPasswordChar = true;
            dlg.Controls.Add(password);

            AddDialogCaption(dlg, "Confirm Password", 24, 312);
            var confirm = MakeDialogTextBox(24, 332, 360, "Confirm password");
            confirm.UseSystemPasswordChar = true;
            dlg.Controls.Add(confirm);

            var btnCreate = new Button
            {
                Text = "CREATE ACCOUNT",
                Location = new Point(24, 390),
                Size = new Size(170, 38),
                BackColor = ThemeManager.AccentPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(210, 390),
                Size = new Size(174, 38),
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnCancel.Click += (s, e) => dlg.Close();

            btnCreate.Click += (s, e) =>
            {
                string fullName = name.Text.Trim();
                string phoneNumber = phone.Text.Trim();
                string userName = username.Text.Trim();
                string pass = password.Text;
                string confirmPass = confirm.Text;

                if (fullName.Length < 2 || userName.Length < 3 || pass.Length < 4)
                {
                    MessageHelper.ShowWarning("Please enter name, username and password. Password must be at least 4 characters.");
                    return;
                }
                if (pass != confirmPass)
                {
                    MessageHelper.ShowWarning("Password and confirm password do not match.");
                    return;
                }

                try
                {
                    CreateCustomerAccount(fullName, phoneNumber, userName, pass);
                    txtUsername.Text = userName;
                    txtPassword.Text = pass;
                    txtUsername.ForeColor = ThemeManager.TextPrimary;
                    txtPassword.ForeColor = ThemeManager.TextPrimary;
                    SetLoginMode(true);
                    MessageHelper.ShowSuccess("Customer account created. You can sign in now.");
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError(ex.Message, "Sign Up Failed");
                }
            };

            dlg.Controls.Add(btnCreate);
            dlg.Controls.Add(btnCancel);
            dlg.ShowDialog(this);
        }

        private void CreateCustomerAccount(string fullName, string phone, string username, string password)
        {
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();

            using (var exists = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username=@Username", conn))
            {
                exists.Parameters.AddWithValue("@Username", username);
                if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("This username already exists. Please choose another username.");
            }

            int roleID;
            using (var roleCmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName='Customer')
    INSERT INTO Roles(RoleName, Description, Permissions)
    VALUES('Customer', 'Customer self ordering account', 'CustomerOrder');
SELECT RoleID FROM Roles WHERE RoleName='Customer';", conn))
            {
                roleID = Convert.ToInt32(roleCmd.ExecuteScalar());
            }

            var salt = ServiceContainer.Auth.GenerateSalt();
            var hash = ServiceContainer.Auth.HashPassword(password, salt);

            int userID;
            using (var userCmd = new SqlCommand(@"
INSERT INTO Users(Username, PasswordHash, PasswordSalt, RoleID, Email, IsActive)
OUTPUT INSERTED.UserID
VALUES(@Username, @PasswordHash, @PasswordSalt, @RoleID, '', 1);", conn))
            {
                userCmd.Parameters.AddWithValue("@Username", username);
                userCmd.Parameters.AddWithValue("@PasswordHash", hash);
                userCmd.Parameters.AddWithValue("@PasswordSalt", salt);
                userCmd.Parameters.AddWithValue("@RoleID", roleID);
                userID = Convert.ToInt32(userCmd.ExecuteScalar());
            }

            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string firstName = parts.Length > 0 ? parts[0] : fullName;
            string lastName = parts.Length > 1 ? parts[1] : "";

            bool hasCustomerUserID;
            using (var schemaCmd = new SqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Customers' AND COLUMN_NAME='UserID'", conn))
            {
                hasCustomerUserID = Convert.ToInt32(schemaCmd.ExecuteScalar()) > 0;
            }

            var customerSql = hasCustomerUserID
                ? @"INSERT INTO Customers(UserID, FirstName, LastName, Phone, Email, MembershipLevel, IsActive)
VALUES(@UserID, @FirstName, @LastName, @Phone, '', 'Bronze', 1);"
                : @"INSERT INTO Customers(FirstName, LastName, Phone, Email, MembershipLevel, IsActive)
VALUES(@FirstName, @LastName, @Phone, '', 'Bronze', 1);";

            using var customerCmd = new SqlCommand(customerSql, conn);
            if (hasCustomerUserID)
                customerCmd.Parameters.AddWithValue("@UserID", userID);
            customerCmd.Parameters.AddWithValue("@FirstName", firstName);
            customerCmd.Parameters.AddWithValue("@LastName", lastName);
            customerCmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
            customerCmd.ExecuteNonQuery();
        }

        private Label MakeDialogLabel(string text, int x, int y, int w, int h, Color fore, Font font)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = fore,
                Font = font,
                BackColor = Color.Transparent
            };
        }

        private void AddDialogCaption(Form dlg, string text, int x, int y)
        {
            dlg.Controls.Add(MakeDialogLabel(text, x, y, 220, 18,
                ThemeManager.AccentPrimary, new Font("Segoe UI", 8, FontStyle.Bold)));
        }

        private TextBox MakeDialogTextBox(int x, int y, int w, string placeholder)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 30),
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.TextPrimary,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = placeholder
            };
        }

        private Button MakeLoginModeButton(string text, int x, int y, bool active)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(155, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? ThemeManager.AccentPrimary : ThemeManager.DarkSurface,
                ForeColor = active ? Color.White : ThemeManager.TextSecondary,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = active ? ThemeManager.AccentPrimary : ThemeManager.BorderColor;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private void SetLoginMode(bool customerMode)
        {
            _customerLoginMode = customerMode;

            lblTitle.Text = customerMode ? "Customer / User Login" : "Admin Login";
            lblSubtitle.Text = customerMode
                ? "Sign in to place your order"
                : "Sign in to manage SmartCafe";
            Text = customerMode ? "SmartCafe Pro - Customer Login" : "SmartCafe Pro - Admin Login";

            btnAdminMode.BackColor = customerMode ? ThemeManager.DarkSurface : ThemeManager.AccentPrimary;
            btnAdminMode.ForeColor = customerMode ? ThemeManager.TextSecondary : Color.White;
            btnAdminMode.FlatAppearance.BorderColor = customerMode ? ThemeManager.BorderColor : ThemeManager.AccentPrimary;

            btnCustomerMode.BackColor = customerMode ? ThemeManager.AccentPrimary : ThemeManager.DarkSurface;
            btnCustomerMode.ForeColor = customerMode ? Color.White : ThemeManager.TextSecondary;
            btnCustomerMode.FlatAppearance.BorderColor = customerMode ? ThemeManager.AccentPrimary : ThemeManager.BorderColor;

            btnSignUp.Visible = customerMode;
        }

        private Label MakeLabel(string text, int x, int y, int w, int h,
            Color fore, Font font)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                ForeColor = fore,
                Font      = font,
                BackColor = Color.Transparent
            };
        }

        private TextBox MakeTextBox(int x, int y, int w, int h, string placeholder)
        {
            var tb = new TextBox
            {
                Location    = new Point(x, y),
                Size        = new Size(w, h),
                BackColor   = ThemeManager.DarkSurface,
                ForeColor   = ThemeManager.TextPrimary,
                Font        = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                Padding     = new Padding(8, 10, 8, 10),
                PlaceholderText = placeholder
            };

            // Bottom border highlight effect
            tb.Paint += (s, e) => { }; // handled via panel wrapper in real impl

            return tb;
        }

        private void AddFeatureBadge(Panel parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(280, 22),
                ForeColor = Color.FromArgb(218, 198, 174),
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d    = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // Allow dragging the borderless form
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN,
                    NativeMethods.HT_CAPTION, 0);
            }
        }
    }

    // ── Native methods for drag ──────────────────────────────────
    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION       = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }

    // ── Database Setup Form ──────────────────────────────────────
    public class DatabaseSetupForm : Form
    {
        private TextBox txtServer, txtDatabase, txtUser, txtPass;
        private Button  btnTest, btnSave;
        private Label   lblStatus;

        public DatabaseSetupForm()
        {
            Text            = "Database Connection Setup";
            Size            = new Size(440, 340);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = ThemeManager.DarkCard;
            ForeColor       = ThemeManager.TextPrimary;

            AddField("Server Name:",    out txtServer,   "localhost",        50);
            AddField("Database Name:",  out txtDatabase, "SmartCafeProDB",  100);
            AddField("Username (opt):", out txtUser,     "",                150);
            AddField("Password (opt):", out txtPass,     "",                200);
            txtPass.PasswordChar = '●';

            lblStatus = new Label { Location = new Point(20, 250), Size = new Size(395, 22),
                ForeColor = ThemeManager.AccentRed, Font = new Font("Segoe UI", 9) };
            Controls.Add(lblStatus);

            btnTest = new Button
            {
                Text = "Test Connection", Location = new Point(20, 278),
                Size = new Size(160, 36), BackColor = ThemeManager.AccentBlue,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += (s, e) =>
            {
                DatabaseManager.Instance.UpdateConnectionString(
                    txtServer.Text, txtDatabase.Text,
                    txtUser.Text, txtPass.Text);
                if (DatabaseManager.Instance.TestConnection())
                {
                    lblStatus.ForeColor = ThemeManager.AccentGreen;
                    lblStatus.Text = "✓ Connection successful!";
                }
                else
                {
                    lblStatus.ForeColor = ThemeManager.AccentRed;
                    lblStatus.Text = "✗ Connection failed. Check settings.";
                }
            };
            Controls.Add(btnTest);

            btnSave = new Button
            {
                Text = "Save & Connect", Location = new Point(255, 278),
                Size = new Size(160, 36), BackColor = ThemeManager.AccentGreen,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                DatabaseManager.Instance.UpdateConnectionString(
                    txtServer.Text, txtDatabase.Text, txtUser.Text, txtPass.Text);
                if (DatabaseManager.Instance.TestConnection())
                    DialogResult = DialogResult.OK;
                else
                    lblStatus.Text = "✗ Still failing. Cannot proceed.";
            };
            Controls.Add(btnSave);
        }

        private void AddField(string label, out TextBox tb, string placeholder, int y)
        {
            Controls.Add(new Label { Text = label, Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            tb = new TextBox { Location = new Point(155, y), Size = new Size(260, 26),
                Text = placeholder, BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10) };
            Controls.Add(tb);
        }
    }
}
