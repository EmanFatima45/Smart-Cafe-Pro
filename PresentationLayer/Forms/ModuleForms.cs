// ============================================================
// SmartCafe Pro - All Module Forms
// CustomerForm, ProductForm, OrderForm, KitchenForm,
// TableForm, ReservationForm, EmployeeForm, InventoryForm,
// ReportsForm, SettingsForm, NotificationsForm, FeedbackForm
// ============================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.Models;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    // ===========================================================
    // BASE MODULE FORM (shared functionality)
    // ===========================================================
    public abstract class BaseModuleForm : UserControl
    {
        protected Panel      pnlHeader, pnlToolbar, pnlContent;
        protected DataGridView dgvMain;
        protected TextBox    txtSearch;
        protected Button     btnAdd, btnEdit, btnDelete, btnRefresh;

        protected abstract string ModuleTitle  { get; }
        protected abstract string ModuleIcon   { get; }
        protected abstract void   LoadData();
        protected abstract void   OnAdd();
        protected abstract void   OnEdit(DataGridViewRow row);
        protected abstract void   OnDelete(DataGridViewRow row);

        protected void InitBaseLayout()
        {
            BackColor     = ThemeManager.Background;
            Dock          = DockStyle.Fill;
            DoubleBuffered = true;

            // Header
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = ThemeManager.Sidebar };
            var lblTitle = new Label
            {
                Text = $"{ModuleIcon}  {ModuleTitle}",
                Location = new Point(16, 14), Size = new Size(400, 28),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblTitle);

            // Toolbar
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = ThemeManager.Sidebar };

            txtSearch = new TextBox
            {
                Location = new Point(0, 10), Size = new Size(260, 34),
                BackColor = ThemeManager.Card, ForeColor = ThemeManager.ForeText,
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10),
                Padding = new Padding(6),
                PlaceholderText = "🔍  Search..."
            };
            txtSearch.TextChanged += (s, e) => LoadData();

            btnAdd = MakeToolBtn("+ Add", ThemeManager.AccentGreen, new Point(274, 10));
            btnAdd.Click += (s, e) => OnAdd();

            btnEdit = MakeToolBtn("✏ Edit", ThemeManager.AccentBlue, new Point(374, 10));
            btnEdit.Click += (s, e) =>
            {
                if (dgvMain?.CurrentRow != null) OnEdit(dgvMain.CurrentRow);
                else MessageHelper.ShowWarning("Please select a row to edit.");
            };

            btnDelete = MakeToolBtn("🗑 Delete", ThemeManager.AccentRed, new Point(474, 10));
            btnDelete.Click += (s, e) =>
            {
                if (dgvMain?.CurrentRow != null)
                {
                    if (MessageHelper.Confirm("Delete this record?"))
                        OnDelete(dgvMain.CurrentRow);
                }
                else MessageHelper.ShowWarning("Please select a row to delete.");
            };

            btnRefresh = MakeToolBtn("⟳ Refresh", ThemeManager.AccentOrange, new Point(574, 10));
            btnRefresh.Click += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[]
                { txtSearch, btnAdd, btnEdit, btnDelete, btnRefresh });

            // Content
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
            dgvMain = BuildGrid();
            pnlContent.Controls.Add(dgvMain);
            Controls.Add(pnlContent);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlHeader);
        }

        protected DataGridView BuildGrid()
        {
            var grid = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                BackgroundColor         = ThemeManager.Card,
                GridColor               = ThemeManager.BorderColor,
                BorderStyle             = BorderStyle.None,
                RowHeadersVisible       = false,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly                = true,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                Font                    = new Font("Segoe UI", 9),
                MultiSelect             = false,
                CellBorderStyle         = DataGridViewCellBorderStyle.SingleHorizontal
            };
            grid.DefaultCellStyle.BackColor          = ThemeManager.Card;
            grid.DefaultCellStyle.ForeColor          = ThemeManager.RowText;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 218, 204);
            grid.DefaultCellStyle.SelectionForeColor = ThemeManager.RowText;
            grid.DefaultCellStyle.Padding            = new Padding(4, 0, 4, 0);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor  = ThemeManager.Sidebar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding    = new Padding(4, 0, 4, 0);
            grid.ColumnHeadersHeight    = 38;
            grid.RowTemplate.Height     = 40;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 240, 232);
            return grid;
        }

        private Button MakeToolBtn(string text, Color color, Point loc)
        {
            var btn = new Button
            {
                Text = text, Location = loc, Size = new Size(92, 34),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, color),
                ForeColor = color, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, color);
            return btn;
        }

        protected GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath(); int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }
    }

    // ===========================================================
    // CUSTOMER FORM
    // ===========================================================
    public class CustomerForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Customer Management";
        protected override string ModuleIcon  => "👥";

        public CustomerForm()
        {
            InitBaseLayout();
            SetupColumns();
            LoadData();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",         HeaderText = "ID",         Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",        HeaderText = "Name"               });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone",       HeaderText = "Phone",      Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email",       HeaderText = "Email"               });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Membership",  HeaderText = "Membership", Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Points",      HeaderText = "Points",     Width = 75  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSpent",  HeaderText = "Total Spent",Width = 110 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "JoinDate",    HeaderText = "Member Since",Width = 110 });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            _ = Task.Run(() =>
            {
                List<Customer> list;
                try
                {
                    var kw = txtSearch.Text.Trim();
                    list = string.IsNullOrEmpty(kw)
                        ? ServiceContainer.CustomerRepo.GetActive()
                        : ServiceContainer.CustomerRepo.Search(kw);
                }
                catch { list = new List<Customer>(); }
                return list;
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                foreach (var c in t.Result)
                {
                    int i = dgvMain.Rows.Add(
                        c.CustomerID, c.FullName, c.Phone, c.Email,
                        c.MembershipLevel, c.LoyaltyPoints,
                        Formatter.Currency(c.TotalSpent),
                        c.JoinDate.ToString("dd-MMM-yyyy"));
                    dgvMain.Rows[i].Cells["Membership"].Style.ForeColor =
                        ColorTranslator.FromHtml(c.MembershipColor);
                    dgvMain.Rows[i].Tag = c;
                }
            }));
        }

        protected override void OnAdd()
        {
            var dlg = new CustomerEditForm(null);
            if (dlg.ShowDialog() == DialogResult.OK) LoadData();
        }

        protected override void OnEdit(DataGridViewRow row)
        {
            if (row.Tag is Customer c)
            {
                var dlg = new CustomerEditForm(c);
                if (dlg.ShowDialog() == DialogResult.OK) LoadData();
            }
        }

        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is Customer c)
            {
                ServiceContainer.CustomerRepo.Delete(c.CustomerID);
                LoadData();
            }
        }
    }

    // ===========================================================
    // CUSTOMER EDIT FORM
    // ===========================================================
    public class CustomerEditForm : Form
    {
        private Customer _customer;
        private TextBox txFirst, txLast, txPhone, txEmail, txAddress, txNotes;
        private DateTimePicker dtpDOB;
        private Button btnSave, btnCancel;

        public CustomerEditForm(Customer c)
        {
            _customer = c;
            Text = c == null ? "Add Customer" : "Edit Customer";
            Size = new Size(460, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ThemeManager.DarkCard;
            ForeColor = ThemeManager.TextPrimary;
            BuildForm();
            if (c != null) PopulateForm(c);
        }

        private void BuildForm()
        {
            int y = 20;
            AddField("First Name*:", out txFirst,   "", ref y);
            AddField("Last Name*:",  out txLast,    "", ref y);
            AddField("Phone:",       out txPhone,   "", ref y);
            AddField("Email:",       out txEmail,   "", ref y);
            AddField("Address:",     out txAddress, "", ref y);
            AddField("Notes:",       out txNotes,   "", ref y);

            Controls.Add(new Label { Text = "Date of Birth:", Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            dtpDOB = new DateTimePicker { Location = new Point(160, y), Size = new Size(270, 26),
                Format = DateTimePickerFormat.Short, Checked = false, ShowCheckBox = true };
            Controls.Add(dtpDOB); y += 38;

            btnSave = new Button { Text = "💾  Save", Location = new Point(160, y),
                Size = new Size(130, 38), BackColor = ThemeManager.AccentGreen,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "✕  Cancel", Location = new Point(300, y),
                Size = new Size(130, 38), BackColor = ThemeManager.AccentRed,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddField(string lbl, out TextBox tb, string ph, ref int y)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            tb = new TextBox { Location = new Point(160, y), Size = new Size(270, 28),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            Controls.Add(tb); y += 38;
        }

        private void PopulateForm(Customer c)
        {
            txFirst.Text   = c.FirstName;
            txLast.Text    = c.LastName;
            txPhone.Text   = c.Phone ?? "";
            txEmail.Text   = c.Email ?? "";
            txAddress.Text = c.Address ?? "";
            txNotes.Text   = c.Notes ?? "";
            if (c.DateOfBirth.HasValue)
            { dtpDOB.Checked = true; dtpDOB.Value = c.DateOfBirth.Value; }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txFirst.Text) || string.IsNullOrWhiteSpace(txLast.Text))
            { MessageHelper.ShowWarning("First and last name are required."); return; }

            var c = _customer ?? new Customer();
            c.FirstName   = txFirst.Text.Trim();
            c.LastName    = txLast.Text.Trim();
            c.Phone       = EmptyToNull(txPhone.Text);
            c.Email       = EmptyToNull(txEmail.Text);
            c.Address     = EmptyToNull(txAddress.Text);
            c.Notes       = EmptyToNull(txNotes.Text);
            c.DateOfBirth = dtpDOB.Checked ? dtpDOB.Value : (DateTime?)null;

            bool ok = _customer == null
                ? ServiceContainer.CustomerRepo.Insert(c)
                : ServiceContainer.CustomerRepo.Update(c);

            if (ok) { MessageHelper.ShowSuccess("Customer saved!"); DialogResult = DialogResult.OK; }
            else
            {
                var detail = (ServiceContainer.CustomerRepo as SmartCafePro.DataAccessLayer.CustomerRepository)?.LastError;
                MessageHelper.ShowError(string.IsNullOrWhiteSpace(detail)
                    ? "Failed to save customer."
                    : "Failed to save customer: " + detail);
            }
        }

        private static string EmptyToNull(string value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }

    // ===========================================================
    // PRODUCT FORM
    // ===========================================================
    public class ProductForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Menu / Products";
        protected override string ModuleIcon  => "🍽";

        public ProductForm()
        {
            InitBaseLayout();
            SetupColumns();
            LoadData();

            // Extra: toggle availability button
            var btnToggle = new Button
            {
                Text = "🔄 Toggle Available", Location = new Point(680, 10), Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, ThemeManager.AccentOrange),
                ForeColor = ThemeManager.AccentOrange,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnToggle.FlatAppearance.BorderColor = Color.FromArgb(60, ThemeManager.AccentOrange);
            btnToggle.Click += (s, e) =>
            {
                if (dgvMain.CurrentRow?.Tag is Product p)
                {
                    ServiceContainer.ProductRepo.ToggleAvailability(p.ProductID);
                    LoadData();
                }
            };
            pnlToolbar.Controls.Add(btnToggle);
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",       HeaderText = "ID",        Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",     HeaderText = "Product"            });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category",  Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price",    HeaderText = "Price",     Width = 90  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost",     HeaderText = "Cost",      Width = 90  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrepTime", HeaderText = "Prep (min)",Width = 80  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Available",HeaderText = "Available", Width = 80  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Combo",    HeaderText = "Combo",     Width = 65  });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            _ = Task.Run(() =>
            {
                try { return ServiceContainer.ProductRepo.GetAll(); }
                catch { return new List<Product>(); }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                var kw = txtSearch.Text.Trim().ToLower();
                foreach (var p in t.Result)
                {
                    if (!string.IsNullOrEmpty(kw) &&
                        !p.ProductName.ToLower().Contains(kw) &&
                        !(p.CategoryName?.ToLower().Contains(kw) ?? false)) continue;
                    int i = dgvMain.Rows.Add(
                        p.ProductID, p.ProductName, p.CategoryName,
                        Formatter.Currency(p.Price), Formatter.Currency(p.CostPrice),
                        p.PrepTimeMin,
                        p.IsAvailable ? "✓ Yes" : "✗ No",
                        p.IsCombo ? "✓" : "");
                    dgvMain.Rows[i].Cells["Available"].Style.ForeColor =
                        p.IsAvailable ? ThemeManager.AccentGreen : ThemeManager.AccentRed;
                    dgvMain.Rows[i].Tag = p;
                }
            }));
        }

        protected override void OnAdd()
        {
            var dlg = new ProductEditForm(null);
            if (dlg.ShowDialog() == DialogResult.OK) LoadData();
        }

        protected override void OnEdit(DataGridViewRow row)
        {
            if (row.Tag is Product p)
            {
                var dlg = new ProductEditForm(p);
                if (dlg.ShowDialog() == DialogResult.OK) LoadData();
            }
        }

        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is Product p)
            { ServiceContainer.ProductRepo.Delete(p.ProductID); LoadData(); }
        }
    }

    // ===========================================================
    // PRODUCT EDIT FORM
    // ===========================================================
    public class ProductEditForm : Form
    {
        private Product _product;
        private TextBox txName, txDesc, txPrice, txCost, txPrepTime, txCalories, txAllergens, txBarcode;
        private ComboBox cmbCategory;
        private CheckBox chkAvailable, chkCombo;
        private Button btnSave, btnCancel;

        public ProductEditForm(Product p)
        {
            _product = p;
            Text = p == null ? "Add Product" : "Edit Product";
            Size = new Size(480, 580); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = ThemeManager.DarkCard; ForeColor = ThemeManager.TextPrimary;
            BuildForm();
            if (p != null) PopulateForm(p);
        }

        private void BuildForm()
        {
            int y = 20;
            AddField("Product Name*:", out txName,      "", ref y);
            AddField("Description:",  out txDesc,      "", ref y);
            AddField("Price (PKR)*:", out txPrice,     "0", ref y);
            AddField("Cost (PKR):",   out txCost,      "0", ref y);
            AddField("Prep Time (min):", out txPrepTime, "5", ref y);
            AddField("Calories:",     out txCalories,  "", ref y);
            AddField("Allergens:",    out txAllergens, "", ref y);
            AddField("Barcode:",      out txBarcode,   "", ref y);

            Controls.Add(new Label { Text = "Category*:", Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            cmbCategory = new ComboBox { Location = new Point(160, y), Size = new Size(270, 28),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.TextPrimary,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList };
            LoadCategories();
            Controls.Add(cmbCategory); y += 38;

            chkAvailable = new CheckBox { Text = "Available", Location = new Point(160, y),
                ForeColor = ThemeManager.TextPrimary, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10), Checked = true };
            chkCombo = new CheckBox { Text = "Is Combo", Location = new Point(280, y),
                ForeColor = ThemeManager.TextPrimary, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10) };
            Controls.AddRange(new Control[] { chkAvailable, chkCombo }); y += 38;

            btnSave = new Button { Text = "💾  Save", Location = new Point(160, y),
                Size = new Size(130, 38), BackColor = ThemeManager.AccentGreen,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "✕  Cancel", Location = new Point(300, y),
                Size = new Size(130, 38), BackColor = ThemeManager.AccentRed,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddField(string lbl, out TextBox tb, string def, ref int y)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            tb = new TextBox { Location = new Point(160, y), Size = new Size(270, 28),
                Text = def, BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            Controls.Add(tb); y += 38;
        }

        private void LoadCategories()
        {
            try
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT CategoryID,CategoryName FROM Categories WHERE IsActive=1 ORDER BY DisplayOrder", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    cmbCategory.Items.Add(new CategoryItem((int)r["CategoryID"], r["CategoryName"].ToString()));
                if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private void PopulateForm(Product p)
        {
            txName.Text      = p.ProductName;
            txDesc.Text      = p.Description ?? "";
            txPrice.Text     = p.Price.ToString();
            txCost.Text      = p.CostPrice.ToString();
            txPrepTime.Text  = p.PrepTimeMin.ToString();
            txCalories.Text  = p.Calories?.ToString() ?? "";
            txAllergens.Text = p.Allergens ?? "";
            txBarcode.Text   = p.Barcode ?? "";
            chkAvailable.Checked = p.IsAvailable;
            chkCombo.Checked     = p.IsCombo;
            foreach (CategoryItem item in cmbCategory.Items)
                if (item.ID == p.CategoryID) { cmbCategory.SelectedItem = item; break; }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txName.Text))
            { MessageHelper.ShowWarning("Product name is required."); return; }
            if (!decimal.TryParse(txPrice.Text, out decimal price))
            { MessageHelper.ShowWarning("Invalid price."); return; }
            if (cmbCategory.SelectedItem == null)
            { MessageHelper.ShowWarning("Please select a category."); return; }

            var p = _product ?? new Product();
            p.ProductName = txName.Text.Trim();
            p.Description = txDesc.Text.Trim();
            p.Price       = price;
            p.CostPrice   = decimal.TryParse(txCost.Text, out decimal cost) ? cost : 0;
            p.PrepTimeMin = int.TryParse(txPrepTime.Text, out int pt) ? pt : 5;
            p.Calories    = int.TryParse(txCalories.Text, out int cal) ? cal : (int?)null;
            p.Allergens   = txAllergens.Text.Trim();
            p.Barcode     = txBarcode.Text.Trim();
            p.IsAvailable = chkAvailable.Checked;
            p.IsCombo     = chkCombo.Checked;
            p.CategoryID  = ((CategoryItem)cmbCategory.SelectedItem).ID;

            bool ok = _product == null
                ? ServiceContainer.ProductRepo.Insert(p)
                : ServiceContainer.ProductRepo.Update(p);

            if (ok) { MessageHelper.ShowSuccess("Product saved!"); DialogResult = DialogResult.OK; }
            else      MessageHelper.ShowError("Failed to save product.");
        }

        private class CategoryItem
        {
            public int ID; public string Name;
            public CategoryItem(int id, string n) { ID = id; Name = n; }
            public override string ToString() => Name;
        }
    }

    // ===========================================================
    // ORDER FORM (view/manage orders)
    // ===========================================================
    public class OrderForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Order Management";
        protected override string ModuleIcon  => "🛒";

        private ComboBox cmbStatusFilter;
        private Timer _autoRefresh;

        public OrderForm()
        {
            InitBaseLayout();
            SetupColumns();

            cmbStatusFilter = new ComboBox
            {
                Location = new Point(680, 10), Size = new Size(130, 34),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[]
                { "All", "Pending", "Preparing", "Ready", "Completed", "Cancelled" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadData();
            pnlToolbar.Controls.Add(cmbStatusFilter);
            btnAdd.Visible = false; // Orders created via POS
            LoadData();
            _autoRefresh = new Timer { Interval = 5000 };
            _autoRefresh.Tick += (s, e) => LoadData();
            _autoRefresh.Start();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderNum",  HeaderText = "Order #",    Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer",  HeaderText = "Customer"             });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table",     HeaderText = "Table",      Width = 70  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",      HeaderText = "Type",       Width = 90  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Items",     HeaderText = "Items",      Width = 55  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total",     HeaderText = "Total",      Width = 105 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",    HeaderText = "Status",     Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time",      HeaderText = "Time",       Width = 80  });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            var filter = cmbStatusFilter?.Text ?? "All";
            var kw = txtSearch.Text.Trim().ToLower();

            _ = Task.Run(() =>
            {
                var orders = new List<Order>();
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 300
    o.OrderID,
    o.OrderNumber,
    ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(c.FirstName,'') + ' ' + ISNULL(c.LastName,''))), ''), 'Walk-in') AS CustomerName,
    t.TableNumber,
    o.OrderType,
    o.Status,
    o.TotalAmount,
    o.OrderDate,
    COUNT(od.OrderDetailID) AS ItemCount
FROM Orders o
LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
LEFT JOIN CafeTables t ON o.TableID = t.TableID
LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
WHERE o.OrderDate >= DATEADD(DAY, -30, GETDATE())
GROUP BY o.OrderID, o.OrderNumber, c.FirstName, c.LastName, t.TableNumber,
         o.OrderType, o.Status, o.TotalAmount, o.OrderDate
ORDER BY o.OrderDate DESC", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var itemCount = Convert.ToInt32(r["ItemCount"]);
                        orders.Add(new Order
                        {
                            OrderID = Convert.ToInt32(r["OrderID"]),
                            OrderNumber = r["OrderNumber"]?.ToString(),
                            CustomerName = r["CustomerName"]?.ToString(),
                            TableNumber = r["TableNumber"] == DBNull.Value ? "-" : r["TableNumber"]?.ToString(),
                            OrderType = r["OrderType"]?.ToString(),
                            Status = r["Status"]?.ToString(),
                            TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                            OrderDate = Convert.ToDateTime(r["OrderDate"]),
                            Items = Enumerable.Range(0, itemCount)
                                .Select(_ => new OrderDetail { ProductID = 1, Quantity = 1, UnitPrice = 0 })
                                .ToList()
                        });
                    }
                }
                catch { }
                return orders;
            }).ContinueWith(t => Invoke(() =>
            {
                if (IsDisposed) return;
                dgvMain.Rows.Clear();
                if (t.IsFaulted || t.Result == null)
                {
                    return;
                }

                foreach (var o in t.Result)
                {
                    if (filter != "All" && o.Status != filter) continue;
                    if (!string.IsNullOrEmpty(kw) &&
                        !(o.OrderNumber?.ToLower().Contains(kw) ?? false) &&
                        !(o.CustomerName?.ToLower().Contains(kw) ?? false)) continue;

                    int i = dgvMain.Rows.Add(
                        o.OrderNumber,
                        o.CustomerName ?? "Walk-in",
                        o.TableNumber ?? "-",
                        o.OrderType,
                        o.ItemCount,
                        Formatter.Currency(o.TotalAmount),
                        o.Status,
                        o.OrderDate.ToString("HH:mm"));
                    dgvMain.Rows[i].Cells["Status"].Style.ForeColor = Formatter.StatusColor(o.Status);
                    dgvMain.Rows[i].Tag = o;
                }
            }));
        }

        protected override void OnAdd()  { }
        protected override void OnEdit(DataGridViewRow row)
        {
            if (row.Tag is Order o)
            {
                var statuses = new[] { "Pending","Confirmed","Preparing","Ready","Served","Completed","Cancelled" };
                var form = new Form
                {
                    Text = $"Update Order {o.OrderNumber}", Size = new Size(300, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
                    BackColor = ThemeManager.DarkCard
                };
                var cmb = new ComboBox { Location = new Point(20, 60), Size = new Size(240, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ThemeManager.DarkSurface,
                    ForeColor = ThemeManager.TextPrimary, FlatStyle = FlatStyle.Flat };
                cmb.Items.AddRange(statuses);
                cmb.Text = o.Status;
                form.Controls.Add(new Label { Text = "Update Status:", Location = new Point(20,30),
                    Size=new Size(200,22), ForeColor=ThemeManager.TextPrimary });
                form.Controls.Add(cmb);
                var btn = new Button { Text = "Update", Location = new Point(20, 110),
                    Size = new Size(100, 36), BackColor = ThemeManager.AccentGreen,
                    ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btn.Click += (s, e) =>
                {
                    ServiceContainer.OrderRepo.UpdateStatus(o.OrderID, cmb.Text);
                    form.DialogResult = DialogResult.OK;
                    LoadData();
                };
                form.Controls.Add(btn);
                form.ShowDialog();
            }
        }

        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is Order o)
            { ServiceContainer.OrderRepo.Delete(o.OrderID); LoadData(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoRefresh?.Stop();
                _autoRefresh?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ===========================================================
    // KITCHEN FORM
    // ===========================================================
    public class KitchenForm : UserControl
    {
        private FlowLayoutPanel flowPending, flowPreparing, flowReady;
        private Timer _autoRefresh;

        public KitchenForm()
        {
            BackColor = ThemeManager.Background;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
            LoadData();
            _autoRefresh = new Timer { Interval = 15000 };
            _autoRefresh.Tick += (s, e) => LoadData();
            _autoRefresh.Start();
        }

        private void BuildLayout()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Fill, Height = 64, BackColor = ThemeManager.Background };
            pnlHeader.Controls.Add(new Label
            {
                Text = "👨‍🍳  Kitchen Display System", Location = new Point(16, 14),
                Size = new Size(400, 28), ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.Transparent
            });
            var btnRefresh = new Button { Text = "⟳ Refresh", Location = new Point(900, 14),
                Size = new Size(100, 30), BackColor = Color.FromArgb(30, ThemeManager.AccentBlue),
                ForeColor = ThemeManager.AccentBlue, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(60, ThemeManager.AccentBlue);
            btnRefresh.Click += (s, e) => LoadData();
            pnlHeader.Controls.Add(btnRefresh);
            foreach (Control c in pnlHeader.Controls)
                if (c is Label label)
                    label.Text = "Kitchen Display System";
            btnRefresh.Text = "Refresh";

            var pnlCols = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Background };

            // Three Kanban columns
            flowPending  = BuildKanbanColumn(pnlCols, "⏳ Pending",   ThemeManager.AccentOrange, DockStyle.Left,   400);
            flowPreparing = BuildKanbanColumn(pnlCols, "🔥 Preparing", ThemeManager.AccentBlue,  DockStyle.Left,   400);
            flowReady    = BuildKanbanColumn(pnlCols, "✅ Ready",     ThemeManager.AccentGreen,  DockStyle.Fill,   400);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ThemeManager.Background,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(pnlHeader, 0, 0);
            layout.Controls.Add(pnlCols, 0, 1);
            Controls.Add(layout);
        }

        private FlowLayoutPanel BuildKanbanColumn(Panel parent, string title, Color accent,
            DockStyle dock, int width)
        {
            var col = new Panel { Dock = dock, Width = width,
                BackColor = ThemeManager.Background, Padding = new Padding(8) };
            col.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor);
                e.Graphics.DrawLine(pen, col.Width-1, 0, col.Width-1, col.Height);
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };
            header.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(Color.FromArgb(28, accent));
                e.Graphics.FillRectangle(brush, 0, 0, header.Width, header.Height);
                using var pen = new Pen(accent);
                e.Graphics.DrawLine(pen, 0, header.Height-1, header.Width, header.Height-1);
            };
            var cleanTitle = title.Contains("Pending") ? "Pending" :
                             title.Contains("Preparing") ? "Preparing" :
                             title.Contains("Ready") ? "Ready" : title;
            var lblTitle = new Label { Text = cleanTitle, Location = new Point(12,12),
                Size = new Size(300, 24), ForeColor = accent,
                Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = Color.Transparent };
            header.Controls.Add(lblTitle);
            header.Dock = DockStyle.Fill;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ThemeManager.Background,
                Padding = new Padding(0, 4, 0, 0)
            };
            var columnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ThemeManager.Background,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            columnLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            columnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            columnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            columnLayout.Controls.Add(header, 0, 0);
            columnLayout.Controls.Add(flow, 0, 1);
            col.Controls.Add(columnLayout);
            parent.Controls.Add(col);
            return flow;
        }

        private void LoadData()
        {
            var dt = new DataTable();
            try
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    od.OrderDetailID,
    o.OrderNumber,
    o.OrderType,
    t.TableNumber,
    p.ProductName,
    od.Quantity,
    od.KitchenStatus,
    od.Notes,
    o.OrderDate,
    DATEDIFF(MINUTE, o.OrderDate, GETDATE()) AS WaitMinutes
FROM OrderDetails od
INNER JOIN Orders o ON od.OrderID = o.OrderID
INNER JOIN Products p ON od.ProductID = p.ProductID
LEFT JOIN CafeTables t ON o.TableID = t.TableID
WHERE od.KitchenStatus IN ('Pending','Preparing','Ready')
  AND o.Status <> 'Cancelled'
  AND o.OrderDate >= DATEADD(DAY, -7, GETDATE())
ORDER BY
    CASE od.KitchenStatus WHEN 'Pending' THEN 1 WHEN 'Preparing' THEN 2 ELSE 3 END,
    o.OrderDate ASC", conn);
                new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
            }
            catch { }

            flowPending.Controls.Clear();
            flowPreparing.Controls.Clear();
            flowReady.Controls.Clear();

            foreach (DataRow row in dt.Rows)
            {
                var status = row["KitchenStatus"].ToString();
                var card   = BuildKitchenCard(row);
                if (status == "Pending")    flowPending.Controls.Add(card);
                else if (status == "Preparing") flowPreparing.Controls.Add(card);
                else if (status == "Ready") flowReady.Controls.Add(card);
            }

            AddEmptyState(flowPending, "No pending orders");
            AddEmptyState(flowPreparing, "No preparing orders");
            AddEmptyState(flowReady, "No ready orders");
        }

        private void AddEmptyState(FlowLayoutPanel flow, string text)
        {
            if (flow.Controls.Count > 0) return;
            flow.Controls.Add(new Label
            {
                Text = text,
                Size = new Size(360, 64),
                Margin = new Padding(0, 8, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeManager.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10, FontStyle.Italic)
            });
        }

        private Panel BuildKitchenCard(DataRow row)
        {
            var status   = row["KitchenStatus"].ToString();
            var waitMins = Convert.ToInt32(row["WaitMinutes"]);
            var isUrgent = waitMins > 15;

            var card = new Panel
            {
                Size = new Size(370, 110),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = isUrgent ? Color.FromArgb(255, 238, 238) : ThemeManager.Card,
                Cursor = Cursors.Hand
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var accentColor = isUrgent ? ThemeManager.AccentRed : ThemeManager.AccentBlue;
                using var path = RoundRect(card.ClientRectangle, 8);
                using var bg   = new SolidBrush(card.BackColor);
                g.FillPath(bg, path);
                using var acBrush = new SolidBrush(accentColor);
                g.FillRectangle(acBrush, 0, 0, 4, card.Height);
                using var border = new Pen(ThemeManager.BorderColor);
                g.DrawPath(border, path);
            };

            var lblOrder = new Label { Text = $"Order: {row["OrderNumber"]}",
                Location = new Point(14, 10), Size = new Size(200, 20),
                ForeColor = ThemeManager.ForeText, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent };
            var lblType = new Label { Text = $"{row["OrderType"]} {(row["TableNumber"] != DBNull.Value ? "| Table: " + row["TableNumber"] : "")}",
                Location = new Point(14, 30), Size = new Size(250, 18),
                ForeColor = ThemeManager.ForeSecond, Font = new Font("Segoe UI", 8),
                BackColor = Color.Transparent };
            var lblProduct = new Label { Text = $"🍴 {row["ProductName"]} × {row["Quantity"]}",
                Location = new Point(14, 52), Size = new Size(280, 20),
                ForeColor = ThemeManager.ForeText, Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent };
            var lblWait = new Label { Text = $"⏱ {waitMins} min",
                Location = new Point(14, 76), Size = new Size(120, 20),
                ForeColor = isUrgent ? ThemeManager.AccentRed : ThemeManager.AccentGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.Transparent };

            lblProduct.Text = $"{row["ProductName"]} x {row["Quantity"]}";
            lblWait.Text = $"{waitMins} min";

            // Action button
            var nextStatus = status == "Pending" ? "Preparing" : status == "Preparing" ? "Ready" : "Served";
            var btnAction  = new Button
            {
                Text = status == "Pending" ? "Start 🔥" : "Ready ✅",
                Location = new Point(260, 68), Size = new Size(100, 30),
                BackColor = status == "Pending" ? ThemeManager.AccentBlue : ThemeManager.AccentGreen,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnAction.Text = status == "Pending" ? "Start" : status == "Preparing" ? "Ready" : "Served";
            btnAction.BackColor = status == "Pending" ? ThemeManager.AccentBlue :
                                  status == "Preparing" ? ThemeManager.AccentGreen : ThemeManager.AccentOrange;
            btnAction.FlatAppearance.BorderSize = 0;
            var detailID = Convert.ToInt32(row["OrderDetailID"]);
            btnAction.Click += (s, e) =>
            {
                ServiceContainer.Orders.UpdateOrderItemStatus(detailID, nextStatus);
                LoadData();
            };

            card.Controls.AddRange(new Control[] { lblOrder, lblType, lblProduct, lblWait, btnAction });
            return card;
        }

        private GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath(); int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        protected override void Dispose(bool disposing)
        {
            _autoRefresh?.Stop(); _autoRefresh?.Dispose();
            base.Dispose(disposing);
        }
    }

    // ===========================================================
    // TABLE FORM
    // ===========================================================
    public class TableForm : UserControl
    {
        private FlowLayoutPanel flowTables;
        private Timer _autoRefresh;

        public TableForm()
        {
            BackColor = ThemeManager.Background;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
            LoadData();
            _autoRefresh = new Timer { Interval = 20000 };
            _autoRefresh.Tick += (s, e) => LoadData();
            _autoRefresh.Start();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = ThemeManager.Background };
            pnlHeader.Controls.Add(new Label
            {
                Text = "🪑  Table Management", Location = new Point(16, 14),
                Size = new Size(300, 30), ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.Transparent
            });

            var legendX = 340;
            foreach (var (status, color) in new[] {
                ("Available", ThemeManager.AccentGreen), ("Reserved", ThemeManager.AccentOrange),
                ("Occupied", ThemeManager.AccentRed),   ("Cleaning", ThemeManager.AccentBlue) })
            {
                var dot = new Panel { Location = new Point(legendX, 30), Size = new Size(10, 10), BackColor = color };
                var label = new Label
                {
                    Text = status,
                    Location = new Point(legendX + 16, 24),
                    Size = new Size(86, 22),
                    ForeColor = ThemeManager.ForeSecond,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.Transparent
                };
                pnlHeader.Controls.Add(dot);
                pnlHeader.Controls.Add(label);
                legendX += 108;
            }

            flowTables = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, AutoScroll = true,
                BackColor = Color.Transparent, Padding = new Padding(16, 18, 16, 16)
            };
            Controls.Add(flowTables);
            Controls.Add(pnlHeader);
        }

        private void LoadData()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    EnsureTableCleaningSchema(conn);
                    EnsureDefaultCafeTables(conn);
                    ReleaseExpiredCleaningTables(conn);
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT * FROM CafeTables WHERE IsActive=1 ORDER BY TableNumber", conn);
                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch (Exception ex)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("LoadError", typeof(string));
                    dt.Rows.Add(ex.Message);
                    return dt;
                }
            }).ContinueWith(t => Invoke(() =>
            {
                flowTables.Controls.Clear();
                if (t.Result.Columns.Contains("LoadError"))
                {
                    flowTables.Controls.Add(BuildTableMessage("Tables load nahi ho rahi", t.Result.Rows[0]["LoadError"].ToString() ?? "Database error"));
                    return;
                }

                if (t.Result.Rows.Count == 0)
                {
                    flowTables.Controls.Add(BuildTableMessage("No tables found", "Database mein active tables nahi hain. Refresh karein, default tables auto-create ho jayengi."));
                    return;
                }

                foreach (DataRow row in t.Result.Rows)
                    flowTables.Controls.Add(BuildTableCard(row));
            }));
        }

        private Panel BuildTableMessage(string title, string message)
        {
            var card = new Panel
            {
                Size = new Size(520, 150),
                Margin = new Padding(0, 0, 12, 12),
                BackColor = ThemeManager.Card
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(card.ClientRectangle, 12);
                using var brush = new SolidBrush(ThemeManager.Card);
                e.Graphics.FillPath(brush, path);
            };

            card.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(18, 18),
                Size = new Size(480, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Text = message,
                Location = new Point(18, 58),
                Size = new Size(480, 70),
                ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent
            });

            return card;
        }

        private Panel BuildTableCard(DataRow row)
        {
            var tableID = Convert.ToInt32(row["TableID"]);
            var number  = row["TableNumber"].ToString();
            var cap     = Convert.ToInt32(row["Capacity"]);
            var loc     = row["Location"].ToString();
            var status  = row["Status"].ToString();
            var color   = status switch
            {
                "Available" => ThemeManager.AccentGreen,
                "Reserved"  => ThemeManager.AccentOrange,
                "Occupied"  => ThemeManager.AccentRed,
                "Cleaning"  => ThemeManager.AccentBlue,
                _ => ThemeManager.TextSecondary
            };

            var card = new Panel
            {
                Size = new Size(160, 160),
                Margin = new Padding(0, 0, 12, 12),
                BackColor = ThemeManager.Card,
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(card.ClientRectangle, 12);
                using var bg   = new SolidBrush(ThemeManager.Card);
                g.FillPath(bg, path);
                // Status ring
                using var statusBrush = new SolidBrush(Color.FromArgb(30, color));
                g.FillEllipse(statusBrush, 50, 20, 60, 60);
                using var statusPen = new Pen(color, 3);
                g.DrawEllipse(statusPen, 50, 20, 60, 60);
                // Table icon
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("🪑", new Font("Segoe UI", 22), new SolidBrush(color),
                    new RectangleF(50, 20, 60, 60), sf);
            };

            var lblNum = new Label { Text = $"Table {number}",
                Location = new Point(0, 88), Size = new Size(160, 22),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            var lblCap = new Label { Text = $"👤 {cap}  |  {loc}",
                Location = new Point(0, 110), Size = new Size(160, 18),
                ForeColor = ThemeManager.TextSecondary, Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            var lblStatus = new Label { Text = status,
                Location = new Point(0, 130), Size = new Size(160, 20),
                ForeColor = color, Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };

            card.Controls.AddRange(new Control[] { lblNum, lblCap, lblStatus });

            // Right-click context menu for status change
            var menu = new ContextMenuStrip();
            foreach (var s in new[] { "Available", "Reserved", "Occupied", "Cleaning" })
            {
                var item = new ToolStripMenuItem(s);
                var st = s;
                item.Click += (sender, e) =>
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    EnsureTableCleaningSchema(conn);
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "UPDATE CafeTables SET Status=@s, StatusChangedAt=GETDATE() WHERE TableID=@id", conn);
                    cmd.Parameters.AddWithValue("@s",  st);
                    cmd.Parameters.AddWithValue("@id", tableID);
                    cmd.ExecuteNonQuery();
                    LoadData();
                };
                menu.Items.Add(item);
            }
            card.ContextMenuStrip = menu;

            return card;
        }

        private void EnsureTableCleaningSchema(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF COL_LENGTH('dbo.CafeTables', 'StatusChangedAt') IS NULL
    ALTER TABLE dbo.CafeTables ADD StatusChangedAt DATETIME2 NULL;
EXEC(N'UPDATE CafeTables SET StatusChangedAt = ISNULL(StatusChangedAt, GETDATE()) WHERE StatusChangedAt IS NULL');", conn);
            cmd.ExecuteNonQuery();
        }

        private void EnsureDefaultCafeTables(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM CafeTables WHERE IsActive=1)
