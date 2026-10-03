// ============================================================
// SmartCafe Pro - POSForm.cs
// Full Point-of-Sale with product menu, cart, and payment
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.DataAccessLayer;
using SmartCafePro.Interfaces;
using SmartCafePro.Models;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    public class POSForm : UserControl
    {
        // Left: product catalog
        private Panel         pnlCatalog;
        private FlowLayoutPanel flowCategories;
        private FlowLayoutPanel flowProducts;
        private TextBox       txtSearch;

        // Right: order cart
        private Panel         pnlCart;
        private DataGridView  dgvCart;
        private Label         lblSubtotal, lblDiscount, lblTax, lblTotal;
        private ComboBox      cmbCustomer, cmbTable, cmbOrderType, cmbDiscount, cmbPayment;
        private TextBox       txtNotes, txtAmountPaid, txtChange;
        private Button        btnPlaceOrder, btnClearCart, btnHoldOrder, btnPrintReceipt;
        private Button        btnAddCustomer;

        // State
        private List<Product>     _allProducts   = new();
        private List<Models.Category> _categories = new();
        private Order             _currentOrder  = new();
        private int               _activeCategory = 0;
        private decimal           _taxRate        = 16m;
        private readonly bool      _customerOnlyMode;
        private static readonly Dictionary<string, Image> _productImageCache = new();

        public POSForm(bool customerOnlyMode = false)
        {
            _customerOnlyMode = customerOnlyMode;
            BackColor     = ThemeManager.Background;
            Dock          = DockStyle.Fill;
            DoubleBuffered = true;
            BuildLayout();
            _ = LoadInitialData();
        }

        // ── Layout ───────────────────────────────────────────────
        private void BuildLayout()
        {
            // Left panel (catalog)
            pnlCatalog = new Panel
            {
                Dock      = DockStyle.None,
                BackColor = ThemeManager.Background,
                Padding   = new Padding(12)
            };

            // Search bar
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.Transparent
            };

            txtSearch = new TextBox
            {
                Location    = new Point(0, 8),
                Size        = new Size(260, 36),
                BackColor   = ThemeManager.Card,
                ForeColor   = ThemeManager.ForeText,
                Font        = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                Padding     = new Padding(8),
                Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                PlaceholderText = "🔍  Search products..."
            };
            txtSearch.TextChanged += (s, e) => FilterProducts();
            pnlSearch.Controls.Add(txtSearch);

            // Categories
            flowCategories = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 52,
                BackColor     = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoScroll    = false,
                Padding       = new Padding(0, 4, 0, 0)
            };

            // Products grid
            flowProducts = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                BackColor     = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoScroll    = true,
                Padding       = new Padding(0, 8, 0, 0)
            };

            pnlCatalog.Controls.Add(flowProducts);
            pnlCatalog.Controls.Add(flowCategories);
            pnlCatalog.Controls.Add(pnlSearch);

            Controls.Add(pnlCatalog);

            // Right panel (cart)
            pnlCart = new Panel
            {
                Dock      = DockStyle.None,
                BackColor = ThemeManager.Card,
                Padding   = new Padding(16)
            };
            pnlCart.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor);
                e.Graphics.DrawLine(pen, 0, 0, 0, pnlCart.Height);
            };

            BuildCartPanel();
            Controls.Add(pnlCart);
            Resize += (s, e) => LayoutPosPanels();
            LayoutPosPanels();
        }

        private void LayoutPosPanels()
        {
            if (pnlCatalog == null || pnlCart == null)
                return;

            var width = Math.Max(ClientSize.Width, 760);
            var height = Math.Max(ClientSize.Height, 520);
            var cartWidth = Math.Min(470, Math.Max(420, width / 3));
            var catalogWidth = Math.Max(360, width - cartWidth);

            pnlCatalog.Location = new Point(0, 0);
            pnlCatalog.Size = new Size(catalogWidth, height);
            pnlCart.Location = new Point(catalogWidth, 0);
            pnlCart.Size = new Size(Math.Max(360, width - catalogWidth), height);

            if (txtSearch != null)
                txtSearch.Width = Math.Max(260, pnlCatalog.ClientSize.Width - pnlCatalog.Padding.Horizontal);

            if (dgvCart != null)
                dgvCart.Height = Math.Max(150, Math.Min(220, pnlCart.ClientSize.Height - 520));
        }

        private void BuildCartPanel()
        {
            // Cart title
            var lblCartTitle = new Label
            {
                Text      = "🛒  Current Order",
                Dock      = DockStyle.Top,
                Height    = 36,
                ForeColor = ThemeManager.ForeText,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 8, 0, 0)
            };
            pnlCart.Controls.Add(lblCartTitle);

            // Order type / table / customer row
            var pnlMeta = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.Transparent };

            cmbOrderType = MakeCombo(new[] { "Takeaway", "Dine-In", "Delivery" },
                "Order Type:", new Point(0, 4), new Size(150, 32), pnlMeta);
            cmbOrderType.SelectedIndexChanged += (s, e) =>
            {
                _currentOrder.OrderType = cmbOrderType.Text;
                cmbTable.Visible = cmbOrderType.Text == "Dine-In";
            };

            var lblTableL = new Label { Text = "Table:", Location = new Point(164, 8),
                Size = new Size(40, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent };
            pnlMeta.Controls.Add(lblTableL);
            lblTableL.Visible = false;
            cmbOrderType.SelectedIndexChanged += (s, e) => lblTableL.Visible = cmbOrderType.Text == "Dine-In";

            cmbTable = new ComboBox { Location = new Point(164, 28), Size = new Size(120, 32),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            pnlMeta.Controls.Add(cmbTable);
            cmbTable.Visible = false;

            var lblCustL = new Label { Text = "Customer:", Location = new Point(0, 54),
                Size = new Size(70, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent };
            pnlMeta.Controls.Add(lblCustL);

            cmbCustomer = new ComboBox { Location = new Point(72, 50), Size = new Size(220, 32),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCustomer.SelectedIndexChanged += (s, e) => RefreshCartGrid();
            pnlMeta.Controls.Add(cmbCustomer);

            btnAddCustomer = new Button { Text = "+", Location = new Point(300, 50), Size = new Size(34, 30),
                BackColor = ThemeManager.AccentGreen, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand };
            btnAddCustomer.FlatAppearance.BorderSize = 0;
            btnAddCustomer.Click += (s, e) =>
            {
                var dlg = new CustomerEditForm(null);
                if (dlg.ShowDialog() == DialogResult.OK)
                    _ = LoadCustomers();
            };
            pnlMeta.Controls.Add(btnAddCustomer);
            if (_customerOnlyMode)
            {
                btnAddCustomer.Visible = false;
                cmbCustomer.Enabled = false;
            }

            pnlCart.Controls.Add(pnlMeta);

            // Cart DataGridView
            dgvCart = new DataGridView
            {
                Dock                    = DockStyle.Top,
                Height                  = 220,
                BackgroundColor         = ThemeManager.Card,
                GridColor               = ThemeManager.BorderColor,
                BorderStyle             = BorderStyle.None,
                RowHeadersVisible       = false,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                AllowUserToResizeRows   = false,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly                = false,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.None,
                Font                    = new Font("Segoe UI", 9)
            };
            StyleGrid(dgvCart);

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn  { Name = "ProductName", HeaderText = "Item",  Width = 150, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn  { Name = "UnitPrice",   HeaderText = "Price", Width = 75,  ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Quantity",    HeaderText = "Qty",   Width = 50,  ReadOnly = false });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Discount",    HeaderText = "Disc%", Width = 50,  ReadOnly = false });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn  { Name = "LineTotal",   HeaderText = "Total", Width = 85,  ReadOnly = true });
            var delCol = new DataGridViewButtonColumn { Name = "Remove", HeaderText = "", Text = "X", Width = 30, UseColumnTextForButtonValue = true };
            dgvCart.Columns.Add(delCol);

            dgvCart.CellEndEdit      += DgvCart_CellEndEdit;
            dgvCart.CellClick        += DgvCart_CellClick;
            pnlCart.Controls.Add(dgvCart);

            // Notes
            var lblNotesL = new Label { Text = "Order Notes:", Dock = DockStyle.Top, Height = 20,
                ForeColor = ThemeManager.ForeSecond, Font = new Font("Segoe UI", 8),
                BackColor = Color.Transparent };
            pnlCart.Controls.Add(lblNotesL);

            txtNotes = new TextBox { Dock = DockStyle.Top, Height = 36,
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9),
                PlaceholderText = "Special instructions, allergies..." };
            pnlCart.Controls.Add(txtNotes);

            // Discount strategy picker
            var pnlDiscRow = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent };
            var lblDiscL = new Label { Text = "Discount:", Location = new Point(0, 10),
                Size = new Size(70, 20), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent };
            cmbDiscount = new ComboBox { Location = new Point(72, 6), Size = new Size(200, 28),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDiscount.Items.AddRange(new object[] {
                "No Discount", "5% Off", "10% Off", "15% Off",
                "Happy Hour (15%)", "Loyalty Discount" });
            cmbDiscount.SelectedIndex = 0;
            cmbDiscount.SelectedIndexChanged += (s, e) => RefreshCartGrid();
            pnlDiscRow.Controls.AddRange(new Control[] { lblDiscL, cmbDiscount });
            pnlCart.Controls.Add(pnlDiscRow);

            // Totals panel
            var pnlTotals = new Panel { Dock = DockStyle.Top, Height = 115, BackColor = Color.Transparent };
            int ty = 4;
            lblSubtotal = MakeTotalRow(pnlTotals, "Subtotal:",  ref ty, ThemeManager.ForeSecond);
            lblDiscount = MakeTotalRow(pnlTotals, "Discount:",  ref ty, ThemeManager.AccentGreen);
            lblTax      = MakeTotalRow(pnlTotals, $"Tax ({_taxRate}%):", ref ty, ThemeManager.ForeSecond);

            var sepLine = new Panel { Location = new Point(0, ty), Size = new Size(310, 1),
                BackColor = ThemeManager.BorderColor };
            pnlTotals.Controls.Add(sepLine); ty += 6;

            lblTotal = new Label { Location = new Point(0, ty), Size = new Size(310, 30),
                ForeColor = ThemeManager.AccentGreen,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent };
            pnlTotals.Controls.Add(lblTotal);

            pnlCart.Controls.Add(pnlTotals);

            // Payment row
            var pnlPay = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.Transparent };

            cmbPayment = MakeCombo(new[] { "Cash", "Card", "Online", "Wallet" },
                "Payment:", new Point(0, 4), new Size(130, 32), pnlPay);
            cmbPayment.SelectedIndexChanged += (s, e) => CalcChange();

            var lblAmt = new Label { Text = "Received:", Location = new Point(138, 8),
                Size = new Size(78, 18), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent };
            pnlPay.Controls.Add(lblAmt);

            txtAmountPaid = new TextBox { Location = new Point(138, 28), Size = new Size(88, 30),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10),
                Text = "0" };
            txtAmountPaid.TextChanged += (s, e) => CalcChange();
            pnlPay.Controls.Add(txtAmountPaid);

            var lblChg = new Label { Text = "Change:", Location = new Point(238, 8),
                Size = new Size(82, 18), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent };
            pnlPay.Controls.Add(lblChg);

            txtChange = new TextBox { Location = new Point(238, 28), Size = new Size(98, 30),
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.AccentGreen,
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ReadOnly = true, Text = "0" };
            pnlPay.Controls.Add(txtChange);

            pnlCart.Controls.Add(pnlPay);

            // Action buttons
            var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 124, BackColor = Color.Transparent };

            btnClearCart = MakeActionBtn("Clear", ThemeManager.AccentRed, new Point(0, 8), new Size(150, 44));
            btnClearCart.Click += (s, e) => { _currentOrder.Items.Clear(); RefreshCartGrid(); };

            btnHoldOrder = MakeActionBtn("Hold", ThemeManager.AccentOrange, new Point(160, 8), new Size(150, 44));
            btnHoldOrder.Click += (s, e) => MessageHelper.ShowWarning("Hold order feature - stores to pending queue.", "Hold Order");

            btnPlaceOrder = MakeActionBtn("PLACE ORDER", ThemeManager.AccentGreen, new Point(0, 62), new Size(310, 56));
            btnPlaceOrder.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnPlaceOrder.Click += BtnPlaceOrder_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnClearCart, btnHoldOrder, btnPlaceOrder });
            pnlCart.Controls.Add(pnlBtns);

            pnlCart.Controls.Clear();
            pnlCart.Controls.Add(pnlBtns);
            pnlCart.Controls.Add(pnlPay);
            pnlCart.Controls.Add(pnlTotals);
            pnlCart.Controls.Add(pnlDiscRow);
            pnlCart.Controls.Add(txtNotes);
            pnlCart.Controls.Add(lblNotesL);
            pnlCart.Controls.Add(dgvCart);
            pnlCart.Controls.Add(pnlMeta);
            pnlCart.Controls.Add(lblCartTitle);

            UpdateTotalLabels();
        }

        // ── Data Loading ─────────────────────────────────────────
        private async Task LoadInitialData()
        {
            _taxRate = ServiceContainer.Settings.GetDecimal("TaxRate", 16m);

            await Task.Run(() =>
            {
                try
                {
                    EnsureDefaultMenuData();
                    _allProducts = ServiceContainer.ProductRepo.GetAvailable();
                    NormalizeProductCategories(_allProducts);
                    _categories = BuildCategoriesFromProducts(_allProducts);
                }
                catch
                {
                    _allProducts = BuildFallbackProducts();
                    NormalizeProductCategories(_allProducts);
                    _categories = BuildCategoriesFromProducts(_allProducts);
                }

                if (_allProducts.Count == 0)
                {
                    _allProducts = BuildFallbackProducts();
                    NormalizeProductCategories(_allProducts);
                }

                _categories = BuildCategoriesFromProducts(_allProducts);

                if (_categories.Count == 0)
                    _categories = BuildFallbackCategories();
            });

            Invoke(() =>
            {
                BuildCategoryTabs();
                ShowProducts(_allProducts);
                LoadTables();
            });

            await LoadCustomers();
        }

        private void EnsureDefaultMenuData()
        {
            using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
            conn.Open();
            EnsurePosSchema(conn);
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Coffee')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Coffee',N'Fresh espresso, latte and cafe drinks',1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Cold Drinks')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Cold Drinks',N'Iced coffee, shakes and juices',2);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Snacks')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Snacks',N'Quick bites for cafe orders',3);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Desserts')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Desserts',N'Sweet treats and bakery items',4);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Cakes')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Cakes',N'Fresh cake slices and bakery desserts',4);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Pizza')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Pizza',N'Fresh baked pizzas',5);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Burgers')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Burgers',N'Juicy burgers and sandwiches',6);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Ice Cream')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Ice Cream',N'Sundaes and scoops',7);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Shakes')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Shakes',N'Creamy milkshakes',8);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName=N'Fries')
    INSERT INTO Categories(CategoryName,Description,DisplayOrder) VALUES(N'Fries',N'Crispy fries and loaded sides',9);

