// ============================================================
// SmartCafe Pro - MainForm.cs
// Shell form: animated sidebar + content panel host
// ============================================================
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    public class MainForm : Form
    {
        // Layout panels
        private Panel  pnlSidebar, pnlTopBar, pnlContent, pnlBody;
        private Panel  pnlSidebarInner;

        // Top bar controls
        private Label  lblPageTitle, lblDateTime, lblUserInfo;
        private Button btnToggleSidebar, btnNotifications, btnLogout, btnTheme;
        private Label  lblNotifBadge;

        // Sidebar nav buttons
        private Button _activeNavBtn;
        private Timer  _clockTimer;
        private Timer  _notifTimer;

        // Sidebar expanded/collapsed
        private bool   _sidebarExpanded = true;
        private int    _sidebarFullWidth  = 230;
        private int    _sidebarMiniWidth  = 65;
        private Timer  _sidebarAnim;

        public MainForm()
        {
            InitializeComponent();
            if (IsCustomerUser())
                LoadCustomerHome();
            else
                LoadDashboard();
            StartTimers();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text            = "SmartCafe Pro";
            Size            = new Size(1280, 800);
            MinimumSize     = new Size(1024, 680);
            StartPosition   = FormStartPosition.CenterScreen;
            WindowState     = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = ThemeManager.Background;
            DoubleBuffered  = true;

            ThemeManager.ThemeChanged += (s, e) => ApplyTheme();

            pnlBody = new Panel
            {
                Dock      = DockStyle.None,
                BackColor = ThemeManager.Background
            };

            // ── Sidebar ──────────────────────────────────────────
            pnlSidebar = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(_sidebarFullWidth, pnlBody.Height),
                BackColor = ThemeManager.Sidebar
            };
            pnlSidebar.Paint += PnlSidebar_Paint;

            // Brand header
            var pnlBrand = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(_sidebarFullWidth, 68),
                BackColor = Color.Transparent
            };
            pnlBrand.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, pnlBrand.Width, 68),
                    Color.FromArgb(80, ThemeManager.AccentPrimary),
                    Color.Transparent, LinearGradientMode.Vertical);
                g.FillRectangle(brush, 0, 0, pnlBrand.Width, 68);
            };

            var lblBrand = new Label
            {
                Text      = "☕ SmartCafe",
                Location  = new Point(15, 15),
                Size      = new Size(200, 36),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            pnlBrand.Controls.Add(lblBrand);
            pnlSidebar.Controls.Add(pnlBrand);

            // Nav items
            int y = 80;
            AddNavSection(pnlSidebar, "MAIN", ref y);
            var btnDash      = AddNavButton(pnlSidebar, "📊", "Dashboard",        ref y, () => LoadDashboard());
            var btnOrders    = AddNavButton(pnlSidebar, "🛒", "Orders",           ref y, () => LoadModule(new OrderForm()));
            var btnPOS       = AddNavButton(pnlSidebar, "💳", "Point of Sale",    ref y, () => LoadModule(new POSForm(IsCustomerUser())));
            var btnKitchen   = AddNavButton(pnlSidebar, "👨‍🍳","Kitchen Display",  ref y, () => LoadModule(new KitchenForm()));
            var btnTables    = AddNavButton(pnlSidebar, "🪑", "Tables",           ref y, () => LoadModule(new TableForm()));
            var btnReserve   = AddNavButton(pnlSidebar, "📅", "Reservations",     ref y, () => LoadModule(new ReservationForm()));

            AddNavSection(pnlSidebar, "MANAGEMENT", ref y);
            var btnCustomers = AddNavButton(pnlSidebar, "👥", "Customers",        ref y, () => LoadModule(new CustomerForm()));
            var btnMenu      = AddNavButton(pnlSidebar, "🍽", "Menu / Products",  ref y, () => LoadModule(new ProductForm()));
            var btnInventory = AddNavButton(pnlSidebar, "📦", "Inventory",        ref y, () => LoadModule(new InventoryForm()));
            var btnEmployees = AddNavButton(pnlSidebar, "🏢", "Employees",        ref y, () => LoadModule(new EmployeeForm()));

            AddNavSection(pnlSidebar, "REPORTS", ref y);
            var btnReports   = AddNavButton(pnlSidebar, "📈", "Analytics",        ref y, () => LoadModule(new ReportsForm()));
            var btnFeedback  = AddNavButton(pnlSidebar, "⭐", "Feedback",         ref y, () => LoadModule(new FeedbackForm()));

            AddNavSection(pnlSidebar, "SYSTEM", ref y);
            var btnSettings  = AddNavButton(pnlSidebar, "⚙",  "Settings",        ref y, () => LoadModule(new SettingsForm()));

            // Role-based visibility
            if (IsCustomerUser())
            {
                btnDash.Visible = false;
                btnOrders.Visible = false;
                btnKitchen.Visible = false;
                btnTables.Visible = false;
                btnCustomers.Visible = false;
                btnMenu.Visible = false;
                btnInventory.Visible = false;
                btnEmployees.Visible = false;
                btnReports.Visible = false;
                btnSettings.Visible = false;

                CompactCustomerNavigation(btnPOS, btnReserve, btnFeedback);

                _activeNavBtn = btnPOS;
                SetActiveNav(btnPOS);
            }
            else if (!SessionManager.Instance.CurrentUser.IsManager)
            {
                btnEmployees.Visible = false;
                btnReports.Visible   = false;

                _activeNavBtn = btnDash;
                SetActiveNav(btnDash);
            }
            else
            {
                _activeNavBtn = btnDash;
                SetActiveNav(btnDash);
            }

            pnlBody.Controls.Add(pnlSidebar);

            // ── Top bar ──────────────────────────────────────────
            pnlTopBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = ThemeManager.Sidebar
            };
            pnlTopBar.Paint += PnlTopBar_Paint;

            btnToggleSidebar = new Button
            {
                Text      = "☰",
                Location  = new Point(12, 13),
                Size      = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13),
                Cursor    = Cursors.Hand
            };
            btnToggleSidebar.FlatAppearance.BorderSize = 0;
            btnToggleSidebar.Click += BtnToggleSidebar_Click;
            pnlTopBar.Controls.Add(btnToggleSidebar);

            lblPageTitle = new Label
            {
                Text      = "Dashboard",
                Location  = new Point(60, 15),
                Size      = new Size(300, 26),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            pnlTopBar.Controls.Add(lblPageTitle);

            // Right side controls
            lblDateTime = new Label
            {
                Location  = new Point(700, 18),
                Size      = new Size(180, 20),
                ForeColor = Color.FromArgb(232, 218, 204),
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlTopBar.Controls.Add(lblDateTime);

            // Notification bell
            var pnlNotif = new Panel
            {
                Size      = new Size(36, 36),
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlNotif.Location = new Point(pnlTopBar.Width - 170, 10);

            btnNotifications = new Button
            {
                Text      = "🔔",
                Location  = new Point(0, 0),
                Size      = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12),
                Cursor    = Cursors.Hand
            };
            btnNotifications.FlatAppearance.BorderSize = 0;
            btnNotifications.Click += (s, e) =>
            {
                if (_activeNavBtn != null)
                {
                    _activeNavBtn.BackColor = Color.Transparent;
                    _activeNavBtn.ForeColor = Color.FromArgb(236, 226, 214);
                    _activeNavBtn = null;
                }

                lblPageTitle.Text = "Notifications";
                LoadModule(new NotificationsForm());
            };

            lblNotifBadge = new Label
            {
                Size      = new Size(18, 18),
                Location  = new Point(18, 0),
                BackColor = ThemeManager.AccentRed,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 7, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = "0",
                Visible   = false
            };

            pnlNotif.Controls.AddRange(new Control[] { btnNotifications, lblNotifBadge });
            pnlTopBar.Controls.Add(pnlNotif);

            // Theme toggle
            btnTheme = new Button
            {
                Text      = "🌙",
                Location  = new Point(pnlTopBar.Width - 128, 10),
                Size      = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTheme.FlatAppearance.BorderSize = 0;
            btnTheme.Click += (s, e) =>
            {
                ThemeManager.ToggleTheme();
                ServiceContainer.Settings.Set("Theme", ThemeManager.IsDarkMode ? "Dark" : "Light");
                btnTheme.Text = ThemeManager.IsDarkMode ? "🌙" : "☀";
            };
            pnlTopBar.Controls.Add(btnTheme);

            // User info + logout
            lblUserInfo = new Label
            {
                Location  = new Point(pnlTopBar.Width - 340, 18),
                Size      = new Size(140, 20),
                ForeColor = Color.FromArgb(232, 218, 204),
                Font      = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Text      = SessionManager.Instance.CurrentUser?.Username ?? "",
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlTopBar.Controls.Add(lblUserInfo);

            btnLogout = new Button
            {
                Text      = "⎋ Logout",
                Location  = new Point(pnlTopBar.Width - 90, 13),
                Size      = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, ThemeManager.AccentRed),
                ForeColor = ThemeManager.AccentRed,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;
            pnlTopBar.Controls.Add(btnLogout);

            Controls.Add(pnlTopBar);

            // ── Content Panel ────────────────────────────────────
            pnlContent = new Panel
            {
                Location  = new Point(_sidebarFullWidth, 0),
                Size      = new Size(Math.Max(0, pnlBody.Width - _sidebarFullWidth), pnlBody.Height),
                BackColor = ThemeManager.Background,
                Padding   = new Padding(0)
            };
            pnlBody.Controls.Add(pnlContent);
            Controls.Add(pnlBody);
            pnlTopBar.BringToFront();
            LayoutBodyPanels();

            ResumeLayout(false);

            // Resize repositioning
            pnlTopBar.Resize += (s, e) => RepositionTopBarControls();
            pnlBody.Resize += (s, e) => LayoutBodyPanels();
            Resize += (s, e) =>
            {
                LayoutBodyPanels();
                RepositionTopBarControls();
            };
            RepositionTopBarControls();
        }

        private void LayoutBodyPanels()
        {
            if (pnlBody == null || pnlSidebar == null || pnlContent == null) return;

            int topBarHeight = pnlTopBar?.Height ?? 0;
            pnlBody.Location = new Point(0, topBarHeight);
            pnlBody.Size = new Size(ClientSize.Width, Math.Max(0, ClientSize.Height - topBarHeight));

            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Size = new Size(pnlSidebar.Width, pnlBody.ClientSize.Height);

            pnlContent.Location = new Point(pnlSidebar.Width, 0);
            pnlContent.Size = new Size(
                Math.Max(0, pnlBody.ClientSize.Width - pnlSidebar.Width),
                pnlBody.ClientSize.Height);
        }

        private void RepositionTopBarControls()
        {
            lblDateTime.Location        = new Point(pnlTopBar.Width - 520, 18);
            lblUserInfo.Location        = new Point(pnlTopBar.Width - 340, 18);
            if (btnNotifications.Parent != null)
                btnNotifications.Parent.Location = new Point(pnlTopBar.Width - 192, 10);
            btnTheme.Location           = new Point(pnlTopBar.Width - 140, 10);
            btnLogout.Location          = new Point(pnlTopBar.Width - 92, 13);
        }

        // ── Painters ────────────────────────────────────────────
        private void PnlSidebar_Paint(object sender, PaintEventArgs e)
        {
            if (pnlSidebar.Width <= 1 || pnlSidebar.Height <= 1)
                return;

            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlSidebar.Width, pnlSidebar.Height);

            var start = ThemeManager.IsDarkMode ? Color.FromArgb(86, 54, 39) : Color.FromArgb(76, 48, 35);
            var end = ThemeManager.IsDarkMode ? Color.FromArgb(58, 36, 27) : Color.FromArgb(45, 29, 22);
            using var brush = new LinearGradientBrush(rect,
                start, end,
                LinearGradientMode.Vertical);
            g.FillRectangle(brush, rect);

            // Right border
            using var pen = new Pen(Color.FromArgb(90, 180, 145, 112), 1);
            g.DrawLine(pen, rect.Right - 1, 0, rect.Right - 1, rect.Bottom);
        }

        private void PnlTopBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            using var brush = new SolidBrush(ThemeManager.Sidebar);
            g.FillRectangle(brush, 0, 0, pnlTopBar.Width, pnlTopBar.Height);

            // Bottom shadow line
            using var pen = new Pen(Color.FromArgb(90, 180, 145, 112));
            g.DrawLine(pen, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
        }

        // ── Navigation Helpers ───────────────────────────────────
        private void AddNavSection(Panel parent, string title, ref int y)
        {
            y += 8;
            var lbl = new Label
            {
                Text      = title,
                Location  = new Point(16, y),
                Size      = new Size(200, 18),
                ForeColor = Color.FromArgb(218, 198, 174),
                Font      = new Font("Segoe UI", 7, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
            y += 20;
        }

        private Button AddNavButton(Panel parent, string icon, string label,
                                     ref int y, Action onClick)
        {
            var btn = new Button
            {
                Text      = $"  {icon}  {label}",
                Location  = new Point(0, y),
                Size      = new Size(_sidebarFullWidth, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(236, 226, 214),
                Font      = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor    = Cursors.Hand,
                Tag       = label
            };
            btn.FlatAppearance.BorderSize      = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 126, 89, 62);

            btn.Click += (s, e) =>
            {
                SetActiveNav(btn);
                lblPageTitle.Text = label;
                onClick?.Invoke();
            };
            btn.MouseEnter += (s, e) =>
            {
                if (btn != _activeNavBtn)
                    btn.ForeColor = Color.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn != _activeNavBtn)
                    btn.ForeColor = Color.FromArgb(236, 226, 214);
            };

            parent.Controls.Add(btn);
            y += 42;
            return btn;
        }

        private void SetActiveNav(Button btn)
        {
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Color.FromArgb(236, 226, 214);
            }
            _activeNavBtn           = btn;
            btn.BackColor           = ThemeManager.IsDarkMode ? Color.FromArgb(245, 238, 229) : Color.FromArgb(245, 238, 229);
            btn.ForeColor           = Color.FromArgb(35, 23, 17);
        }

        // ── Module Loading ───────────────────────────────────────
        private void CompactCustomerNavigation(params Button[] visibleButtons)
        {
            foreach (Control control in pnlSidebar.Controls)
            {
                if (control is Label label && label.Parent == pnlSidebar)
                    label.Visible = false;
            }

            int y = 92;
            foreach (var button in visibleButtons)
            {
                button.Visible = true;
                button.Location = new Point(0, y);
                button.Size = new Size(_sidebarFullWidth, 48);
                y += 54;
            }
        }

        private bool IsCustomerUser()
        {
            return string.Equals(SessionManager.Instance.CurrentRole, "Customer",
                StringComparison.OrdinalIgnoreCase);
        }

        private void LoadCustomerHome()
        {
            lblPageTitle.Text = "Point of Sale";
            LoadModule(new POSForm(true));
        }

        private void LoadDashboard()
        {
            lblPageTitle.Text = "Dashboard";
            LoadModule(new DashboardForm());
        }

        private void LoadModule(UserControl module)
        {
            pnlContent.SuspendLayout();
            foreach (Control c in pnlContent.Controls)
                c.Dispose();
            pnlContent.Controls.Clear();

            module.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(module);
            pnlContent.ResumeLayout(true);

            // Slide-in animation
            _ = SlideInModule(module);
        }

        private async Task SlideInModule(Control module)
        {
            module.Left = 40;
            for (int x = 40; x >= 0; x -= 5)
            {
                module.Left = x;
                await Task.Delay(8);
            }
            module.Left = 0;
        }

        // ── Sidebar Toggle Animation ─────────────────────────────
        private void BtnToggleSidebar_Click(object sender, EventArgs e)
        {
            _sidebarExpanded = !_sidebarExpanded;
            int target = _sidebarExpanded ? _sidebarFullWidth : _sidebarMiniWidth;

            _sidebarAnim?.Stop();
            _sidebarAnim = new Timer { Interval = 10 };
            _sidebarAnim.Tick += (ss, ee) =>
            {
                int diff = target - pnlSidebar.Width;
                if (Math.Abs(diff) < 4)
                {
                    pnlSidebar.Width = target;
                    LayoutBodyPanels();
                    _sidebarAnim.Stop();
                    return;
                }
                pnlSidebar.Width += diff / 4;
                LayoutBodyPanels();
            };
            _sidebarAnim.Start();
        }

        // ── Event Handlers ───────────────────────────────────────
        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm("Are you sure you want to logout?", "Logout")) return;
            ServiceContainer.Auth.Logout();
            var login = new LoginForm();
            login.Show();
            Close();
        }

        private void StartTimers()
        {
            // Clock
            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
                lblDateTime.Text = DateTime.Now.ToString("ddd, dd MMM yyyy  |  HH:mm:ss");
            _clockTimer.Start();
            lblDateTime.Text = DateTime.Now.ToString("ddd, dd MMM yyyy  |  HH:mm:ss");

            // Notification badge
            _notifTimer = new Timer { Interval = 15000 };
            _notifTimer.Tick += async (s, e) => await RefreshNotifBadge();
            _notifTimer.Start();
            _ = RefreshNotifBadge();
        }

        private async Task RefreshNotifBadge()
        {
            await Task.Run(() =>
            {
                try
                {
                    var count = ServiceContainer.UserRepo != null
                        ? new DataAccessLayer.NotificationRepository()
                              .GetUnreadCount(SessionManager.Instance.CurrentRole)
                        : 0;
                    Invoke(() =>
                    {
                        lblNotifBadge.Text    = count > 9 ? "9+" : count.ToString();
                        lblNotifBadge.Visible = count > 0;
                    });
                }
                catch { /* ignore */ }
            });
        }

        private void ApplyTheme()
        {
            BackColor        = ThemeManager.Background;
            pnlSidebar.BackColor = ThemeManager.Sidebar;
            pnlTopBar.BackColor  = ThemeManager.Sidebar;
            btnToggleSidebar.ForeColor = Color.White;
            lblPageTitle.ForeColor = Color.White;
            lblDateTime.ForeColor = Color.FromArgb(232, 218, 204);
            btnNotifications.ForeColor = Color.White;
            btnTheme.ForeColor = Color.White;
            lblUserInfo.ForeColor = Color.FromArgb(232, 218, 204);
            pnlContent.BackColor = ThemeManager.Background;
            if (_activeNavBtn != null)
                SetActiveNav(_activeNavBtn);
            pnlSidebar.Invalidate();
            pnlTopBar.Invalidate();
            foreach (Control control in pnlContent.Controls)
                ApplyThemeToChild(control);
        }

        private void ApplyThemeToChild(Control control)
        {
            if (control.Tag?.ToString() == "product" && control is Panel productBar)
            {
                productBar.BackColor = Color.FromArgb(99, 102, 241);
                return;
            }

            if (control is UserControl || control is Panel || control is TableLayoutPanel)
                control.BackColor = ThemeManager.Background;
            else if (control is Label)
                control.ForeColor = ThemeManager.ForeText;
            else if (control is TextBox || control is ComboBox || control is DateTimePicker)
            {
                control.BackColor = ThemeManager.Card;
                control.ForeColor = ThemeManager.ForeText;
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = ThemeManager.Card;
                grid.GridColor = ThemeManager.BorderColor;
                grid.DefaultCellStyle.BackColor = ThemeManager.Card;
                grid.DefaultCellStyle.ForeColor = ThemeManager.RowText;
                grid.AlternatingRowsDefaultCellStyle.BackColor = ThemeManager.IsDarkMode
                    ? Color.FromArgb(55, 37, 29)
                    : Color.FromArgb(245, 238, 229);
                grid.AlternatingRowsDefaultCellStyle.ForeColor = ThemeManager.RowText;
                grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Sidebar;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            }

            foreach (Control child in control.Controls)
                ApplyThemeToChild(child);
        }

        // Allow dragging
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN,
                    NativeMethods.HT_CAPTION, 0);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _clockTimer?.Stop();
            _notifTimer?.Stop();
            base.OnFormClosed(e);
        }
    }

    // Extension helper
    internal static class PanelExtensions
    {
        public static Panel Let(this Panel p, Action<Panel> action) { action(p); return p; }
    }
}