BEGIN
    EXEC(N'
    MERGE CafeTables AS target
    USING (VALUES
        (''T01'', 2, ''Indoor''),  (''T02'', 2, ''Indoor''),
        (''T03'', 4, ''Indoor''),  (''T04'', 4, ''Indoor''),
        (''T05'', 4, ''Indoor''),  (''T06'', 6, ''Indoor''),
        (''T07'', 6, ''Outdoor''), (''T08'', 4, ''Outdoor''),
        (''T09'', 2, ''Outdoor''), (''T10'', 8, ''VIP''),
        (''T11'', 4, ''VIP''),     (''T12'', 2, ''Window'')
    ) AS src(TableNumber, Capacity, Location)
    ON target.TableNumber = src.TableNumber
    WHEN MATCHED THEN
        UPDATE SET Capacity=src.Capacity, Location=src.Location, IsActive=1,
                   Status=CASE WHEN target.Status IN (''Available'',''Reserved'',''Occupied'',''Cleaning'') THEN target.Status ELSE ''Available'' END,
                   StatusChangedAt=ISNULL(target.StatusChangedAt, GETDATE())
    WHEN NOT MATCHED THEN
        INSERT (TableNumber, Capacity, Location, Status, IsActive, StatusChangedAt)
        VALUES (src.TableNumber, src.Capacity, src.Location, ''Available'', 1, GETDATE());');
END;", conn);
            cmd.ExecuteNonQuery();
        }

        private void ReleaseExpiredCleaningTables(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
UPDATE CafeTables
SET Status='Available', StatusChangedAt=GETDATE()
WHERE Status='Cleaning'
  AND DATEADD(MINUTE, 2, ISNULL(StatusChangedAt, DATEADD(MINUTE, -3, GETDATE()))) <= GETDATE();", conn);
            cmd.ExecuteNonQuery();
        }

        private GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath(); int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        protected override void Dispose(bool d) { _autoRefresh?.Stop(); base.Dispose(d); }
    }

    // ===========================================================
    // RESERVATION FORM
    // ===========================================================
    public class ReservationForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Reservations";
        protected override string ModuleIcon  => "📅";

        public ReservationForm()
        {
            InitBaseLayout();
            btnAdd.Text = "+ Reserve Table";
            btnAdd.Width = 130;
            btnEdit.Location = new Point(414, 10);
            btnDelete.Location = new Point(514, 10);
            btnRefresh.Location = new Point(614, 10);
            if (IsCustomerMode())
            {
                txtSearch.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnRefresh.Visible = false;
                btnAdd.Location = new Point(20, 10);
                btnAdd.Text = "+ Reserve";
                btnAdd.Width = 120;
            }
            else
            {
                btnAdd.Visible = false;
                btnEdit.Location = new Point(274, 10);
                btnDelete.Location = new Point(374, 10);
                btnRefresh.Location = new Point(474, 10);
            }
            SetupColumns();
            LoadData();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",       HeaderText = "ID",       Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer"          });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone",    HeaderText = "Phone",    Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table",    HeaderText = "Table",    Width = 70  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",     HeaderText = "Date",     Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time",     HeaderText = "Time",     Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Guests",   HeaderText = "Guests",   Width = 65  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "Status",   Width = 90  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",    HeaderText = "Notes"             });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    EnsureReservationTables(conn);
                    bool customerMode = IsCustomerMode();
                    int customerID = customerMode ? GetCurrentUserCustomerID(conn) : 0;
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    r.ReservationID,
    r.ReservationDate,
    r.StartTime,
    r.EndTime,
    r.GuestCount,
    r.Status,
    r.SpecialRequests,
    ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(c.FirstName,'') + ' ' + ISNULL(c.LastName,''))), ''), 'Walk-in') AS CustomerName,
    c.Phone,
    t.TableNumber