DECLARE @Coffee INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Coffee');
DECLARE @Cold INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Cold Drinks');
DECLARE @Snacks INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Snacks');
DECLARE @Desserts INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Desserts');
DECLARE @Cakes INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Cakes');
DECLARE @Pizza INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Pizza');
DECLARE @Burgers INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Burgers');
DECLARE @IceCream INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Ice Cream');
DECLARE @Shakes INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Shakes');
DECLARE @Fries INT = (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName=N'Fries');

IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Cappuccino')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Coffee,N'Cappuccino',N'Creamy espresso with velvet foam',350,180,N'pos:cappuccino',0,1,5,1);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Cafe Latte')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Coffee,N'Cafe Latte',N'Smooth milk coffee with soft finish',380,190,N'pos:latte',0,1,5,2);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Mocha')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Coffee,N'Mocha',N'Chocolate espresso topped with cream',420,210,N'pos:mocha',0,1,6,3);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Iced Coffee')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cold,N'Iced Coffee',N'Chilled coffee over ice',330,150,N'pos:iced-coffee',0,1,4,4);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Mango Shake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cold,N'Mango Shake',N'Fresh mango shake with cream',450,230,N'pos:mango-shake',0,1,5,5);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Club Sandwich')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Snacks,N'Club Sandwich',N'Toasted sandwich with fries',650,360,N'pos:club-sandwich',0,1,12,6);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Loaded Fries')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Snacks,N'Loaded Fries',N'Crispy fries with cheese sauce',480,240,N'pos:loaded-fries',0,1,8,7);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Chocolate Cake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Desserts,N'Chocolate Cake',N'Rich slice with chocolate glaze',520,270,N'pos:chocolate-cake',0,1,3,8);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Brownie Sundae')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Desserts,N'Brownie Sundae',N'Warm brownie with ice cream',590,300,N'pos:brownie-sundae',0,1,4,9);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Chicken Fajita Pizza')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Pizza,N'Chicken Fajita Pizza',N'Spicy chicken pizza with melted cheese',1250,700,N'pos:pizza',0,1,16,10);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Cheese Lover Pizza')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Pizza,N'Cheese Lover Pizza',N'Extra cheese pizza with golden crust',1150,650,N'pos:cheese-pizza',0,1,15,11);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Zinger Burger')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Burgers,N'Zinger Burger',N'Crispy chicken burger with special sauce',680,360,N'pos:zinger-burger',0,1,10,12);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Beef Burger')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Burgers,N'Beef Burger',N'Grilled beef patty with cheese',760,420,N'pos:beef-burger',0,1,12,13);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Vanilla Ice Cream')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@IceCream,N'Vanilla Ice Cream',N'Two scoops with wafer',280,120,N'pos:vanilla-icecream',0,1,2,14);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Strawberry Ice Cream')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@IceCream,N'Strawberry Ice Cream',N'Strawberry scoops with syrup',320,140,N'pos:strawberry-icecream',0,1,2,15);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Black Forest Cake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cakes,N'Black Forest Cake',N'Chocolate cake with cream and cherry',560,280,N'pos:black-forest-cake',0,1,3,16);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Cheese Cake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cakes,N'Cheese Cake',N'Creamy cheesecake slice',620,320,N'pos:cheese-cake',0,1,3,17);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Pepsi')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cold,N'Pepsi',N'Chilled cold drink',180,90,N'pos:pepsi',0,1,1,18);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Lemon Mint')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Cold,N'Lemon Mint',N'Fresh lemon mint cooler',360,160,N'pos:lemon-mint',0,1,4,19);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Oreo Shake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Shakes,N'Oreo Shake',N'Creamy Oreo milkshake',520,260,N'pos:oreo-shake',0,1,5,20);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Chocolate Shake')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Shakes,N'Chocolate Shake',N'Thick chocolate milkshake',500,250,N'pos:chocolate-shake',0,1,5,21);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Nuggets')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Snacks,N'Nuggets',N'Crispy chicken nuggets',430,220,N'pos:nuggets',0,1,7,22);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Chicken Wings')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Snacks,N'Chicken Wings',N'Spicy crispy wings',590,310,N'pos:wings',0,1,10,23);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'French Fries')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Fries,N'French Fries',N'Crispy golden fries',300,120,N'pos:french-fries',0,1,6,24);
IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName=N'Cheese Fries')
    INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,ImagePath,IsCombo,IsAvailable,PrepTimeMin,DisplayOrder)
    VALUES(@Fries,N'Cheese Fries',N'Fries topped with cheese sauce',430,210,N'pos:cheese-fries',0,1,7,25);

UPDATE Categories SET IsActive=1
WHERE CategoryName IN (N'Coffee',N'Cold Drinks',N'Snacks',N'Desserts',N'Cakes',N'Pizza',N'Burgers',N'Ice Cream',N'Shakes',N'Fries');

UPDATE Products SET IsActive=1, IsAvailable=1
WHERE ProductName IN (N'Cappuccino',N'Cafe Latte',N'Mocha',N'Iced Coffee',N'Mango Shake',N'Club Sandwich',N'Loaded Fries',N'Chocolate Cake',N'Brownie Sundae',
N'Chicken Fajita Pizza',N'Cheese Lover Pizza',N'Zinger Burger',N'Beef Burger',N'Vanilla Ice Cream',N'Strawberry Ice Cream',N'Black Forest Cake',
N'Cheese Cake',N'Pepsi',N'Lemon Mint',N'Oreo Shake',N'Chocolate Shake',N'Nuggets',N'Chicken Wings',N'French Fries',N'Cheese Fries');

UPDATE Products SET CategoryID=@Cakes WHERE ProductName IN (N'Black Forest Cake',N'Cheese Cake');
UPDATE Products SET CategoryID=@Pizza WHERE ProductName IN (N'Chicken Fajita Pizza',N'Cheese Lover Pizza');
UPDATE Products SET CategoryID=@Burgers WHERE ProductName IN (N'Zinger Burger',N'Beef Burger');
UPDATE Products SET CategoryID=@IceCream WHERE ProductName IN (N'Vanilla Ice Cream',N'Strawberry Ice Cream');
UPDATE Products SET CategoryID=@Shakes WHERE ProductName IN (N'Oreo Shake',N'Chocolate Shake');
UPDATE Products SET CategoryID=@Fries WHERE ProductName IN (N'French Fries',N'Cheese Fries');",
                conn) { CommandType = System.Data.CommandType.Text };
            cmd.ExecuteNonQuery();
        }

        private void EnsurePosSchema(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF COL_LENGTH('dbo.Categories', 'IsActive') IS NULL
    ALTER TABLE dbo.Categories ADD IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT(1);
IF COL_LENGTH('dbo.Categories', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Categories ADD UpdatedAt DATETIME2 NULL;",
                conn) { CommandType = System.Data.CommandType.Text })
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
IF COL_LENGTH('dbo.Products', 'IsActive') IS NULL
    ALTER TABLE dbo.Products ADD IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1);