FROM Reservations r
INNER JOIN Customers c ON r.CustomerID = c.CustomerID
INNER JOIN CafeTables t ON r.TableID = t.TableID
WHERE r.Status NOT IN ('Cancelled','NoShow')
  AND r.ReservationDate >= DATEADD(DAY, -30, CAST(GETDATE() AS DATE))
  AND (@CustomerMode = 0 OR r.CustomerID = @CustomerID)
ORDER BY r.ReservationDate DESC, r.StartTime DESC", conn);
                    cmd.Parameters.AddWithValue("@CustomerMode", customerMode ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CustomerID", customerID);
                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch { return new DataTable(); }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                var kw = txtSearch.Text.Trim().ToLower();
                foreach (DataRow row in t.Result.Rows)
                {
                    if (!string.IsNullOrEmpty(kw) &&
                        !row["CustomerName"].ToString().ToLower().Contains(kw)) continue;
                    var st = row["Status"].ToString();
                    int i = dgvMain.Rows.Add(
                        row["ReservationID"], row["CustomerName"], row["Phone"],
                        row["TableNumber"],
                        Convert.ToDateTime(row["ReservationDate"]).ToString("dd-MMM-yyyy"),
                        $"{row["StartTime"]} - {row["EndTime"]}",
                        row["GuestCount"], st,
                        row["SpecialRequests"]);
                    dgvMain.Rows[i].Cells["Status"].Style.ForeColor = Formatter.StatusColor(st);
                    dgvMain.Rows[i].Tag = row["ReservationID"];
                }
            }));
        }

        protected override void OnAdd()
        {
            if (!IsCustomerMode())
                return;

            ShowReservationDialog();
            return;

            // Simple add reservation dialog
            MessageHelper.ShowWarning("Reservation creation dialog — implement via ReservationEditForm.");
            LoadData();
        }
        private void ShowReservationDialog()
        {
            using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
            conn.Open();
            EnsureReservationTables(conn);

            var tables = LoadReservationTables(conn);
            if (tables.Count == 0)
            {
                MessageHelper.ShowWarning("No active tables found. Please add tables first.");
                return;
            }
            bool customerMode = IsCustomerMode();
            int currentCustomerID = customerMode ? GetCurrentUserCustomerID(conn) : 0;
            var currentCustomer = currentCustomerID > 0 ? GetReservationCustomer(conn, currentCustomerID) : null;

            var form = new Form
            {
                Text = "Reserve Table",
                Size = new Size(460, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.DarkCard
            };

            form.Controls.Add(new Label
            {
                Text = "Reserve Table",
                Location = new Point(24, 18),
                Size = new Size(360, 32),
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            });

            var txtName = AddReservationTextBox(form, "Customer Name", 24, 70, "e.g. Ahmed Ali");
            var txtPhone = AddReservationTextBox(form, "Phone", 24, 125, "03xx-xxxxxxx");
            if (customerMode)
            {
                txtName.Text = currentCustomer?.Name ?? SessionManager.Instance.CurrentUser?.Username ?? "";
                txtPhone.Text = currentCustomer?.Phone ?? "";
                txtName.ReadOnly = true;
                txtPhone.ReadOnly = true;
                txtName.BackColor = ThemeManager.Surface;
                txtPhone.BackColor = ThemeManager.Surface;
            }

            AddReservationLabel(form, "Table", 24, 182);
            var cmbTable = new ComboBox
            {
                Location = new Point(24, 205),
                Size = new Size(390, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cmbTable.Items.AddRange(tables.Cast<object>().ToArray());
            cmbTable.SelectedIndex = 0;
            form.Controls.Add(cmbTable);

            AddReservationLabel(form, "Date", 24, 237);
            var dtDate = new DateTimePicker
            {
                Location = new Point(24, 260),
                Size = new Size(180, 32),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                MinDate = DateTime.Today,
                Value = DateTime.Today
            };
            form.Controls.Add(dtDate);

            AddReservationLabel(form, "Start", 220, 237);
            var dtStart = new DateTimePicker
            {
                Location = new Point(220, 260),
                Size = new Size(90, 32),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(19)
            };
            form.Controls.Add(dtStart);

            AddReservationLabel(form, "End", 324, 237);
            var dtEnd = new DateTimePicker
            {
                Location = new Point(324, 260),
                Size = new Size(90, 32),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(21)
            };
            form.Controls.Add(dtEnd);

            AddReservationLabel(form, "Guests", 24, 292);
            var numGuests = new NumericUpDown
            {
                Location = new Point(24, 315),
                Size = new Size(100, 32),
                Minimum = 1,
                Maximum = 50,
                Value = 2,
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 10)
            };
            form.Controls.Add(numGuests);

            var txtNotes = AddReservationTextBox(form, "Special Requests", 24, 370, "Birthday, window table, etc.");
            txtNotes.Width = 390;

            var btnSave = new Button
            {
                Text = "Reserve Table",
                Location = new Point(24, 430),
                Size = new Size(190, 42),
                BackColor = ThemeManager.AccentGreen,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(224, 430),
                Size = new Size(190, 42),
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnCancel.Click += (s, e) => form.Close();

            btnSave.Click += (s, e) =>
            {
                var name = txtName.Text.Trim();
                var phone = txtPhone.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageHelper.ShowWarning("Customer name required.");
                    return;
                }

                var table = (TableChoice)cmbTable.SelectedItem;
                var date = dtDate.Value.Date;
                var start = dtStart.Value.TimeOfDay;
                var end = dtEnd.Value.TimeOfDay;
                if (end <= start)
                {
                    MessageHelper.ShowWarning("End time start time se baad honi chahiye.");
                    return;
                }

                try
                {
                    using var saveConn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    saveConn.Open();
                    EnsureReservationTables(saveConn);

                    if (HasReservationConflict(saveConn, table.ID, date, start, end))
                    {
                        MessageHelper.ShowWarning("This table is already reserved for this time.");
                        return;
                    }

                    var customerID = customerMode
                        ? GetCurrentUserCustomerID(saveConn)
                        : GetOrCreateReservationCustomer(saveConn, name, phone);
                    if (customerID <= 0)
                    {
                        MessageHelper.ShowWarning("Customer account linked nahi hai. Please customer account se login karein.");
                        return;
                    }
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
INSERT INTO Reservations(CustomerID, TableID, ReservationDate, StartTime, EndTime, GuestCount, Status, SpecialRequests, CreatedBy)
VALUES(@CustomerID, @TableID, @Date, @Start, @End, @Guests, 'Confirmed', @Notes, @CreatedBy);
UPDATE CafeTables SET Status='Reserved' WHERE TableID=@TableID;", saveConn);
                    cmd.Parameters.AddWithValue("@CustomerID", customerID);
                    cmd.Parameters.AddWithValue("@TableID", table.ID);
                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", end);
                    cmd.Parameters.AddWithValue("@Guests", (int)numGuests.Value);
                    cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? DBNull.Value : txtNotes.Text.Trim());
                    cmd.Parameters.AddWithValue("@CreatedBy", SessionManager.Instance.CurrentEmployeeID > 0 ? SessionManager.Instance.CurrentEmployeeID : DBNull.Value);
                    cmd.ExecuteNonQuery();

                    MessageHelper.ShowSuccess("Table reserved successfully.");
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError("Reservation save nahi ho saki: " + ex.Message);
                }
            };

            form.Controls.AddRange(new Control[] { btnSave, btnCancel });
            form.ShowDialog();
        }

        private bool IsCustomerMode()
        {
            return string.Equals(SessionManager.Instance.CurrentRole, "Customer",
                StringComparison.OrdinalIgnoreCase);
        }

        private int GetCurrentUserCustomerID(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            try
            {
                using (var schemaCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Customers' AND COLUMN_NAME='UserID'",
                    conn))
                {
                    if (Convert.ToInt32(schemaCmd.ExecuteScalar()) > 0)
                    {
                        using var userCmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT TOP 1 CustomerID FROM Customers WHERE UserID=@UserID AND IsActive=1 ORDER BY CustomerID DESC",
                            conn);
                        userCmd.Parameters.AddWithValue("@UserID", SessionManager.Instance.CurrentUserID);
                        var value = userCmd.ExecuteScalar();
                        if (value != null && value != DBNull.Value)
                            return Convert.ToInt32(value);
                    }
                }

                string username = SessionManager.Instance.CurrentUser?.Username ?? "";
                if (string.IsNullOrWhiteSpace(username))
                    return 0;

                using var nameCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 1 CustomerID
FROM Customers
WHERE IsActive=1
  AND (FirstName=@Username
       OR LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,'')))=@Username
       OR Email=@Username)
ORDER BY CustomerID DESC", conn);
                nameCmd.Parameters.AddWithValue("@Username", username);
                var fallback = nameCmd.ExecuteScalar();
                return fallback == null || fallback == DBNull.Value ? 0 : Convert.ToInt32(fallback);
            }
            catch
            {
                return 0;
            }
        }

        private (string Name, string Phone)? GetReservationCustomer(Microsoft.Data.SqlClient.SqlConnection conn, int customerID)
        {
            try
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 1
       LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''))) AS CustomerName,
       ISNULL(Phone,'') AS Phone
FROM Customers
WHERE CustomerID=@CustomerID", conn);
                cmd.Parameters.AddWithValue("@CustomerID", customerID);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                return (reader["CustomerName"].ToString(), reader["Phone"].ToString());
            }
            catch
            {
                return null;
            }
        }

        private TextBox AddReservationTextBox(Control parent, string label, int x, int y, string placeholder)
        {
            AddReservationLabel(parent, label, x, y - 23);
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(390, 30),
                BackColor = ThemeManager.DarkSurface,
                ForeColor = ThemeManager.ForeText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = placeholder
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private void AddReservationLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(180, 20),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9)
            });
        }

        private void EnsureReservationTables(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM CafeTables)
BEGIN
    INSERT INTO CafeTables(TableNumber, Capacity, Location, Status)
    VALUES
    (N'T01',4,N'Main Hall',N'Available'),
    (N'T02',4,N'Main Hall',N'Available'),
    (N'T03',2,N'Window',N'Available'),
    (N'T04',6,N'Family Area',N'Available'),
    (N'T05',8,N'VIP',N'Available'),
    (N'T06',4,N'Outdoor',N'Available');