IF COL_LENGTH('dbo.Products', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Products ADD UpdatedAt DATETIME2 NULL;",
                conn) { CommandType = System.Data.CommandType.Text })
            {
                cmd.ExecuteNonQuery();
            }
        }

        private List<Models.Category> BuildFallbackCategories() => new()
        {
            new Models.Category { CategoryID = 1, CategoryName = "Coffee" },
            new Models.Category { CategoryID = 2, CategoryName = "Cold Drinks" },
            new Models.Category { CategoryID = 3, CategoryName = "Shakes" },
            new Models.Category { CategoryID = 4, CategoryName = "Cakes" },
            new Models.Category { CategoryID = 5, CategoryName = "Pizza" },
            new Models.Category { CategoryID = 6, CategoryName = "Burgers" },
            new Models.Category { CategoryID = 7, CategoryName = "Ice Cream" },
            new Models.Category { CategoryID = 8, CategoryName = "Snacks" },
            new Models.Category { CategoryID = 9, CategoryName = "Fries" }
        };

        private void NormalizeProductCategories(List<Product> products)
        {
            foreach (var product in products)
            {
                var category = GuessCategoryForProduct(product.ProductName);
                product.CategoryName = category;
                product.CategoryID = CategoryIdForName(category);
            }
        }

        private List<Models.Category> BuildCategoriesFromProducts(List<Product> products)
        {
            return products
                .Where(p => p.IsAvailable && p.IsActive && !string.IsNullOrWhiteSpace(p.CategoryName))
                .GroupBy(p => new { p.CategoryID, p.CategoryName })
                .Select(g => new Models.Category
                {
                    CategoryID = g.Key.CategoryID,
                    CategoryName = g.Key.CategoryName
                })
                .OrderBy(c => CategoryIdForName(c.CategoryName))
                .ThenBy(c => c.CategoryName)
                .ToList();
        }

        private int CategoryIdForName(string categoryName) => categoryName switch
        {
            "Coffee" => 1,
            "Cold Drinks" => 2,
            "Shakes" => 3,
            "Cakes" => 4,
            "Pizza" => 5,
            "Burgers" => 6,
            "Ice Cream" => 7,
            "Snacks" => 8,
            "Fries" => 9,
            _ => 99
        };

        private List<Product> BuildFallbackProducts() => new()
        {
            new Product { ProductID = 1, CategoryID = 1, CategoryName = "Pizza", ProductName = "Chicken Fajita Pizza", Description = "Spicy chicken pizza", Price = 1250, IsAvailable = true, IsActive = true },
            new Product { ProductID = 2, CategoryID = 1, CategoryName = "Pizza", ProductName = "Cheese Lover Pizza", Description = "Extra cheese pizza", Price = 1150, IsAvailable = true, IsActive = true },
            new Product { ProductID = 3, CategoryID = 2, CategoryName = "Burgers", ProductName = "Zinger Burger", Description = "Crispy chicken burger", Price = 680, IsAvailable = true, IsActive = true },
            new Product { ProductID = 4, CategoryID = 2, CategoryName = "Burgers", ProductName = "Beef Burger", Description = "Grilled beef burger", Price = 760, IsAvailable = true, IsActive = true },
            new Product { ProductID = 5, CategoryID = 3, CategoryName = "Ice Cream", ProductName = "Vanilla Ice Cream", Description = "Two scoops with wafer", Price = 280, IsAvailable = true, IsActive = true },
            new Product { ProductID = 6, CategoryID = 3, CategoryName = "Ice Cream", ProductName = "Strawberry Ice Cream", Description = "Strawberry scoops", Price = 320, IsAvailable = true, IsActive = true },
            new Product { ProductID = 7, CategoryID = 4, CategoryName = "Cakes", ProductName = "Black Forest Cake", Description = "Chocolate cream cake", Price = 560, IsAvailable = true, IsActive = true },
            new Product { ProductID = 8, CategoryID = 4, CategoryName = "Cakes", ProductName = "Cheese Cake", Description = "Creamy cheesecake slice", Price = 620, IsAvailable = true, IsActive = true },
            new Product { ProductID = 9, CategoryID = 5, CategoryName = "Cold Drinks", ProductName = "Pepsi", Description = "Chilled cold drink", Price = 180, IsAvailable = true, IsActive = true },
            new Product { ProductID = 10, CategoryID = 5, CategoryName = "Cold Drinks", ProductName = "Lemon Mint", Description = "Fresh cooler", Price = 360, IsAvailable = true, IsActive = true },
            new Product { ProductID = 11, CategoryID = 6, CategoryName = "Shakes", ProductName = "Oreo Shake", Description = "Creamy Oreo shake", Price = 520, IsAvailable = true, IsActive = true },
            new Product { ProductID = 12, CategoryID = 6, CategoryName = "Shakes", ProductName = "Chocolate Shake", Description = "Thick chocolate shake", Price = 500, IsAvailable = true, IsActive = true },
            new Product { ProductID = 13, CategoryID = 7, CategoryName = "Snacks", ProductName = "Nuggets", Description = "Crispy chicken nuggets", Price = 430, IsAvailable = true, IsActive = true },
            new Product { ProductID = 14, CategoryID = 7, CategoryName = "Snacks", ProductName = "Chicken Wings", Description = "Spicy crispy wings", Price = 590, IsAvailable = true, IsActive = true },
            new Product { ProductID = 15, CategoryID = 8, CategoryName = "Fries", ProductName = "French Fries", Description = "Crispy golden fries", Price = 300, IsAvailable = true, IsActive = true },
            new Product { ProductID = 16, CategoryID = 8, CategoryName = "Fries", ProductName = "Cheese Fries", Description = "Fries with cheese sauce", Price = 430, IsAvailable = true, IsActive = true }
        };

        private List<Models.Category> GetCategories()
        {
            var cats = new List<Models.Category>();
            using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT CategoryID,CategoryName FROM Categories WHERE IsActive=1 ORDER BY DisplayOrder", conn)
                { CommandType = System.Data.CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read())
                cats.Add(new Models.Category
                {
                    CategoryID   = (int)r["CategoryID"],
                    CategoryName = r["CategoryName"].ToString()
                });
            return cats;
        }

        private void BuildCategoryTabs()
        {
            flowCategories.Controls.Clear();
            var btnAll = MakeCategoryTab("All", 0, true);
            flowCategories.Controls.Add(btnAll);

            foreach (var cat in _categories)
                flowCategories.Controls.Add(MakeCategoryTab(cat.CategoryName, cat.CategoryID));
        }

        private Button MakeCategoryTab(string name, int id, bool active = false)
        {
            var btn = new Button
            {
                Text      = name,
                Size      = new Size(Math.Max(80, name.Length * 9 + 20), 38),
                Margin    = new Padding(0, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? ThemeManager.AccentPrimary : ThemeManager.Card,
                ForeColor = active ? Color.White : ThemeManager.ForeSecond,
                Font      = new Font("Segoe UI", 9, active ? FontStyle.Bold : FontStyle.Regular),
                Cursor    = Cursors.Hand,
                Tag       = id
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                _activeCategory = id;
                // Reset all tabs
                foreach (Control c in flowCategories.Controls)
                    if (c is Button b)
                    {
                        b.BackColor = ThemeManager.Card;
                        b.ForeColor = ThemeManager.ForeSecond;
                        b.Font = new Font("Segoe UI", 9);
                    }
                btn.BackColor = ThemeManager.AccentPrimary;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                FilterProducts();
            };
            return btn;
        }

        private void FilterProducts()
        {
            var keyword  = txtSearch.Text.Trim().ToLower();
            var filtered = _allProducts.Where(p =>
                (_activeCategory == 0 || p.CategoryID == _activeCategory) &&
                (string.IsNullOrEmpty(keyword) ||
                 p.ProductName.ToLower().Contains(keyword) ||
                 (p.Description?.ToLower().Contains(keyword) ?? false))
            ).ToList();
            ShowProducts(filtered);
        }

        private void ShowProducts(List<Product> products)
        {
            flowProducts.SuspendLayout();
            flowProducts.Controls.Clear();

            foreach (var p in products)
                flowProducts.Controls.Add(MakeProductCard(p));

            flowProducts.ResumeLayout(true);
        }

        private Panel MakeProductCard(Product p)
        {
            var card = new Panel
            {
                Size      = new Size(188, 190),
                Margin    = new Padding(0, 0, 12, 12),
                BackColor = ThemeManager.Card,
                Cursor    = Cursors.Hand,
                Tag       = p
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(card.ClientRectangle, 10);
                using var bg   = new SolidBrush(ThemeManager.Card);
                g.FillPath(bg, path);
                if (!p.IsAvailable)
                {
                    using var overlay = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                    g.FillPath(overlay, path);
                }
            };

            // Product image area
            var pnlImg = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(188, 106),
                BackColor = Color.Transparent
            };
            pnlImg.Paint += (s, e) => PaintProductPicture(e.Graphics, pnlImg.ClientRectangle, p);

            card.Controls.Add(pnlImg);

            var lblName = new Label
            {
                Text      = p.ProductName,
                Location  = new Point(10, 112),
                Size      = new Size(168, 34),
                ForeColor = ThemeManager.ForeText,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            var lblPrice = new Label
            {
                Text      = Formatter.Currency(p.Price),
                Location  = new Point(10, 150),
                Size      = new Size(168, 26),
                ForeColor = ThemeManager.AccentGreen,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { pnlImg, lblName, lblPrice });

            // Hover
            Action highlight   = () => card.BackColor = Color.FromArgb(55, 55, 70);
            Action unhighlight = () => card.BackColor = ThemeManager.Card;
            card.MouseEnter  += (s, e) => highlight();
            card.MouseLeave  += (s, e) => unhighlight();
            foreach (Control c in card.Controls)
            {
                c.MouseEnter += (s, e) => highlight();
                c.MouseLeave += (s, e) => unhighlight();
            }

            // Click = add to cart
            Action addToCart = () =>
            {
                if (!p.IsAvailable) { MessageHelper.ShowWarning("This item is currently unavailable."); return; }
                AddToCart(p);
            };
            card.Click     += (s, e) => addToCart();
            lblName.Click  += (s, e) => addToCart();
            lblPrice.Click += (s, e) => addToCart();
            pnlImg.Click   += (s, e) => addToCart();

            return card;
        }

        private void PaintProductPicture(Graphics g, Rectangle rect, Product p)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var imageRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            using var path = RoundRect(imageRect, 12);

            var photo = TryGetProductPhoto(p);
            if (photo != null)
            {
                var oldClip = g.Clip;
                g.SetClip(path);
                g.DrawImage(photo, GetCoverDestinationRect(photo, imageRect));

                using var overlay = new LinearGradientBrush(imageRect,
                    Color.FromArgb(0, Color.Black),
                    Color.FromArgb(45, Color.Black),
                    LinearGradientMode.Vertical);
                g.FillRectangle(overlay, imageRect);
                g.Clip = oldClip;
            }
            else
            {
                var accent = GetProductAccent(p);
                using var brush = new LinearGradientBrush(rect,
                    Color.FromArgb(235, accent),
                    Color.FromArgb(120, ThemeManager.AccentPrimary),
                    LinearGradientMode.ForwardDiagonal);
                g.FillPath(brush, path);
            }

            using var border = new Pen(Color.FromArgb(45, ThemeManager.AccentPrimary));
            g.DrawPath(border, path);
        }

        private Image TryGetProductPhoto(Product p)
        {
            var fileName = GetProductPhotoFileName(p);
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            if (_productImageCache.TryGetValue(fileName, out var cached))
                return cached;

            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "ProductImages", fileName);
            if (!File.Exists(path))
            {
                path = Path.Combine(Application.StartupPath, "Assets", "ProductImages", fileName);
                if (!File.Exists(path))
                    return null;
            }

            var image = Image.FromFile(path);
            _productImageCache[fileName] = image;
            return image;
        }

        private Rectangle GetCoverDestinationRect(Image image, Rectangle target)
        {
            var imageRatio = image.Width / (float)image.Height;
            var targetRatio = target.Width / (float)target.Height;

            int width;
            int height;
            if (imageRatio > targetRatio)
            {
                height = target.Height;
                width = (int)(height * imageRatio);
            }
            else
            {
                width = target.Width;
                height = (int)(width / imageRatio);
            }

            return new Rectangle(
                target.X + (target.Width - width) / 2,
                target.Y + (target.Height - height) / 2,
                width,
                height);
        }

        private string GetProductPhotoFileName(Product p)
        {
            var key = $"{p.CategoryName} {p.ProductName}".ToLower();
            if (key.Contains("latte") || key.Contains("mocha")) return "latte.jpg";
            if (key.Contains("iced coffee")) return "iced-coffee.jpg";
            if (key.Contains("coffee") || key.Contains("cappuccino")) return "coffee.jpg";
            if (key.Contains("pepsi")) return "pepsi.jpg";
            if (key.Contains("lemon")) return "lemon-mint.jpg";
            if (key.Contains("cold")) return "cold-drink.jpg";
            if (key.Contains("shake") || key.Contains("mango")) return "shake.jpg";
            if (key.Contains("cheese cake") || key.Contains("cheesecake")) return "cheesecake.jpg";
            if (key.Contains("cake") || key.Contains("brownie")) return "cake.jpg";
            if (key.Contains("pizza")) return "pizza.jpg";
            if (key.Contains("burger")) return "burger.jpg";
            if (key.Contains("ice cream") || key.Contains("icecream") || key.Contains("ice ")) return "ice-cream.jpg";
            if (key.Contains("sandwich")) return "sandwich.jpg";
            if (key.Contains("wings")) return "wings.jpg";
            if (key.Contains("nuggets")) return "nuggets.jpg";
            if (key.Contains("fries")) return "fries.jpg";
            return "coffee.jpg";
        }

        private Color GetProductAccent(Product p)
        {
            var key = $"{p.CategoryName} {p.ProductName}".ToLower();
            if (key.Contains("pizza")) return Color.FromArgb(218, 76, 54);
            if (key.Contains("burger")) return Color.FromArgb(180, 100, 48);
            if (key.Contains("ice cream") || key.Contains("icecream")) return Color.FromArgb(236, 112, 164);
            if (key.Contains("mango")) return Color.FromArgb(255, 177, 48);
            if (key.Contains("chocolate") || key.Contains("brownie") || key.Contains("mocha")) return Color.FromArgb(128, 72, 48);
            if (key.Contains("fries")) return Color.FromArgb(247, 170, 44);
            if (key.Contains("wings") || key.Contains("nuggets")) return Color.FromArgb(214, 96, 54);
            if (key.Contains("sandwich")) return Color.FromArgb(80, 164, 100);
            if (key.Contains("cold") || key.Contains("iced") || key.Contains("pepsi") || key.Contains("lemon")) return Color.FromArgb(45, 185, 210);
            if (key.Contains("dessert") || key.Contains("cake")) return Color.FromArgb(210, 92, 142);
            return Color.FromArgb(167, 112, 70);
        }

        private string GetProductIcon(Product p)
        {
            var key = $"{p.CategoryName} {p.ProductName}".ToLower();
            if (key.Contains("pizza")) return "\U0001F355";
            if (key.Contains("burger")) return "\U0001F354";
            if (key.Contains("ice cream") || key.Contains("icecream")) return "\U0001F368";
            if (key.Contains("cake") || key.Contains("dessert") || key.Contains("brownie")) return "\U0001F370";
            if (key.Contains("shake") || key.Contains("mango")) return "\U0001F95B";
            if (key.Contains("cold") || key.Contains("pepsi") || key.Contains("lemon")) return "\U0001F964";
            if (key.Contains("fries")) return "\U0001F35F";
            if (key.Contains("wings") || key.Contains("nuggets") || key.Contains("snack")) return "\U0001F357";
            if (key.Contains("sandwich")) return "\U0001F96A";
            if (key.Contains("coffee") || key.Contains("latte") || key.Contains("mocha")) return "\u2615";
            return "\U0001F37D";
        }

        // ── Cart Operations ──────────────────────────────────────
        private void AddToCart(Product p)
        {
            var existing = _currentOrder.Items.FirstOrDefault(x => x.ProductID == p.ProductID);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _currentOrder.Items.Add(new OrderDetail
                {
                    ProductID   = p.ProductID,
                    ProductName = p.ProductName,
                    UnitPrice   = p.Price,
                    Quantity    = 1,
                    Discount    = 0
                });
            }
            RefreshCartGrid();
        }

        private void RefreshCartGrid()
        {
            dgvCart.Rows.Clear();
            var orderDiscountPercent = GetSelectedOrderDiscountPercent();
            foreach (var item in _currentOrder.Items)
            {
                var displayDiscount = orderDiscountPercent > 0 ? orderDiscountPercent : item.Discount;
                var displayLineTotal = item.Quantity * item.UnitPrice * (1 - displayDiscount / 100);
                dgvCart.Rows.Add(
                    item.ProductName,
                    Formatter.Currency(item.UnitPrice),
                    item.Quantity,
                    displayDiscount,
                    Formatter.Currency(displayLineTotal),
                    "✕");
            }
            RecalculateTotals();
        }

        private decimal GetSelectedOrderDiscountPercent()
        {
            if (cmbDiscount == null) return 0;

            return cmbDiscount.SelectedIndex switch
            {
                1 => 5,
                2 => 10,
                3 => 15,
                4 => 15,
                5 => GetSelectedCustomerLoyaltyDiscount(),
                _ => 0
            };
        }

        private decimal GetSelectedCustomerLoyaltyDiscount()
        {
            if (cmbCustomer?.SelectedItem is not CustomerComboItem ci || ci.ID <= 0)
                return 0;

            try
            {
                return ServiceContainer.CustomerRepo.GetByID(ci.ID)?.LoyaltyDiscount ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private void DgvCart_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentOrder.Items.Count) return;
            var item = _currentOrder.Items[e.RowIndex];
            var cell = dgvCart.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (e.ColumnIndex == 2) // Quantity
            {
                if (int.TryParse(cell.Value?.ToString(), out int qty) && qty > 0)
                    item.Quantity = qty;
                else
                    { _currentOrder.Items.RemoveAt(e.RowIndex); RefreshCartGrid(); return; }
            }
            else if (e.ColumnIndex == 3) // Discount
            {
                if (decimal.TryParse(cell.Value?.ToString(), out decimal disc) && disc >= 0 && disc <= 100)
                    item.Discount = disc;
            }
            RefreshCartGrid();
        }

        private void DgvCart_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5 && e.RowIndex >= 0 && e.RowIndex < _currentOrder.Items.Count)
            {
                _currentOrder.Items.RemoveAt(e.RowIndex);
                RefreshCartGrid();
            }
        }

        private void RecalculateTotals()
        {
            if (_currentOrder?.Items == null) return;

            // Apply discount strategy
            IDiscountStrategy strategy = cmbDiscount?.SelectedIndex switch
            {
                1 => new PercentageDiscount(5),
                2 => new PercentageDiscount(10),
                3 => new PercentageDiscount(15),
                4 => new HappyHourDiscount(),
                5 => new LoyaltyDiscount(),
                _ => new NoDiscount()
            };

            Customer customer = null;
            if (cmbCustomer?.SelectedItem is CustomerComboItem ci)
                customer = ServiceContainer.CustomerRepo.GetByID(ci.ID);

            // Recalculate
            decimal sub = _currentOrder.Items.Sum(x => x.LineTotal);
            _currentOrder.SubTotal   = sub;
            _currentOrder.DiscountAmt = strategy.CalculateDiscount(_currentOrder, customer);
            _currentOrder.TaxAmt     = (sub - _currentOrder.DiscountAmt) * _taxRate / 100;
            _currentOrder.TotalAmount = sub - _currentOrder.DiscountAmt + _currentOrder.TaxAmt;

            UpdateTotalLabels();
            CalcChange();
        }

        private void UpdateTotalLabels()
        {
            lblSubtotal.Text = $"Subtotal:   {Formatter.Currency(_currentOrder.SubTotal)}";
            lblDiscount.Text = $"Discount:  -{Formatter.Currency(_currentOrder.DiscountAmt)}";
            lblTax.Text      = $"Tax ({_taxRate}%): {Formatter.Currency(_currentOrder.TaxAmt)}";
            lblTotal.Text    = $"TOTAL:  {Formatter.Currency(GetPayableTotal())}";
            txtAmountPaid.Text = GetPayableTotal().ToString("F0");
        }

        private void CalcChange()
        {
            if (decimal.TryParse(txtAmountPaid.Text, out decimal paid))
            {
                var change = paid - GetPayableTotal();
                txtChange.Text      = Formatter.Currency(Math.Max(0, change));
                txtChange.ForeColor = change < 0 ? ThemeManager.AccentRed : ThemeManager.AccentGreen;
            }
        }

        private decimal GetPayableTotal() => Math.Ceiling(_currentOrder.TotalAmount);

        // ── Place Order ──────────────────────────────────────────
        private void BtnPlaceOrder_Click(object sender, EventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            { MessageHelper.ShowWarning("Please add items to the order."); return; }

            var payableTotal = GetPayableTotal();
            if (!decimal.TryParse(txtAmountPaid.Text, out decimal paid) || paid < payableTotal)
                if (cmbPayment.Text == "Cash")
                { MessageHelper.ShowError($"Received amount kam hai. Total {Formatter.Currency(payableTotal)} receive karo."); return; }

            _currentOrder.OrderType  = cmbOrderType.Text;
            _currentOrder.Notes      = txtNotes.Text;
            _currentOrder.EmployeeID = SessionManager.Instance.CurrentEmployeeID > 0
                                        ? SessionManager.Instance.CurrentEmployeeID : (int?)null;

            if (cmbCustomer.SelectedItem is CustomerComboItem ci)
                _currentOrder.CustomerID = ci.ID;

            if (_currentOrder.OrderType == "Dine-In" &&
                cmbTable.SelectedItem is TableComboItem ti && ti.ID > 0)
            {
                _currentOrder.TableID = ti.ID;
            }
            else
            {
                _currentOrder.TableID = null;
            }

            if (!EnsureCartProductsPersisted(out var productError))
            {
                MessageHelper.ShowError("Order items database mein save nahi ho sake: " + productError);
                return;
            }

            // Determine discount strategy
            IDiscountStrategy discStrat = cmbDiscount.SelectedIndex switch
            {
                1 => new PercentageDiscount(5),
                2 => new PercentageDiscount(10),
                3 => new PercentageDiscount(15),
                4 => new HappyHourDiscount(),
                5 => new LoyaltyDiscount(),
                _ => new NoDiscount()
            };

            var payment = PaymentFactory.Create(cmbPayment.Text);
            var (success, orderID, message) = ServiceContainer.Orders.PlaceOrder(_currentOrder, discStrat, _taxRate);

            if (!success)
            { MessageHelper.ShowError($"Order failed: {message}"); return; }

            var (paid2, msg2) = ServiceContainer.Orders.CompleteOrder(orderID, payment, paid);

            if (paid2)
            {
                // Print receipt
                var order = ServiceContainer.OrderRepo.GetWithDetails(orderID);
                ReceiptPrinter.PrintReceipt(order,
                    ServiceContainer.Settings.Get("CafeName", "SmartCafe Pro"),
                    ServiceContainer.Settings.Get("CafeAddress", ""),
                    ServiceContainer.Settings.Get("ReceiptFooter", "Thank you!"));

                MessageHelper.ShowSuccess($"✓ Order #{orderID} placed & paid!\nChange: {Formatter.Currency(Math.Max(0, paid - payableTotal))}");
                ResetOrder();
            }
            else
            {
                MessageHelper.ShowError($"Payment failed: {msg2}");
            }
        }

        private bool EnsureCartProductsPersisted(out string message)
        {
            message = "";
            try
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                EnsurePosSchema(conn);

                foreach (var item in _currentOrder.Items)
                {
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
DECLARE @CategoryID INT;
SELECT TOP 1 @CategoryID = CategoryID FROM Categories WHERE CategoryName = @CategoryName;
IF @CategoryID IS NULL
BEGIN
    INSERT INTO Categories(CategoryName, Description, DisplayOrder, IsActive)
    VALUES(@CategoryName, @CategoryName + N' menu items', 99, 1);
    SET @CategoryID = SCOPE_IDENTITY();
END

DECLARE @ProductID INT;
SELECT TOP 1 @ProductID = ProductID FROM Products WHERE ProductName = @ProductName;
IF @ProductID IS NULL
BEGIN
    INSERT INTO Products(CategoryID, ProductName, Description, Price, CostPrice, ImagePath, IsCombo, IsAvailable, PrepTimeMin, DisplayOrder, IsActive)
    VALUES(@CategoryID, @ProductName, @Description, @Price, @CostPrice, @ImagePath, 0, 1, 5, 99, 1);
    SET @ProductID = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Products
    SET CategoryID=@CategoryID, Price=@Price, IsActive=1, IsAvailable=1, UpdatedAt=GETDATE()
    WHERE ProductID=@ProductID;
END

SELECT @ProductID;",
                        conn) { CommandType = System.Data.CommandType.Text };

                    var categoryName = GuessCategoryForProduct(item.ProductName);
                    cmd.Parameters.AddWithValue("@CategoryName", categoryName);
                    cmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    cmd.Parameters.AddWithValue("@Description", item.ProductName);
                    cmd.Parameters.AddWithValue("@Price", item.UnitPrice);
                    cmd.Parameters.AddWithValue("@CostPrice", Math.Max(0, item.UnitPrice * 0.55m));
                    cmd.Parameters.AddWithValue("@ImagePath", "pos:" + item.ProductName.ToLower().Replace(" ", "-"));

                    var id = cmd.ExecuteScalar();
                    item.ProductID = Convert.ToInt32(id);
                }

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        private string GuessCategoryForProduct(string name)
        {
            var key = (name ?? "").ToLower();
            if (key.Contains("coffee") || key.Contains("cappuccino") || key.Contains("latte") || key.Contains("mocha")) return "Coffee";
            if (key.Contains("pizza")) return "Pizza";
            if (key.Contains("burger")) return "Burgers";
            if (key.Contains("ice cream")) return "Ice Cream";
            if (key.Contains("cake") || key.Contains("brownie")) return "Cakes";
            if (key.Contains("shake")) return "Shakes";
            if (key.Contains("pepsi") || key.Contains("lemon") || key.Contains("cold") || key.Contains("iced")) return "Cold Drinks";
            if (key.Contains("fries")) return "Fries";
            if (key.Contains("nuggets") || key.Contains("wings") || key.Contains("sandwich")) return "Snacks";
            return "Snacks";
        }

        private void ResetOrder()
        {
            _currentOrder = new Order();
            dgvCart.Rows.Clear();
            cmbCustomer.SelectedIndex = 0;
            cmbTable.SelectedIndex    = 0;
            txtNotes.Clear();
            cmbDiscount.SelectedIndex = 0;
            cmbPayment.SelectedIndex  = 0;
            UpdateTotalLabels();
        }

        // ── Loaders ─────────────────────────────────────────────
        private void LoadTables()
        {
            cmbTable.Items.Clear();
            cmbTable.Items.Add(new TableComboItem(0, "None"));
            try
            {
                var tables = ServiceContainer.TableRepo?.GetByStatus("Available")
                             ?? new List<CafeTable>();
                foreach (var t in tables)
                    cmbTable.Items.Add(new TableComboItem(t.TableID, t.TableNumber));
            }
            catch { }
            cmbTable.SelectedIndex = 0;
        }

        private async Task LoadCustomers()
        {
            var customers = await Task.Run(() =>
            {
                try { return ServiceContainer.CustomerRepo.GetActive(); }
                catch { return new List<Customer>(); }
            });

            if (IsDisposed) return;
            Invoke(() =>
            {
                cmbCustomer.Items.Clear();
                bool customerMode = _customerOnlyMode || string.Equals(SessionManager.Instance.CurrentRole, "Customer",
                    StringComparison.OrdinalIgnoreCase);

                if (customerMode)
                {
                    btnAddCustomer.Visible = false;
                    cmbCustomer.Enabled = false;

                    var customer = FindLoggedInCustomer(customers);
                    if (customer != null)
                    {
                        cmbCustomer.Items.Add(new CustomerComboItem(customer.CustomerID,
                            $"{customer.FullName} ({customer.MembershipLevel})"));
                    }
                    else
                    {
                        cmbCustomer.Items.Add(new CustomerComboItem(0,
                            SessionManager.Instance.CurrentUser?.Username ?? "Customer"));
                    }
                }
                else
                {
                    btnAddCustomer.Visible = true;
                    cmbCustomer.Enabled = true;
                    cmbCustomer.Items.Add(new CustomerComboItem(0, "Walk-in Customer"));
                    foreach (var c in customers)
                        cmbCustomer.Items.Add(new CustomerComboItem(c.CustomerID, $"{c.FullName} ({c.MembershipLevel})"));
                }

                cmbCustomer.SelectedIndex = 0;
            });
        }

        // ── UI Helpers ───────────────────────────────────────────
        private Customer FindLoggedInCustomer(List<Customer> customers)
        {
            int customerID = GetCurrentUserCustomerID();
            if (customerID > 0)
                return customers.FirstOrDefault(c => c.CustomerID == customerID);

            string username = SessionManager.Instance.CurrentUser?.Username ?? "";
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return customers.FirstOrDefault(c =>
                string.Equals(c.FirstName, username, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.FullName, username, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Email, username, StringComparison.OrdinalIgnoreCase));
        }

        private int GetCurrentUserCustomerID()
        {
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();

                using (var schemaCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Customers' AND COLUMN_NAME='UserID'",
                    conn))
                {
                    if (Convert.ToInt32(schemaCmd.ExecuteScalar()) == 0)
                        return 0;
                }

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT TOP 1 CustomerID FROM Customers WHERE UserID=@UserID AND IsActive=1 ORDER BY CustomerID DESC",
                    conn);
                cmd.Parameters.AddWithValue("@UserID", SessionManager.Instance.CurrentUserID);
                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private Button MakeActionBtn(string text, Color color, Point loc, Size size)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private ComboBox MakeCombo(string[] items, string label, Point loc, Size size, Panel parent)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(loc.X, loc.Y - 4),
                Size = new Size(size.Width, 18), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 8), BackColor = Color.Transparent });

            var cb = new ComboBox { Location = new Point(loc.X, loc.Y + 16), Size = size,
                BackColor = ThemeManager.DarkSurface, ForeColor = ThemeManager.ForeText,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cb.Items.AddRange(items);
            cb.SelectedIndex = 0;
            parent.Controls.Add(cb);
            return cb;
        }

        private Label MakeTotalRow(Panel parent, string lbl, ref int y, Color color)
        {
            parent.Controls.Add(new Label { Text = lbl, Location = new Point(0, y),
                Size = new Size(100, 22), ForeColor = ThemeManager.ForeSecond,
                Font = new Font("Segoe UI", 9), BackColor = Color.Transparent });
            var val = new Label { Location = new Point(105, y), Size = new Size(205, 22),
                ForeColor = color, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent, Text = "PKR 0" };
            parent.Controls.Add(val);
            y += 25;
            return val;
        }

        private void StyleGrid(DataGridView grid)
        {
            grid.DefaultCellStyle.BackColor          = ThemeManager.Card;
            grid.DefaultCellStyle.ForeColor          = ThemeManager.RowText;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 218, 204);
            grid.DefaultCellStyle.SelectionForeColor = ThemeManager.RowText;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Sidebar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersHeight    = 32;
            grid.RowTemplate.Height     = 36;
        }

        private GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Combo box item wrappers
        private class CustomerComboItem
        {
            public int    ID   { get; }
            public string Name { get; }
            public CustomerComboItem(int id, string name) { ID = id; Name = name; }
            public override string ToString() => Name;
        }
        private class TableComboItem
        {
            public int    ID     { get; }
            public string Number { get; }
            public TableComboItem(int id, string num) { ID = id; Number = num; }
            public override string ToString() => Number;
        }
    }
}