END", conn);
            cmd.ExecuteNonQuery();
        }

        private List<TableChoice> LoadReservationTables(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            var list = new List<TableChoice>();
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT TableID, TableNumber, Capacity, Location, Status FROM CafeTables WHERE IsActive=1 ORDER BY TableNumber", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new TableChoice(
                    Convert.ToInt32(r["TableID"]),
                    r["TableNumber"]?.ToString() ?? "",
                    Convert.ToInt32(r["Capacity"]),
                    r["Location"]?.ToString() ?? "",
                    r["Status"]?.ToString() ?? ""));
            }
            return list;
        }

        private int GetOrCreateReservationCustomer(Microsoft.Data.SqlClient.SqlConnection conn, string fullName, string phone)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                using var find = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT TOP 1 CustomerID FROM Customers WHERE Phone=@Phone", conn);
                find.Parameters.AddWithValue("@Phone", phone);
                var existing = find.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing);
            }

            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0] : fullName;
            var last = parts.Length > 1 ? parts[1] : "-";
            using var insert = new Microsoft.Data.SqlClient.SqlCommand(@"
INSERT INTO Customers(FirstName, LastName, Phone, MembershipLevel, Notes)
OUTPUT INSERTED.CustomerID
VALUES(@First, @Last, @Phone, 'Bronze', 'Created from reservation');", conn);
            insert.Parameters.AddWithValue("@First", first);
            insert.Parameters.AddWithValue("@Last", last);
            insert.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
            return Convert.ToInt32(insert.ExecuteScalar());
        }

        private bool HasReservationConflict(Microsoft.Data.SqlClient.SqlConnection conn, int tableID, DateTime date, TimeSpan start, TimeSpan end)
        {
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT COUNT(*)
FROM Reservations
WHERE TableID=@TableID
  AND ReservationDate=@Date
  AND Status NOT IN ('Cancelled','NoShow','Completed')
  AND StartTime < @End
  AND EndTime > @Start", conn);
            cmd.Parameters.AddWithValue("@TableID", tableID);
            cmd.Parameters.AddWithValue("@Date", date.Date);
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private class TableChoice
        {
            public int ID { get; }
            private string Number { get; }
            private int Capacity { get; }
            private string Location { get; }
            private string Status { get; }

            public TableChoice(int id, string number, int capacity, string location, string status)
            {
                ID = id;
                Number = number;
                Capacity = capacity;
                Location = location;
                Status = status;
            }

            public override string ToString()
                => $"{Number} - {Capacity} seats - {Location} ({Status})";
        }

        public static void EnsureEmployeeData(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF OBJECT_ID('Employees','U') IS NULL
BEGIN
    CREATE TABLE Employees (
        EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NULL,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        HireDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        Department NVARCHAR(100) NULL,
        Position NVARCHAR(100) NULL,
        Salary DECIMAL(10,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;
IF COL_LENGTH('Employees','UserID') IS NULL ALTER TABLE Employees ADD UserID INT NULL;
IF COL_LENGTH('Employees','FirstName') IS NULL ALTER TABLE Employees ADD FirstName NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','LastName') IS NULL ALTER TABLE Employees ADD LastName NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','Phone') IS NULL ALTER TABLE Employees ADD Phone NVARCHAR(20) NULL;
IF COL_LENGTH('Employees','Email') IS NULL ALTER TABLE Employees ADD Email NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','HireDate') IS NULL ALTER TABLE Employees ADD HireDate DATE NOT NULL CONSTRAINT DF_Employees_HireDate DEFAULT CAST(GETDATE() AS DATE);
IF COL_LENGTH('Employees','Department') IS NULL ALTER TABLE Employees ADD Department NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','Position') IS NULL ALTER TABLE Employees ADD Position NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','Salary') IS NULL ALTER TABLE Employees ADD Salary DECIMAL(10,2) NOT NULL CONSTRAINT DF_Employees_Salary DEFAULT 0;
IF COL_LENGTH('Employees','IsActive') IS NULL ALTER TABLE Employees ADD IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1;", conn))
            {
                cmd.ExecuteNonQuery();
            }

            using var seed = new Microsoft.Data.SqlClient.SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='System' AND LastName='Admin')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('System', 'Admin', '+92-300-1111111', 'admin@smartcafe.com', 'Management', 'Administrator', 75000, DATEADD(YEAR, -2, CAST(GETDATE() AS DATE)), 1);
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='Fatima' AND LastName='Ali')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('Fatima', 'Ali', '+92-333-2222222', 'cashier@smartcafe.com', 'Front Office', 'Cashier', 35000, DATEADD(MONTH, -10, CAST(GETDATE() AS DATE)), 1);
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='Hassan' AND LastName='Raza')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('Hassan', 'Raza', '+92-345-3333333', 'waiter@smartcafe.com', 'Service', 'Head Waiter', 28000, DATEADD(MONTH, -8, CAST(GETDATE() AS DATE)), 1);
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='Zainab' AND LastName='Malik')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('Zainab', 'Malik', '+92-311-4444444', 'chef@smartcafe.com', 'Kitchen', 'Head Chef', 45000, DATEADD(MONTH, -14, CAST(GETDATE() AS DATE)), 1);
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='Omar' AND LastName='Sheikh')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('Omar', 'Sheikh', '+92-321-5555555', 'manager@smartcafe.com', 'Operations', 'Cafe Manager', 60000, DATEADD(YEAR, -1, CAST(GETDATE() AS DATE)), 1);
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName='Mariam' AND LastName='Khan')
    INSERT INTO Employees (FirstName, LastName, Phone, Email, Department, Position, Salary, HireDate, IsActive)
    VALUES ('Mariam', 'Khan', '+92-322-6666666', 'barista@smartcafe.com', 'Beverages', 'Barista', 32000, DATEADD(MONTH, -6, CAST(GETDATE() AS DATE)), 1);", conn);
            seed.ExecuteNonQuery();
        }

        protected override void OnEdit(DataGridViewRow row) { LoadData(); }
        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is int id)
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "UPDATE Reservations SET Status='Cancelled' WHERE ReservationID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
            }
        }
    }

    // ===========================================================
    // EMPLOYEE FORM
    // ===========================================================
    public class EmployeeForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Employee Management";
        protected override string ModuleIcon  => "🏢";

        public EmployeeForm()
        {
            InitBaseLayout();
            SetupColumns();
            LoadData();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",         HeaderText = "ID",         Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",        HeaderText = "Full Name"          });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Department",  HeaderText = "Department", Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Position",    HeaderText = "Position",   Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone",       HeaderText = "Phone",      Width = 120 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Salary",      HeaderText = "Salary",     Width = 110 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "HireDate",    HeaderText = "Hire Date",  Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",      HeaderText = "Status",     Width = 80  });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            var kw = txtSearch.Text.Trim();
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    ReservationForm.EnsureEmployeeData(conn);
                    var sql = @"
SELECT
    EmployeeID,
    ISNULL(FirstName, '') AS FirstName,
    ISNULL(LastName, '') AS LastName,
    ISNULL(Department, '') AS Department,
    ISNULL(Position, '') AS Position,
    ISNULL(Phone, '') AS Phone,
    ISNULL(Salary, 0) AS Salary,
    ISNULL(HireDate, CAST(GETDATE() AS DATE)) AS HireDate,
    ISNULL(IsActive, 1) AS IsActive
FROM Employees
WHERE 1=1";
                    if (!string.IsNullOrEmpty(kw)) sql += " AND (FirstName LIKE @kw OR LastName LIKE @kw OR Department LIKE @kw OR Position LIKE @kw OR Phone LIKE @kw)";
                    sql += " ORDER BY IsActive DESC, FirstName, LastName";
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    if (!string.IsNullOrEmpty(kw)) cmd.Parameters.AddWithValue("@kw", $"%{kw}%");
                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch { return new DataTable(); }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                foreach (DataRow row in t.Result.Rows)
                {
                    int i = dgvMain.Rows.Add(
                        row["EmployeeID"],
                        $"{row["FirstName"]} {row["LastName"]}",
                        row["Department"], row["Position"], row["Phone"],
                        Formatter.Currency(Convert.ToDecimal(row["Salary"])),
                        Convert.ToDateTime(row["HireDate"]).ToString("dd-MMM-yyyy"),
                        Convert.ToBoolean(row["IsActive"]) ? "Active" : "Inactive");
                    dgvMain.Rows[i].Tag = row;
                }
                if (dgvMain.Rows.Count == 0 && string.IsNullOrWhiteSpace(kw))
                {
                    dgvMain.Rows.Add(
                        1, "System Admin", "Management", "Administrator",
                        "+92-300-1111111", Formatter.Currency(75000),
                        DateTime.Today.AddYears(-2).ToString("dd-MMM-yyyy"), "Active");
                }
            }));
        }

        protected override void OnAdd()    { MessageHelper.ShowWarning("Employee add form — extend EmployeeEditForm."); LoadData(); }
        protected override void OnEdit(DataGridViewRow row) { LoadData(); }
        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is DataRow dr)
            {
                int id = Convert.ToInt32(dr["EmployeeID"]);
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "UPDATE Employees SET IsActive=0 WHERE EmployeeID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
            }
        }
    }

    // ===========================================================
    // INVENTORY FORM
    // ===========================================================
    public class InventoryForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Inventory Management";
        protected override string ModuleIcon  => "📦";

        public InventoryForm()
        {
            InitBaseLayout();
            SetupColumns();

            // Adjust stock button
            var btnAdjust = new Button
            {
                Text = "📥 Adjust Stock", Location = new Point(680, 10), Size = new Size(140, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, ThemeManager.AccentBlue),
                ForeColor = ThemeManager.AccentBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnAdjust.FlatAppearance.BorderColor = Color.FromArgb(60, ThemeManager.AccentBlue);
            btnAdjust.Click += (s, e) =>
            {
                if (dgvMain.CurrentRow?.Tag is DataRow dr)
                {
                    var id = Convert.ToInt32(dr["InventoryID"]);
                    var qty = Microsoft.VisualBasic.Interaction.InputBox("Enter quantity to add:", "Adjust Stock", "0");
                    if (decimal.TryParse(qty, out decimal q) && q != 0)
                    {
                        var op = q > 0 ? "ADD" : "SUBTRACT";
                        using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                        conn.Open();
                        EnsureInventoryData(conn);
                        try
                        {
                            using var cmd = new Microsoft.Data.SqlClient.SqlCommand("sp_UpdateInventory", conn)
                                { CommandType = CommandType.StoredProcedure };
                            cmd.Parameters.AddWithValue("@InventoryID", id);
                            cmd.Parameters.AddWithValue("@Quantity",    Math.Abs(q));
                            cmd.Parameters.AddWithValue("@Operation",   op);
                            cmd.Parameters.AddWithValue("@UpdatedBy",   DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                        catch
                        {
                            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
UPDATE Inventory
SET CurrentStock = CASE
        WHEN @Operation = 'SUBTRACT' AND CurrentStock - @Quantity < 0 THEN 0
        WHEN @Operation = 'SUBTRACT' THEN CurrentStock - @Quantity
        ELSE CurrentStock + @Quantity
    END,
    LastUpdated = GETDATE()
WHERE InventoryID = @InventoryID;", conn);
                            cmd.Parameters.AddWithValue("@InventoryID", id);
                            cmd.Parameters.AddWithValue("@Quantity", Math.Abs(q));
                            cmd.Parameters.AddWithValue("@Operation", op);
                            cmd.ExecuteNonQuery();
                        }
                        LoadData();
                    }
                }
            };
            pnlToolbar.Controls.Add(btnAdjust);
            LoadData();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",       HeaderText = "ID",       Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Item",     HeaderText = "Item Name"         });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", Width = 110 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock",    HeaderText = "Stock",    Width = 80  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Min",      HeaderText = "Min",      Width = 65  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",     HeaderText = "Unit",     Width = 65  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "Status",   Width = 85  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Expiry",   HeaderText = "Expiry",   Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Supplier", HeaderText = "Supplier", Width = 130 });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    EnsureInventoryData(conn);
                    var kw  = txtSearch.Text.Trim();
                    var sql = @"
SELECT
    i.InventoryID,
    i.ItemName,
    ISNULL(i.Category, '') AS Category,
    i.Unit,
    i.CurrentStock,
    i.MinStockLevel,
    CASE WHEN i.CurrentStock <= i.MinStockLevel THEN 'Low'
         WHEN i.CurrentStock <= i.MinStockLevel * 2 THEN 'Medium'
         ELSE 'Good'
    END AS StockStatus,
    i.ExpiryDate,
    ISNULL(s.SupplierName, '') AS SupplierName
FROM Inventory i
LEFT JOIN Suppliers s ON i.SupplierID = s.SupplierID
WHERE ISNULL(i.IsActive, 1) = 1";
                    if (!string.IsNullOrEmpty(kw)) sql += " AND (i.ItemName LIKE @kw OR i.Category LIKE @kw)";
                    sql += @"
ORDER BY CASE WHEN i.CurrentStock <= i.MinStockLevel THEN 0
              WHEN i.CurrentStock <= i.MinStockLevel * 2 THEN 1
              ELSE 2 END,
         i.ItemName;";
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                    if (!string.IsNullOrEmpty(kw)) cmd.Parameters.AddWithValue("@kw", $"%{kw}%");
                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch { return new DataTable(); }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                foreach (DataRow row in t.Result.Rows)
                {
                    var stockStatus = row["StockStatus"].ToString();
                    var statusColor = stockStatus == "Low" ? ThemeManager.AccentRed
                        : stockStatus == "Medium" ? ThemeManager.AccentOrange : ThemeManager.AccentGreen;
                    int i = dgvMain.Rows.Add(
                        row["InventoryID"], row["ItemName"], row["Category"],
                        row["CurrentStock"], row["MinStockLevel"], row["Unit"],
                        stockStatus,
                        row["ExpiryDate"] != DBNull.Value
                            ? Convert.ToDateTime(row["ExpiryDate"]).ToString("dd-MMM-yyyy") : "-",
                        row["SupplierName"] == DBNull.Value ? "" : row["SupplierName"]);
                    dgvMain.Rows[i].Cells["Status"].Style.ForeColor = statusColor;
                    dgvMain.Rows[i].Tag = row;
                }
            }));
        }

        private static void EnsureInventoryData(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF OBJECT_ID('Inventory','U') IS NULL
BEGIN
    CREATE TABLE Inventory (
        InventoryID INT IDENTITY(1,1) PRIMARY KEY,
        SupplierID INT NULL,
        ItemName NVARCHAR(150) NOT NULL,
        Category NVARCHAR(100) NULL,
        Unit NVARCHAR(30) NOT NULL DEFAULT 'pcs',
        CurrentStock DECIMAL(10,2) NOT NULL DEFAULT 0,
        MinStockLevel DECIMAL(10,2) NOT NULL DEFAULT 5,
        CostPerUnit DECIMAL(10,2) NOT NULL DEFAULT 0,
        ExpiryDate DATE NULL,
        Location NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;
IF COL_LENGTH('Inventory','SupplierID') IS NULL ALTER TABLE Inventory ADD SupplierID INT NULL;
IF COL_LENGTH('Inventory','ItemName') IS NULL ALTER TABLE Inventory ADD ItemName NVARCHAR(150) NULL;
IF COL_LENGTH('Inventory','Category') IS NULL ALTER TABLE Inventory ADD Category NVARCHAR(100) NULL;
IF COL_LENGTH('Inventory','Unit') IS NULL ALTER TABLE Inventory ADD Unit NVARCHAR(30) NOT NULL CONSTRAINT DF_Inventory_Unit DEFAULT 'pcs';
IF COL_LENGTH('Inventory','CurrentStock') IS NULL ALTER TABLE Inventory ADD CurrentStock DECIMAL(10,2) NOT NULL CONSTRAINT DF_Inventory_CurrentStock DEFAULT 0;
IF COL_LENGTH('Inventory','MinStockLevel') IS NULL ALTER TABLE Inventory ADD MinStockLevel DECIMAL(10,2) NOT NULL CONSTRAINT DF_Inventory_MinStockLevel DEFAULT 5;
IF COL_LENGTH('Inventory','CostPerUnit') IS NULL ALTER TABLE Inventory ADD CostPerUnit DECIMAL(10,2) NOT NULL CONSTRAINT DF_Inventory_CostPerUnit DEFAULT 0;
IF COL_LENGTH('Inventory','ExpiryDate') IS NULL ALTER TABLE Inventory ADD ExpiryDate DATE NULL;
IF COL_LENGTH('Inventory','Location') IS NULL ALTER TABLE Inventory ADD Location NVARCHAR(100) NULL;
IF COL_LENGTH('Inventory','IsActive') IS NULL ALTER TABLE Inventory ADD IsActive BIT NOT NULL CONSTRAINT DF_Inventory_IsActive DEFAULT 1;
IF COL_LENGTH('Inventory','LastUpdated') IS NULL ALTER TABLE Inventory ADD LastUpdated DATETIME2 NOT NULL CONSTRAINT DF_Inventory_LastUpdated DEFAULT GETDATE();", conn))
            {
                cmd.ExecuteNonQuery();
            }

            using var seed = new Microsoft.Data.SqlClient.SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Coffee Beans')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Coffee Beans', 'Beverages', 'kg', 45, 10, 1800, DATEADD(MONTH, 8, GETDATE()), 'Dry Store', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Milk')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Milk', 'Dairy', 'liter', 80, 20, 280, DATEADD(DAY, 5, GETDATE()), 'Cold Room', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Sugar')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Sugar', 'Grocery', 'kg', 60, 15, 190, DATEADD(MONTH, 12, GETDATE()), 'Dry Store', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Flour')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Flour', 'Bakery', 'kg', 50, 12, 160, DATEADD(MONTH, 6, GETDATE()), 'Dry Store', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Burger Buns')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Burger Buns', 'Bakery', 'pcs', 120, 30, 45, DATEADD(DAY, 4, GETDATE()), 'Bakery Shelf', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Pizza Dough')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Pizza Dough', 'Bakery', 'pcs', 75, 20, 95, DATEADD(DAY, 3, GETDATE()), 'Cold Room', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Chicken')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Chicken', 'Meat', 'kg', 35, 8, 720, DATEADD(DAY, 4, GETDATE()), 'Freezer', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Beef')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Beef', 'Meat', 'kg', 28, 7, 1100, DATEADD(DAY, 5, GETDATE()), 'Freezer', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Cheese')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Cheese', 'Dairy', 'kg', 32, 8, 1350, DATEADD(DAY, 14, GETDATE()), 'Cold Room', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Potatoes')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Potatoes', 'Vegetables', 'kg', 90, 20, 120, DATEADD(DAY, 20, GETDATE()), 'Vegetable Rack', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Soft Drinks')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Soft Drinks', 'Beverages', 'bottle', 160, 35, 95, DATEADD(MONTH, 10, GETDATE()), 'Beverage Shelf', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Ice Cream Base')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Ice Cream Base', 'Desserts', 'liter', 38, 10, 420, DATEADD(DAY, 12, GETDATE()), 'Freezer', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Chocolate')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Chocolate', 'Desserts', 'kg', 26, 6, 950, DATEADD(MONTH, 6, GETDATE()), 'Dry Store', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Mango Pulp')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Mango Pulp', 'Beverages', 'liter', 24, 6, 380, DATEADD(MONTH, 3, GETDATE()), 'Cold Room', 1);
IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ItemName = 'Cooking Oil')
    INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit, ExpiryDate, Location, IsActive)
    VALUES ('Cooking Oil', 'Kitchen', 'liter', 55, 12, 520, DATEADD(MONTH, 9, GETDATE()), 'Kitchen Store', 1);

UPDATE Inventory SET IsActive = 1 WHERE ISNULL(IsActive, 1) = 0 AND ItemName IN
('Coffee Beans','Milk','Sugar','Flour','Burger Buns','Pizza Dough','Chicken','Beef','Cheese','Potatoes','Soft Drinks','Ice Cream Base','Chocolate','Mango Pulp','Cooking Oil');", conn);
            seed.ExecuteNonQuery();
        }

        protected override void OnAdd()   { MessageHelper.ShowWarning("Inventory add form — extend InventoryEditForm."); }
        protected override void OnEdit(DataGridViewRow row) { }
        protected override void OnDelete(DataGridViewRow row)
        {
            if (row.Tag is DataRow dr)
            {
                int id = Convert.ToInt32(dr["InventoryID"]);
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "UPDATE Inventory SET IsActive=0 WHERE InventoryID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
            }
        }
    }

    // ===========================================================
    // REPORTS FORM
    // ===========================================================
    public class ReportsForm : UserControl
    {
        private DataGridView dgvReport;
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cmbReportType;
        private Label lblSummary;

        public ReportsForm()
        {
            BackColor = ThemeManager.Background;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
            ThemeManager.ThemeChanged += (s, e) =>
            {
                if (IsDisposed) return;
                BackColor = ThemeManager.Background;
                Controls.Clear();
                BuildLayout();
            };
        }

        private void BuildLayout()
        {
            BuildLayoutV2();
            return;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
            pnlHeader.Controls.Add(new Label
            {
                Text = "📈  Analytics & Reports", Location = new Point(16, 14),
                Size = new Size(400, 28), ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.Transparent
            });
            Controls.Add(pnlHeader);

            var pnlFilters = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };

            pnlFilters.Controls.Add(new Label { Text = "Report:", Location = new Point(0, 14),
                Size = new Size(55, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9), BackColor = Color.Transparent });
            cmbReportType = new ComboBox { Location = new Point(58, 10), Size = new Size(200, 32),
                BackColor = ThemeManager.Card, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReportType.Items.AddRange(new object[]
                { "Daily Sales", "Monthly Revenue", "Top Products", "Payment Methods",
                  "Customer Loyalty", "Order Types", "Inventory Status", "Employee Attendance" });
            cmbReportType.SelectedIndex = 0;
            pnlFilters.Controls.Add(cmbReportType);

            pnlFilters.Controls.Add(new Label { Text = "From:", Location = new Point(272, 14),
                Size = new Size(40, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9), BackColor = Color.Transparent });
            dtpFrom = new DateTimePicker { Location = new Point(315, 10), Size = new Size(130, 30),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            pnlFilters.Controls.Add(dtpFrom);

            pnlFilters.Controls.Add(new Label { Text = "To:", Location = new Point(458, 14),
                Size = new Size(25, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9), BackColor = Color.Transparent });
            dtpTo = new DateTimePicker { Location = new Point(486, 10), Size = new Size(130, 30),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlFilters.Controls.Add(dtpTo);

            var btnRun = new Button { Text = "▶ Run", Location = new Point(630, 10),
                Size = new Size(90, 34), BackColor = ThemeManager.AccentPrimary,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += (s, e) => RunReport();
            pnlFilters.Controls.Add(btnRun);

            Controls.Add(pnlFilters);

            lblSummary = new Label { Dock = DockStyle.Top, Height = 38,
                ForeColor = ThemeManager.ForeSecond, Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent, Padding = new Padding(4) };
            Controls.Add(lblSummary);

            dgvReport = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Card,
                GridColor = ThemeManager.BorderColor, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, ReadOnly = true,
                AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9)
            };
            dgvReport.DefaultCellStyle.BackColor = ThemeManager.Card;
            dgvReport.DefaultCellStyle.ForeColor = ThemeManager.RowText;
            dgvReport.EnableHeadersVisualStyles = false;
            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.ColumnHeadersHeight = 36;
            dgvReport.RowTemplate.Height  = 36;
            Controls.Add(dgvReport);

            RunReport();
        }

        private void BuildLayoutV2()
        {
            Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = ThemeManager.Background,
                Padding = new Padding(18, 10, 18, 18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var pnlHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Analytics Reports",
                Location = new Point(14, 12),
                Size = new Size(430, 32),
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            layout.Controls.Add(pnlHeader, 0, 0);

            var pnlFilters = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.DarkSurface,
                Padding = new Padding(14, 10, 14, 10)
            };
            layout.Controls.Add(pnlFilters, 0, 1);

            pnlFilters.Controls.Add(new Label
            {
                Text = "Report",
                Location = new Point(14, 18),
                Size = new Size(55, 20),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            });

            cmbReportType = new ComboBox
            {
                Location = new Point(72, 12),
                Size = new Size(230, 32),
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbReportType.Items.AddRange(new object[]
            {
                "All Records", "Daily Sales", "Monthly Revenue", "Top Products", "Payment Methods",
                "Customer Loyalty", "Order Types", "Inventory Status", "Employee Attendance"
            });
            cmbReportType.SelectedIndex = 0;
            pnlFilters.Controls.Add(cmbReportType);

            pnlFilters.Controls.Add(new Label
            {
                Text = "From",
                Location = new Point(322, 18),
                Size = new Size(40, 20),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            dtpFrom = new DateTimePicker
            {
                Location = new Point(366, 12),
                Size = new Size(140, 30),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30)
            };
            pnlFilters.Controls.Add(dtpFrom);

            pnlFilters.Controls.Add(new Label
            {
                Text = "To",
                Location = new Point(522, 18),
                Size = new Size(25, 20),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            dtpTo = new DateTimePicker
            {
                Location = new Point(550, 12),
                Size = new Size(140, 30),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            pnlFilters.Controls.Add(dtpTo);

            var btnRun = new Button
            {
                Text = "Run",
                Location = new Point(708, 11),
                Size = new Size(120, 34),
                BackColor = ThemeManager.AccentPrimary,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += (s, e) => RunReport();
            pnlFilters.Controls.Add(btnRun);

            lblSummary = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                Padding = new Padding(8, 10, 8, 0)
            };
            layout.Controls.Add(lblSummary, 0, 2);

            dgvReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Card,
                GridColor = ThemeManager.BorderColor,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9),
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvReport.DefaultCellStyle.BackColor = ThemeManager.Card;
            dgvReport.DefaultCellStyle.ForeColor = ThemeManager.RowText;
            dgvReport.DefaultCellStyle.SelectionBackColor = ThemeManager.AccentPrimary;
            dgvReport.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 240, 232);
            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvReport.ColumnHeadersHeight = 40;
            dgvReport.RowTemplate.Height = 38;
            layout.Controls.Add(dgvReport, 0, 3);

            RunReport();
        }

        private void RunReport()
        {
            RunReportV2();
            return;

            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    var report = cmbReportType.Text;
                    Microsoft.Data.SqlClient.SqlCommand cmd;

                    if (report == "Daily Sales")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand("sp_DailySalesReport", conn)
                            { CommandType = CommandType.StoredProcedure };
                        cmd.Parameters.AddWithValue("@Date", dtpFrom.Value.Date);
                    }
                    else if (report == "Monthly Revenue")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand("sp_MonthlyReport", conn)
                            { CommandType = CommandType.StoredProcedure };
                        cmd.Parameters.AddWithValue("@Year",  dtpFrom.Value.Year);
                        cmd.Parameters.AddWithValue("@Month", DBNull.Value);
                    }
                    else if (report == "Top Products")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(
                            @"SELECT TOP 20 p.ProductName, c.CategoryName,
                               SUM(od.Quantity) AS TotalSold,
                               SUM(od.LineTotal) AS TotalRevenue
                              FROM OrderDetails od
                              INNER JOIN Products p ON od.ProductID=p.ProductID
                              INNER JOIN Categories c ON p.CategoryID=c.CategoryID
                              INNER JOIN Orders o ON od.OrderID=o.OrderID
                              WHERE CAST(o.OrderDate AS DATE) BETWEEN @from AND @to
                                AND o.Status='Completed'
                              GROUP BY p.ProductName, c.CategoryName
                              ORDER BY TotalSold DESC", conn);
                        cmd.Parameters.AddWithValue("@from", dtpFrom.Value.Date);
                        cmd.Parameters.AddWithValue("@to",   dtpTo.Value.Date);
                    }
                    else if (report == "Payment Methods")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(
                            @"SELECT PaymentMethod, COUNT(*) AS Transactions, SUM(Amount) AS Total
                              FROM Payments
                              WHERE CAST(PaymentDate AS DATE) BETWEEN @from AND @to
                                AND Status='Completed'
                              GROUP BY PaymentMethod", conn);
                        cmd.Parameters.AddWithValue("@from", dtpFrom.Value.Date);
                        cmd.Parameters.AddWithValue("@to",   dtpTo.Value.Date);
                    }
                    else if (report == "Inventory Status")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT * FROM vw_InventoryStatus ORDER BY StockStatus, ItemName", conn);
                    }
                    else
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(
                            $@"SELECT * FROM fn_GetOrdersByDateRange(@from,@to) ORDER BY OrderDate DESC", conn);
                        cmd.Parameters.AddWithValue("@from", dtpFrom.Value.Date);
                        cmd.Parameters.AddWithValue("@to",   dtpTo.Value.Date);
                    }

                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError(ex.Message);
                    return new DataTable();
                }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvReport.DataSource = t.Result;
                lblSummary.Text = $"  {cmbReportType.Text}  |  {dtpFrom.Value:dd MMM yyyy} – {dtpTo.Value:dd MMM yyyy}  |  {t.Result.Rows.Count} records";
            }));
        }

        private void RunReportV2()
        {
            var report = cmbReportType.Text;
            var from = dtpFrom.Value.Date;
            var to = dtpTo.Value.Date.AddDays(1);

            lblSummary.Text = "Loading analytics records...";
            dgvReport.DataSource = null;

            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    Microsoft.Data.SqlClient.SqlCommand cmd;

                    if (report == "All Records")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    o.OrderNumber AS [Order #],
    CONVERT(varchar(16), o.OrderDate, 120) AS [Date Time],
    ISNULL(c.FirstName + ' ' + c.LastName, 'Walk-in Customer') AS Customer,
    ISNULL(t.TableNumber, '-') AS [Table],
    o.OrderType AS [Type],
    o.Status,
    COUNT(od.OrderDetailID) AS [Items],
    ISNULL(pay.PaymentMethod, '-') AS [Payment],
    CAST(o.SubTotal AS decimal(12,0)) AS [Subtotal],
    CAST(o.DiscountAmt AS decimal(12,0)) AS [Discount],
    CAST(o.TaxAmt AS decimal(12,0)) AS [Tax],
    CAST(o.TotalAmount AS decimal(12,0)) AS [Total]
FROM Orders o
LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
LEFT JOIN CafeTables t ON o.TableID = t.TableID
LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
OUTER APPLY (
    SELECT TOP 1 PaymentMethod
    FROM Payments p
    WHERE p.OrderID = o.OrderID
    ORDER BY p.PaymentDate DESC
) pay
WHERE o.OrderDate >= @from AND o.OrderDate < @to
GROUP BY o.OrderNumber, o.OrderDate, c.FirstName, c.LastName, t.TableNumber,
         o.OrderType, o.Status, pay.PaymentMethod, o.SubTotal, o.DiscountAmt, o.TaxAmt, o.TotalAmount
ORDER BY o.OrderDate DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Daily Sales")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    CAST(o.OrderDate AS date) AS [Sale Date],
    COUNT(*) AS [Orders],
    CAST(SUM(o.SubTotal) AS decimal(12,0)) AS [Subtotal],
    CAST(SUM(o.DiscountAmt) AS decimal(12,0)) AS [Discount],
    CAST(SUM(o.TaxAmt) AS decimal(12,0)) AS [Tax],
    CAST(SUM(o.TotalAmount) AS decimal(12,0)) AS [Total Sales]
FROM Orders o
WHERE o.OrderDate >= @from AND o.OrderDate < @to
GROUP BY CAST(o.OrderDate AS date)
ORDER BY [Sale Date] DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Monthly Revenue")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    YEAR(OrderDate) AS [Year],
    DATENAME(MONTH, OrderDate) AS [Month],
    COUNT(*) AS [Orders],
    CAST(SUM(SubTotal) AS decimal(12,0)) AS [Subtotal],
    CAST(SUM(DiscountAmt) AS decimal(12,0)) AS [Discount],
    CAST(SUM(TaxAmt) AS decimal(12,0)) AS [Tax],
    CAST(SUM(TotalAmount) AS decimal(12,0)) AS [Revenue]
FROM Orders
WHERE OrderDate >= @from AND OrderDate < @to
GROUP BY YEAR(OrderDate), MONTH(OrderDate), DATENAME(MONTH, OrderDate)
ORDER BY [Year] DESC, MONTH(OrderDate) DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Top Products")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 50
    p.ProductName AS [Product],
    c.CategoryName AS [Category],
    SUM(od.Quantity) AS [Sold],
    CAST(SUM(od.LineTotal) AS decimal(12,0)) AS [Revenue]
FROM OrderDetails od
INNER JOIN Products p ON od.ProductID = p.ProductID
INNER JOIN Categories c ON p.CategoryID = c.CategoryID
INNER JOIN Orders o ON od.OrderID = o.OrderID
WHERE o.OrderDate >= @from AND o.OrderDate < @to
  AND o.Status <> 'Cancelled'
GROUP BY p.ProductName, c.CategoryName
ORDER BY [Sold] DESC, [Revenue] DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Payment Methods")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    PaymentMethod AS [Payment Method],
    COUNT(*) AS [Transactions],
    CAST(SUM(Amount) AS decimal(12,0)) AS [Total]
FROM Payments
WHERE PaymentDate >= @from AND PaymentDate < @to
  AND Status = 'Completed'
GROUP BY PaymentMethod
ORDER BY [Total] DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Customer Loyalty")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    c.FirstName + ' ' + c.LastName AS [Customer],
    c.Phone,
    c.MembershipLevel AS [Level],
    c.LoyaltyPoints AS [Points],
    CAST(c.TotalSpent AS decimal(12,0)) AS [Total Spent],
    COUNT(o.OrderID) AS [Orders In Range],
    CAST(ISNULL(SUM(o.TotalAmount), 0) AS decimal(12,0)) AS [Spent In Range]
FROM Customers c
LEFT JOIN Orders o ON c.CustomerID = o.CustomerID
    AND o.OrderDate >= @from AND o.OrderDate < @to
WHERE c.IsActive = 1
GROUP BY c.FirstName, c.LastName, c.Phone, c.MembershipLevel, c.LoyaltyPoints, c.TotalSpent
ORDER BY [Spent In Range] DESC, [Total Spent] DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Order Types")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    OrderType AS [Order Type],
    COUNT(*) AS [Orders],
    CAST(SUM(TotalAmount) AS decimal(12,0)) AS [Total Revenue],
    CAST(AVG(TotalAmount) AS decimal(12,0)) AS [Average Order]
FROM Orders
WHERE OrderDate >= @from AND OrderDate < @to
GROUP BY OrderType
ORDER BY [Orders] DESC;", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }
                    else if (report == "Inventory Status")
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    ItemName AS [Item],
    Category,
    Unit,
    CurrentStock AS [Current Stock],
    MinStockLevel AS [Min Stock],
    CASE WHEN CurrentStock <= MinStockLevel THEN 'Low Stock' ELSE 'Available' END AS [Status],
    Location,
    LastUpdated AS [Last Updated]
FROM Inventory
WHERE IsActive = 1
ORDER BY [Status] DESC, [Item];", conn);
                    }
                    else
                    {
                        cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT
    e.FirstName + ' ' + e.LastName AS [Employee],
    e.Position,
    a.AttendDate AS [Date],
    CONVERT(varchar(8), a.CheckIn, 108) AS [Check In],
    CONVERT(varchar(8), a.CheckOut, 108) AS [Check Out],
    a.Status,
    a.OvertimeHrs AS [Overtime],
    a.Notes
FROM Attendance a
INNER JOIN Employees e ON a.EmployeeID = e.EmployeeID
WHERE a.AttendDate >= @from AND a.AttendDate < @to
ORDER BY a.AttendDate DESC, [Employee];", conn);
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@to", to);
                    }

                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        dt.Columns.Add("Message");
                        dt.Rows.Add("Is date range mein koi analytics record nahi mila.");
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Error");
                    dt.Rows.Add(ex.Message);
                    return dt;
                }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvReport.DataSource = t.Result;
                ApplyAnalyticsGridTheme();
                dgvReport.ClearSelection();
                lblSummary.Text = $"{report} | {from:dd MMM yyyy} - {to.AddDays(-1):dd MMM yyyy} | {t.Result.Rows.Count} records";
            }));
        }

        private void ApplyAnalyticsGridTheme()
        {
            dgvReport.EnableHeadersVisualStyles = false;
            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvReport.DefaultCellStyle.ForeColor = ThemeManager.RowText;
            dgvReport.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 218, 204);
            dgvReport.DefaultCellStyle.SelectionForeColor = ThemeManager.RowText;
        }
    }

    // ===========================================================
    // SETTINGS FORM
    // ===========================================================
    public class SettingsForm : UserControl
    {
        public SettingsForm()
        {
            BackColor = ThemeManager.Background;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Controls.Add(new Label { Text = "⚙  Settings", Location = new Point(16, 14),
                Size = new Size(300, 28), ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), BackColor = Color.Transparent });

            var keys = new[] {
                "CafeName", "CafeAddress", "CafePhone", "CafeEmail",
                "TaxRate", "CurrencySymbol", "LoyaltyRate", "ReceiptFooter" };

            int y = 60;
            var fields = new Dictionary<string, TextBox>();

            foreach (var key in keys)
            {
                Controls.Add(new Label { Text = key + ":", Location = new Point(20, y),
                    Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                    Font = new Font("Segoe UI", 9) });
                var tb = new TextBox { Location = new Point(160, y), Size = new Size(400, 28),
                    Text = ServiceContainer.Settings.Get(key),
                    BackColor = ThemeManager.Card, ForeColor = ThemeManager.ForeText,
                    BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
                Controls.Add(tb);
                fields[key] = tb;
                y += 38;
            }

            // Theme toggle
            Controls.Add(new Label { Text = "Theme:", Location = new Point(20, y),
                Size = new Size(130, 22), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9) });
            var btnThemeDark  = new Button { Text = "🌙 Dark",  Location = new Point(160, y),
                Size = new Size(95, 30), BackColor = ThemeManager.IsDarkMode ? ThemeManager.AccentPrimary : ThemeManager.Surface,
                ForeColor = ThemeManager.IsDarkMode ? Color.White : ThemeManager.ForeText, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnThemeLight = new Button { Text = "☀ Light", Location = new Point(264, y),
                Size = new Size(95, 30), BackColor = !ThemeManager.IsDarkMode ? ThemeManager.AccentPrimary : ThemeManager.Surface,
                ForeColor = !ThemeManager.IsDarkMode ? Color.White : ThemeManager.ForeText, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnThemeDark.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnThemeLight.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnThemeDark.Click  += (s, e) =>
            {
                ServiceContainer.Settings.Set("Theme", "Dark");
                ThemeManager.SetTheme(true);
            };
            btnThemeLight.Click += (s, e) =>
            {
                ServiceContainer.Settings.Set("Theme", "Light");
                ThemeManager.SetTheme(false);
            };
            Controls.AddRange(new Control[] { btnThemeDark, btnThemeLight });
            y += 46;

            // Backup
            var btnBackup = new Button { Text = "💾 Backup Database", Location = new Point(160, y),
                Size = new Size(200, 36), BackColor = ThemeManager.AccentPrimary,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBackup.FlatAppearance.BorderSize = 0;
            btnBackup.Click += (s, e) =>
            {
                var path = ServiceContainer.Settings.Get("BackupPath", @"C:\SmartCafeBackup");
                if (Helpers.BackupHelper.BackupDatabase(path))
                    MessageHelper.ShowSuccess($"Backup saved to {path}");
                else
                    MessageHelper.ShowError("Backup failed. Check SQL Server permissions.");
            };
            Controls.Add(btnBackup);
            y += 50;

            var btnSave = new Button { Text = "💾  Save Settings", Location = new Point(160, y),
                Size = new Size(180, 40), BackColor = ThemeManager.AccentGreen,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                foreach (var kv in fields)
                    ServiceContainer.Settings.Set(kv.Key, kv.Value.Text);
                MessageHelper.ShowSuccess("Settings saved successfully!");
            };
            Controls.Add(btnSave);
        }
    }

    // ===========================================================
    // NOTIFICATIONS FORM
    // ===========================================================
    public class NotificationsForm : UserControl
    {
        public NotificationsForm()
        {
            BackColor = ThemeManager.Background;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var pnlHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlHeader.Controls.Add(new Label { Text = "🔔  Notifications",
                Location = new Point(18, 20), Size = new Size(360, 34),
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 15, FontStyle.Bold), BackColor = Color.Transparent });
            var btnMarkAll = new Button { Text = "✓ Mark All Read", Location = new Point(800, 14),
                Size = new Size(150, 30), BackColor = Color.FromArgb(30, ThemeManager.AccentGreen),
                ForeColor = ThemeManager.AccentGreen, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnMarkAll.FlatAppearance.BorderColor = Color.FromArgb(60, ThemeManager.AccentGreen);
            btnMarkAll.Click += (s, e) =>
            {
                new DataAccessLayer.NotificationRepository()
                    .MarkAllRead(SessionManager.Instance.CurrentRole);
                BuildLayout();
            };
            pnlHeader.Controls.Add(btnMarkAll);
            pnlHeader.Resize += (s, e) =>
                btnMarkAll.Location = new Point(Math.Max(18, pnlHeader.ClientSize.Width - btnMarkAll.Width - 20), 20);
            layout.Controls.Add(pnlHeader, 0, 0);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true,
                BackColor = Color.Transparent, Padding = new Padding(18, 8, 18, 18),
                Margin = new Padding(0)
            };

            try
            {
                var notifs = new DataAccessLayer.NotificationRepository()
                    .GetUnread(SessionManager.Instance.CurrentRole);
                foreach (var n in notifs)
                    flow.Controls.Add(BuildNotifCard(n));
            }
            catch { }

            layout.Controls.Add(flow, 0, 1);
            flow.Resize += (s, e) => ResizeNotificationCards(flow);
            flow.HandleCreated += (s, e) => ResizeNotificationCards(flow);
            ResizeNotificationCards(flow);
        }

        private Panel BuildNotifCard(Notification n)
        {
            var accent = ColorTranslator.FromHtml(n.TypeColor.StartsWith("#") ? n.TypeColor : "#3498DB");
            var card   = new Panel
            {
                Size = new Size(900, 80),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = ThemeManager.Card
            };

            var icon = new Label { Text = n.TypeIcon, Location = new Point(16, 20),
                Size = new Size(30, 40), Font = new Font("Segoe UI", 18),
                ForeColor = accent, BackColor = Color.Transparent };
            var title = new Label { Text = n.Title, Location = new Point(56, 14),
                Size = new Size(600, 22), ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.Transparent };
            var msg = new Label { Text = n.Message, Location = new Point(56, 38),
                Size = new Size(700, 18), ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9), BackColor = Color.Transparent };
            var time = new Label { Text = n.CreatedAt.ToString("dd MMM HH:mm"),
                Location = new Point(820, 30), Size = new Size(120, 18),
                ForeColor = ThemeManager.TextMuted, Font = new Font("Segoe UI", 8),
                BackColor = Color.Transparent };
            var btnRead = new Button { Text = "✓", Location = new Point(760, 26),
                Size = new Size(32, 28), BackColor = Color.FromArgb(30, accent),
                ForeColor = accent, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRead.FlatAppearance.BorderSize = 0;
            var notifID = n.NotificationID;
            btnRead.Click += (s, e) =>
            {
                new DataAccessLayer.NotificationRepository().MarkAsRead(notifID);
                card.Visible = false;
            };

            card.Controls.AddRange(new Control[] { icon, title, msg, time, btnRead });
            card.Resize += (s, e) =>
            {
                int textWidth = Math.Max(240, card.ClientSize.Width - 250);
                title.Width = textWidth;
                msg.Width = textWidth;
                btnRead.Location = new Point(Math.Max(56, card.ClientSize.Width - 156), 26);
                time.Location = new Point(Math.Max(96, card.ClientSize.Width - 116), 30);
            };

            // Accent left bar
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, 4, card.Height);
            };

            return card;
        }

        private void ResizeNotificationCards(FlowLayoutPanel flow)
        {
            int width = Math.Max(640,
                flow.ClientSize.Width - flow.Padding.Left - flow.Padding.Right -
                SystemInformation.VerticalScrollBarWidth - 8);

            foreach (Control control in flow.Controls)
            {
                control.Width = width;
                control.Height = 80;
            }
        }
    }

    // ===========================================================
    // FEEDBACK FORM
    // ===========================================================
    public class FeedbackForm : BaseModuleForm
    {
        protected override string ModuleTitle => "Customer Feedback";
        protected override string ModuleIcon  => "⭐";

        public FeedbackForm()
        {
            InitBaseLayout();
            SetupColumns();
            btnAdd.Text = "+ Feedback";
            btnAdd.Size = new Size(132, 34);
            btnEdit.Location = new Point(420, 10);
            btnDelete.Location = new Point(520, 10);
            btnRefresh.Location = new Point(620, 10);
            if (IsCustomerMode())
            {
                txtSearch.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnRefresh.Visible = false;
                btnAdd.Location = new Point(20, 10);
                btnAdd.Text = "+ Feedback";
                btnAdd.Width = 132;
            }
            else
            {
                btnAdd.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnRefresh.Location = new Point(504, 10);
            }
            LoadData();
        }

        private void SetupColumns()
        {
            dgvMain.Columns.Clear();
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",       HeaderText = "ID",      Width = 50  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer"          });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rating",   HeaderText = "Rating",  Width = 80  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category",Width = 100 });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comment",  HeaderText = "Comment"           });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resolved", HeaderText = "Resolved",Width = 80  });
            dgvMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",     HeaderText = "Date",    Width = 110 });
            dgvMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        protected override void LoadData()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    bool customerMode = IsCustomerMode();
                    int customerID = customerMode ? GetCurrentUserCustomerID(conn) : 0;
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        @"SELECT f.*, ISNULL(c.FirstName+' '+c.LastName,'Anonymous') AS CustomerName
                          FROM Feedback f LEFT JOIN Customers c ON f.CustomerID=c.CustomerID
                          WHERE (@CustomerMode = 0 OR f.CustomerID = @CustomerID)
                          ORDER BY f.CreatedAt DESC", conn);
                    cmd.Parameters.AddWithValue("@CustomerMode", customerMode ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CustomerID", customerID);
                    var dt = new DataTable();
                    new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                    return dt;
                }
                catch { return new DataTable(); }
            }).ContinueWith(t => Invoke(() =>
            {
                dgvMain.Rows.Clear();
                foreach (DataRow row in t.Result.Rows)
                {
                    var rating = Convert.ToInt32(row["Rating"]);
                    var stars  = new string('★', rating) + new string('☆', 5 - rating);
                    int i = dgvMain.Rows.Add(
                        row["FeedbackID"], row["CustomerName"], stars,
                        row["Category"], row["Comment"],
                        Convert.ToBoolean(row["IsResolved"]) ? "✓" : "✗",
                        Convert.ToDateTime(row["CreatedAt"]).ToString("dd-MMM-yyyy HH:mm"));
                    dgvMain.Rows[i].Cells["Rating"].Style.ForeColor = ThemeManager.AccentOrange;
                    dgvMain.Rows[i].Cells["Resolved"].Style.ForeColor =
                        Convert.ToBoolean(row["IsResolved"]) ? ThemeManager.AccentGreen : ThemeManager.AccentRed;
                    dgvMain.Rows[i].Tag = row;
                }
            }));
        }

        protected override void OnAdd()
        {
            if (!IsCustomerMode())
                return;

            ShowFeedbackDialog();
        }

        private void ShowFeedbackDialog()
        {
            using var dlg = new Form
            {
                Text = "Customer Feedback",
                Size = new Size(500, 470),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Surface
            };

            var title = new Label
            {
                Text = "Share Customer Feedback",
                Location = new Point(24, 20),
                Size = new Size(430, 32),
                ForeColor = ThemeManager.AccentPrimary,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            dlg.Controls.Add(title);

            var lblCustomer = MakeDialogLabel("Customer", 24, 72);
            var cmbCustomer = new ComboBox
            {
                Location = new Point(150, 68),
                Size = new Size(300, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            LoadFeedbackCustomers(cmbCustomer);
            if (IsCustomerMode())
            {
                cmbCustomer.Enabled = false;
                cmbCustomer.BackColor = ThemeManager.Surface;
            }
            dlg.Controls.Add(lblCustomer);
            dlg.Controls.Add(cmbCustomer);

            var lblRating = MakeDialogLabel("Rating", 24, 118);
            var rating = new NumericUpDown
            {
                Location = new Point(150, 114),
                Size = new Size(90, 32),
                Minimum = 1,
                Maximum = 5,
                Value = 5,
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                Font = new Font("Segoe UI", 10)
            };
            var ratingHint = new Label
            {
                Text = "1 = Poor, 5 = Excellent",
                Location = new Point(252, 119),
                Size = new Size(200, 22),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };
            dlg.Controls.Add(lblRating);
            dlg.Controls.Add(rating);
            dlg.Controls.Add(ratingHint);

            var lblCategory = MakeDialogLabel("Category", 24, 164);
            var cmbCategory = new ComboBox
            {
                Location = new Point(150, 160),
                Size = new Size(300, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cmbCategory.Items.AddRange(new object[] { "Overall", "Food Quality", "Service", "Ambience", "Cleanliness", "Pricing", "General" });
            cmbCategory.SelectedIndex = 0;
            dlg.Controls.Add(lblCategory);
            dlg.Controls.Add(cmbCategory);

            var lblComment = MakeDialogLabel("Comment", 24, 210);
            var txtComment = new TextBox
            {
                Location = new Point(150, 206),
                Size = new Size(300, 105),
                Multiline = true,
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Customer feedback..."
            };
            dlg.Controls.Add(lblComment);
            dlg.Controls.Add(txtComment);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(240, 350),
                Size = new Size(100, 38),
                DialogResult = DialogResult.Cancel,
                BackColor = ThemeManager.Card,
                ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            var btnSave = new Button
            {
                Text = "Submit Feedback",
                Location = new Point(350, 350),
                Size = new Size(120, 38),
                BackColor = ThemeManager.AccentGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnSave.FlatAppearance.BorderSize = 0;
            dlg.Controls.Add(btnCancel);
            dlg.Controls.Add(btnSave);
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;

            btnSave.Click += (s, e) =>
            {
                try
                {
                    var selected = cmbCustomer.SelectedItem as FeedbackCustomerChoice;
                    using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                    conn.Open();
                    int customerID = IsCustomerMode()
                        ? GetCurrentUserCustomerID(conn)
                        : (selected?.CustomerID ?? 0);
                    if (IsCustomerMode() && customerID <= 0)
                    {
                        MessageHelper.ShowWarning("Customer account linked nahi hai. Please customer account se login karein.");
                        return;
                    }
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
INSERT INTO Feedback(CustomerID, Rating, Category, Comment, IsResolved, CreatedAt)
VALUES(@CustomerID, @Rating, @Category, @Comment, 0, GETDATE());", conn);
                    cmd.Parameters.AddWithValue("@CustomerID", customerID > 0 ? customerID : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rating", Convert.ToInt32(rating.Value));
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Comment", string.IsNullOrWhiteSpace(txtComment.Text) ? DBNull.Value : txtComment.Text.Trim());
                    cmd.ExecuteNonQuery();

                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadData();
                    MessageHelper.ShowSuccess("Feedback saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError("Feedback save nahi ho saka: " + ex.Message);
                }
            };

            dlg.ShowDialog(this);
        }

        private Label MakeDialogLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(110, 24),
                ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };
        }

        private void LoadFeedbackCustomers(ComboBox cmbCustomer)
        {
            cmbCustomer.Items.Clear();

            try
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                if (IsCustomerMode())
                {
                    int customerID = GetCurrentUserCustomerID(conn);
                    var currentCustomer = customerID > 0 ? GetFeedbackCustomer(conn, customerID) : null;
                    cmbCustomer.Items.Add(new FeedbackCustomerChoice(customerID, currentCustomer ?? SessionManager.Instance.CurrentUser?.Username ?? "Customer"));
                    cmbCustomer.SelectedIndex = 0;
                    return;
                }

                cmbCustomer.Items.Add(new FeedbackCustomerChoice(0, "Anonymous Customer"));
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT CustomerID, FirstName + ' ' + LastName AS CustomerName
FROM Customers
WHERE IsActive=1
ORDER BY FirstName, LastName;", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cmbCustomer.Items.Add(new FeedbackCustomerChoice(
                        Convert.ToInt32(reader["CustomerID"]),
                        reader["CustomerName"].ToString() ?? "Customer"));
                }
            }
            catch
            {
                // Keep anonymous feedback available if customers fail to load.
                if (cmbCustomer.Items.Count == 0)
                    cmbCustomer.Items.Add(new FeedbackCustomerChoice(0, IsCustomerMode()
                        ? SessionManager.Instance.CurrentUser?.Username ?? "Customer"
                        : "Anonymous Customer"));
            }

            cmbCustomer.SelectedIndex = 0;
        }

        private bool IsCustomerMode()
        {
            return string.Equals(SessionManager.Instance.CurrentRole, "Customer",
                StringComparison.OrdinalIgnoreCase);
        }

        private int GetCurrentUserCustomerID(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            try
            {
                using (var schemaCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Customers' AND COLUMN_NAME='UserID'",
                    conn))
                {
                    if (Convert.ToInt32(schemaCmd.ExecuteScalar()) > 0)
                    {
                        using var userCmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT TOP 1 CustomerID FROM Customers WHERE UserID=@UserID AND IsActive=1 ORDER BY CustomerID DESC",
                            conn);
                        userCmd.Parameters.AddWithValue("@UserID", SessionManager.Instance.CurrentUserID);
                        var value = userCmd.ExecuteScalar();
                        if (value != null && value != DBNull.Value)
                            return Convert.ToInt32(value);
                    }
                }

                string username = SessionManager.Instance.CurrentUser?.Username ?? "";
                if (string.IsNullOrWhiteSpace(username))
                    return 0;

                using var nameCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 1 CustomerID
FROM Customers
WHERE IsActive=1
  AND (FirstName=@Username
       OR LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,'')))=@Username
       OR Email=@Username)
ORDER BY CustomerID DESC", conn);
                nameCmd.Parameters.AddWithValue("@Username", username);
                var fallback = nameCmd.ExecuteScalar();
                return fallback == null || fallback == DBNull.Value ? 0 : Convert.ToInt32(fallback);
            }
            catch
            {
                return 0;
            }
        }

        private string GetFeedbackCustomer(Microsoft.Data.SqlClient.SqlConnection conn, int customerID)
        {
            try
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP 1 LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''))) AS CustomerName
FROM Customers
WHERE CustomerID=@CustomerID", conn);
                cmd.Parameters.AddWithValue("@CustomerID", customerID);
                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? "" : value.ToString();
            }
            catch
            {
                return "";
            }
        }

        private sealed class FeedbackCustomerChoice
        {
            public int CustomerID { get; }
            private string Name { get; }

            public FeedbackCustomerChoice(int customerID, string name)
            {
                CustomerID = customerID;
                Name = name;
            }

            public override string ToString() => Name;
        }

        protected override void OnEdit(DataGridViewRow row)
        {
            if (IsCustomerMode()) return;
            if (row.Tag is DataRow dr)
            {
                int id = Convert.ToInt32(dr["FeedbackID"]);
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "UPDATE Feedback SET IsResolved=1 WHERE FeedbackID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
                MessageHelper.ShowSuccess("Feedback marked as resolved.");
            }
        }
        protected override void OnDelete(DataGridViewRow row)
        {
            if (IsCustomerMode()) return;
            if (row.Tag is DataRow dr)
            {
                int id = Convert.ToInt32(dr["FeedbackID"]);
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "DELETE FROM Feedback WHERE FeedbackID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
            }
        }
    }
}
