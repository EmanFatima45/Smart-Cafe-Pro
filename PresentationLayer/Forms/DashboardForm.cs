// ============================================================
// SmartCafe Pro - DashboardForm.cs
// Real-time dashboard with KPI cards and charts
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartCafePro.BusinessLayer;
using SmartCafePro.Models;
using SmartCafePro.Utilities;

namespace SmartCafePro.PresentationLayer.Forms
{
    public class DashboardForm : UserControl
    {
        private Panel           pnlKPI, pnlCharts, pnlRecent;
        private Panel           _scroll;
        private FlowLayoutPanel flowKPI;
        private DataGridView    _recentGrid;
        private Timer           _refreshTimer;
        private DashboardStats  _stats;

        // Chart panels
        private Panel pnlBarChart, pnlDonutChart, pnlTopProducts;

        public DashboardForm()
        {
            BackColor     = ThemeManager.Background;
            Dock          = DockStyle.Fill;
            DoubleBuffered = true;
            InitLayout();
            _ = LoadDataAsync();
            StartAutoRefresh();
        }

        private void InitLayout()
        {
            // Scrollable main container
            _scroll = new Panel
            {
                Dock          = DockStyle.Fill,
                AutoScroll    = true,
                BackColor     = ThemeManager.Background
            };

            // ── Header bar ───────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = ThemeManager.Sidebar
            };

            var lblWelcome = new Label
            {
                Text      = $"Good {GetTimeOfDay()}, {SessionManager.Instance.CurrentUser?.Username}! 👋",
                Location  = new Point(20, 10),
                Size      = new Size(500, 28),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            var lblDate = new Label
            {
                Text      = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                Location  = new Point(20, 38),
                Size      = new Size(400, 18),
                ForeColor = Color.FromArgb(232, 218, 204),
                Font      = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };

            var btnRefresh = new Button
            {
                Text      = "⟳  Refresh",
                Location  = new Point(900, 15),
                Size      = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 238, 229),
                ForeColor = ThemeManager.AccentPrimary,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(60, ThemeManager.AccentPrimary);
            btnRefresh.Click += async (s, e) => await LoadDataAsync();
            pnlHeader.Controls.AddRange(new Control[] { lblWelcome, lblDate, btnRefresh });

            // ── KPI Row ──────────────────────────────────────────
            flowKPI = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 145,
                BackColor     = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                Padding       = new Padding(16, 8, 16, 8)
            };

            // ── Charts row ───────────────────────────────────────
            pnlCharts = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 300,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 8, 16, 8)
            };
            BuildChartsLayout();

            // ── Recent orders section ────────────────────────────
            pnlRecent = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 320,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 8, 16, 8)
            };
            BuildRecentOrdersPanel();

            // Assemble scroll content
            var content = new Panel
            {
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent
            };
            content.Controls.Add(pnlRecent);
            content.Controls.Add(pnlCharts);
            content.Controls.Add(flowKPI);
            content.Controls.Add(pnlHeader);

            // Stack them vertically
            pnlHeader.Dock   = DockStyle.Top;
            flowKPI.Dock     = DockStyle.Top;
            pnlCharts.Dock   = DockStyle.Top;
            pnlRecent.Dock   = DockStyle.Top;

            _scroll.Controls.Add(pnlRecent);
            _scroll.Controls.Add(pnlCharts);
            _scroll.Controls.Add(flowKPI);
            _scroll.Controls.Add(pnlHeader);

            Controls.Add(_scroll);
            _scroll.Resize += (s, e) => LayoutDashboard();
            LayoutDashboard();
        }

        private async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                try { _stats = ServiceContainer.Dashboard.GetDashboardStats(); }
                catch { _stats = new DashboardStats(); }
            });

            if (IsDisposed) return;
            Invoke(RenderDashboard);
        }

        private void RenderDashboard()
        {
            if (_stats == null) return;

            flowKPI.Controls.Clear();

            // KPI Cards
            flowKPI.Controls.Add(MakeKPICard("💰 Today's Revenue",
                Formatter.Currency(_stats.TodayRevenue),
                $"Month: {Formatter.Currency(_stats.MonthRevenue)}",
                ThemeManager.AccentGreen));

            flowKPI.Controls.Add(MakeKPICard("🛒 Today's Orders",
                _stats.TodayOrders.ToString(),
                $"Avg: {Formatter.Currency(_stats.AvgOrderValue)}",
                ThemeManager.AccentBlue));

            flowKPI.Controls.Add(MakeKPICard("⏳ Pending Orders",
                _stats.PendingOrders.ToString(),
                "Requires attention",
                _stats.PendingOrders > 5 ? ThemeManager.AccentRed : ThemeManager.AccentOrange));

            flowKPI.Controls.Add(MakeKPICard("🪑 Tables Occupied",
                $"{_stats.TablesOccupied}/{_stats.TotalTables}",
                $"{(_stats.TotalTables > 0 ? _stats.TablesOccupied * 100 / _stats.TotalTables : 0)}% occupied",
                ThemeManager.AccentPrimary));

            flowKPI.Controls.Add(MakeKPICard("📦 Low Stock Items",
                _stats.LowStockItems.ToString(),
                "Need restocking",
                _stats.LowStockItems > 0 ? ThemeManager.AccentRed : ThemeManager.AccentGreen));

            flowKPI.Controls.Add(MakeKPICard("📅 Reservations",
                _stats.TodayReservations.ToString(),
                "Booked today",
                Color.FromArgb(20, 184, 166)));

            // Redraw charts
            pnlBarChart?.Invalidate();
            pnlDonutChart?.Invalidate();
            RenderTopProducts();
        }

        private Panel MakeKPICard(string title, string value, string sub, Color accent)
        {
            var card = new Panel
            {
                Size      = new Size(185, 120),
                Margin    = new Padding(0, 0, 12, 0),
                BackColor = ThemeManager.Card,
                Cursor    = Cursors.Hand
            };
            card.Paint += (s, e) => PaintCard(e.Graphics, card.ClientRectangle, accent);

            var lblTitle = new Label
            {
                Text      = title,
                Location  = new Point(16, 16),
                Size      = new Size(155, 20),
                ForeColor = ThemeManager.ForeSecond,
                Font      = new Font("Segoe UI", 8),
                BackColor = Color.Transparent
            };
            var lblValue = new Label
            {
                Text      = value,
                Location  = new Point(14, 38),
                Size      = new Size(160, 38),
                ForeColor = ThemeManager.ForeText,
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            var lblSub = new Label
            {
                Text      = sub,
                Location  = new Point(16, 80),
                Size      = new Size(155, 18),
                ForeColor = accent,
                Font      = new Font("Segoe UI", 8),
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblTitle, lblValue, lblSub });

            // Hover effect
            card.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(55, 55, 70); };
            card.MouseLeave += (s, e) => { card.BackColor = ThemeManager.Card; };

            return card;
        }

        private void LayoutDashboard()
        {
            if (_scroll == null || pnlCharts == null || pnlRecent == null || flowKPI == null)
                return;

            int width = Math.Max(760, _scroll.ClientSize.Width - 24);
            bool compact = width < 1120;

            flowKPI.Height = compact ? 275 : 145;

            if (compact)
            {
                pnlCharts.Height = 590;
                pnlBarChart.Location = new Point(16, 8);
                pnlBarChart.Size = new Size(Math.Max(520, width - 48), 270);
                pnlDonutChart.Location = new Point(16, 300);
                pnlDonutChart.Size = new Size(Math.Max(250, (width - 64) / 2), 270);
                pnlTopProducts.Location = new Point(pnlDonutChart.Right + 16, 300);
                pnlTopProducts.Size = new Size(Math.Max(250, (width - 64) / 2), 270);
            }
            else
            {
                pnlCharts.Height = 300;
                int available = width - 64;
                int sideWidth = 220;
                pnlBarChart.Location = new Point(16, 8);
                pnlBarChart.Size = new Size(Math.Max(520, available - sideWidth * 2 - 32), 280);
                pnlDonutChart.Location = new Point(pnlBarChart.Right + 16, 8);
                pnlDonutChart.Size = new Size(sideWidth, 280);
                pnlTopProducts.Location = new Point(pnlDonutChart.Right + 16, 8);
                pnlTopProducts.Size = new Size(sideWidth, 280);
            }

            if (_recentGrid != null)
                _recentGrid.Size = new Size(Math.Max(500, pnlRecent.ClientSize.Width - 32), 265);

            pnlBarChart.Invalidate();
            pnlDonutChart.Invalidate();
            RenderTopProducts();
        }

        private void PaintCard(Graphics g, Rectangle rect, Color accent)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundRect(rect, 12);
            using var bg    = new SolidBrush(ThemeManager.Card);
            g.FillPath(bg, path);

            // Left accent bar
            using var accBrush = new SolidBrush(accent);
            var accRect = new Rectangle(0, 0, 4, rect.Height);
            using var accPath = RoundRect(accRect, 2);
            g.FillPath(accBrush, accPath);

            // Subtle border
            using var pen = new Pen(Color.FromArgb(35, 255, 255, 255));
            g.DrawPath(pen, path);
        }

        private void BuildChartsLayout()
        {
            // Weekly revenue bar chart
            pnlBarChart = new Panel
            {
                Location  = new Point(16, 8),
                Size      = new Size(560, 280),
                BackColor = ThemeManager.Card
            };
            pnlBarChart.Paint += PaintBarChart;
            AddCardTitle(pnlBarChart, "📊 Weekly Revenue");
            pnlCharts.Controls.Add(pnlBarChart);

            // Payment breakdown donut
            pnlDonutChart = new Panel
            {
                Location  = new Point(596, 8),
                Size      = new Size(200, 280),
                BackColor = ThemeManager.Card
            };
            pnlDonutChart.Paint += PaintDonutChart;
            AddCardTitle(pnlDonutChart, "💳 Payments");
            pnlCharts.Controls.Add(pnlDonutChart);

            // Top products
            pnlTopProducts = new Panel
            {
                Location  = new Point(816, 8),
                Size      = new Size(200, 280),
                BackColor = ThemeManager.Card
            };
            AddCardTitle(pnlTopProducts, "🏆 Top Products");
            pnlCharts.Controls.Add(pnlTopProducts);
        }

        private void PaintBarChart(object sender, PaintEventArgs e)
        {
            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = (sender as Panel).ClientRectangle;

            // Background
            using var bgBrush = new SolidBrush(ThemeManager.Card);
            using var bgPath  = RoundRect(rect, 12);
            g.FillPath(bgBrush, bgPath);

            if (_stats?.WeeklyRevenue == null || _stats.WeeklyRevenue.Count == 0)
            {
                DrawNoData(g, rect);
                return;
            }

            var data   = _stats.WeeklyRevenue;
            var maxVal = 0m;
            foreach (var (_, v) in data) if (v > maxVal) maxVal = v;
            if (maxVal == 0) maxVal = 1;

            int    chartX  = 60;
            int    chartY  = 40;
            int    chartW  = rect.Width  - 80;
            int    chartH  = rect.Height - 80;
            int    barW    = data.Count > 0 ? (chartW / data.Count) - 8 : 30;
            var    colors  = new[] {
                Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246),
                ThemeManager.AccentBlue, ThemeManager.AccentGreen,
                ThemeManager.AccentOrange, Color.FromArgb(20,184,166), ThemeManager.AccentRed
            };

            // Grid lines
            using var gridPen = new Pen(Color.FromArgb(55, 90, 76, 66));
            for (int i = 0; i <= 4; i++)
            {
                int lineY = chartY + chartH - (chartH * i / 4);
                g.DrawLine(gridPen, chartX, lineY, chartX + chartW, lineY);
                decimal val = maxVal * i / 4;
                g.DrawString(Formatter.Currency(val, ""), new Font("Segoe UI", 7),
                    new SolidBrush(ThemeManager.ForeSecond), 5, lineY - 8);
            }

            // Bars
            for (int i = 0; i < data.Count; i++)
            {
                var (label, value) = data[i];
                int barH  = (int)(chartH * value / maxVal);
                int barX  = chartX + i * (barW + 8);
                int barY  = chartY + chartH - barH;
                var color = colors[i % colors.Length];

                if (barH > 0 && barW > 0)
                {
                    using var barGrad = new LinearGradientBrush(
                        new Rectangle(barX, barY, barW, barH),
                        color, Color.FromArgb(120, color), LinearGradientMode.Vertical);
                    using var barPath = RoundRect(new Rectangle(barX, barY, barW, barH), 4);
                    g.FillPath(barGrad, barPath);
                }
                else
                {
                    using var zeroPen = new Pen(Color.FromArgb(60, color), 2);
                    g.DrawLine(zeroPen, barX, chartY + chartH, barX + Math.Max(4, barW), chartY + chartH);
                }

                // Label
                g.DrawString(label[..Math.Min(3, label.Length)], new Font("Segoe UI", 7),
                    new SolidBrush(ThemeManager.ForeSecond), barX + barW / 2 - 10, chartY + chartH + 4);

                // Value on bar
                if (barH > 20)
                    g.DrawString(Formatter.Currency(value, ""), new Font("Segoe UI", 7),
                        new SolidBrush(Color.White), barX + 2, barY + 2);
            }
        }

        private void PaintDonutChart(object sender, PaintEventArgs e)
        {
            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = (sender as Panel).ClientRectangle;
            using var bgBrush = new SolidBrush(ThemeManager.Card);
            using var bgPath  = RoundRect(rect, 12);
            g.FillPath(bgBrush, bgPath);

            if (_stats?.PaymentBreakdown == null || _stats.PaymentBreakdown.Count == 0)
            { DrawNoData(g, rect); return; }

            var    data     = _stats.PaymentBreakdown;
            decimal total   = 0;
            foreach (var (_, v) in data) total += v;
            if (total == 0) { DrawNoData(g, rect); return; }

            var colors = new[] { ThemeManager.AccentGreen, ThemeManager.AccentBlue,
                                  Color.FromArgb(99, 102, 241), ThemeManager.AccentOrange };

            int cx = rect.Width / 2, cy = rect.Height / 2 - 10;
            int r  = 60, innerR = 38;
            float startAngle = -90f;

            for (int i = 0; i < data.Count; i++)
            {
                var (method, amt) = data[i];
                float sweep = (float)(amt / total * 360m);
                using var brush = new SolidBrush(colors[i % colors.Length]);
                g.FillPie(brush, cx - r, cy - r, r * 2, r * 2, startAngle, sweep);
                startAngle += sweep;
            }

            // Inner circle (donut hole)
            using var holeBrush = new SolidBrush(ThemeManager.Card);
            g.FillEllipse(holeBrush, cx - innerR, cy - innerR, innerR * 2, innerR * 2);

            // Center text
            var sf = new StringFormat { Alignment = StringAlignment.Center,
                                        LineAlignment = StringAlignment.Center };
            g.DrawString(Formatter.Currency(total, ""),
                new Font("Segoe UI", 8, FontStyle.Bold),
                new SolidBrush(ThemeManager.ForeText),
                new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2), sf);

            // Legend
            int ly = cy + r + 15;
            for (int i = 0; i < data.Count; i++)
            {
                var (method, _) = data[i];
                using var lb = new SolidBrush(colors[i % colors.Length]);
                g.FillRectangle(lb, 16, ly + i * 18, 10, 10);
                g.DrawString(method, new Font("Segoe UI", 7),
                    new SolidBrush(ThemeManager.ForeSecond), 30, ly + i * 18 - 1);
            }
        }

        private void RenderTopProducts()
        {
            if (pnlTopProducts == null || _stats?.TopProducts == null) return;

            // Remove old product labels (keep title)
            var toRemove = new List<Control>();
            foreach (Control c in pnlTopProducts.Controls)
                if (c.Tag?.ToString() == "product") toRemove.Add(c);
            foreach (var c in toRemove) pnlTopProducts.Controls.Remove(c);

            int y = 40;
            var maxSold = _stats.TopProducts.Count > 0 ? _stats.TopProducts[0].Sold : 1;

            for (int i = 0; i < _stats.TopProducts.Count && i < 5; i++)
            {
                var (name, sold) = _stats.TopProducts[i];
                int barW = maxSold > 0 ? (int)(140f * sold / maxSold) : 0;

                var lbl = new Label { Text = $"#{i+1} {Formatter.TruncateText(name,14)}",
                    Location = new Point(12, y), Size = new Size(175, 16),
                    ForeColor = ThemeManager.ForeText, Font = new Font("Segoe UI", 8),
                    BackColor = Color.Transparent, Tag = "product" };

                var bar = new Panel { Location = new Point(12, y + 17),
                    Size = new Size(barW, 6), BackColor = Color.FromArgb(99, 102, 241),
                    Tag = "product" };

                var lblCount = new Label { Text = $"{sold} sold",
                    Location = new Point(12 + barW + 4, y + 14), Size = new Size(60, 14),
                    ForeColor = ThemeManager.ForeSecond, Font = new Font("Segoe UI", 7),
                    BackColor = Color.Transparent, Tag = "product" };

                pnlTopProducts.Controls.AddRange(new Control[] { lbl, bar, lblCount });
                y += 46;
            }
        }

        private void BuildRecentOrdersPanel()
        {
            AddCardTitle(pnlRecent, "🕐 Recent Orders");

            _recentGrid = new DataGridView
            {
                Location          = new Point(16, 38),
                Size              = new Size(Math.Max(500, pnlRecent.Width - 32), 265),
                BackgroundColor   = ThemeManager.Card,
                GridColor         = ThemeManager.BorderColor,
                BorderStyle       = BorderStyle.None,
                RowHeadersVisible = false,
                ReadOnly          = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font              = new Font("Segoe UI", 9),
                Anchor            = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Style
            _recentGrid.DefaultCellStyle.BackColor         = ThemeManager.Card;
            _recentGrid.DefaultCellStyle.ForeColor         = ThemeManager.RowText;
            _recentGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 218, 204);
            _recentGrid.DefaultCellStyle.SelectionForeColor = ThemeManager.RowText;
            _recentGrid.EnableHeadersVisualStyles = false;
            _recentGrid.ColumnHeadersDefaultCellStyle.BackColor  = ThemeManager.Sidebar;
            _recentGrid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            _recentGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.Sidebar;
            _recentGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            _recentGrid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9, FontStyle.Bold);
            _recentGrid.ColumnHeadersHeight                       = 36;
            _recentGrid.RowTemplate.Height                        = 38;

            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderNum", HeaderText = "Order #", Width = 120 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer",  HeaderText = "Customer" });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",      HeaderText = "Type",   Width = 80 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount",    HeaderText = "Amount", Width = 100 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",    HeaderText = "Status", Width = 100 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time",      HeaderText = "Time",   Width = 80 });

            _ = LoadRecentOrders(_recentGrid);
            pnlRecent.Controls.Add(_recentGrid);
            pnlRecent.Resize += (s, e) => LayoutDashboard();
        }

        private async Task LoadRecentOrders(DataGridView grid)
        {
            var orders = await Task.Run(() =>
            {
                try { return ServiceContainer.OrderRepo.GetByDate(DateTime.Today); }
                catch { return new System.Collections.Generic.List<Models.Order>(); }
            });

            if (IsDisposed) return;
            Invoke(() =>
            {
                grid.Rows.Clear();
                foreach (var o in orders)
                {
                    int idx = grid.Rows.Add(
                        o.OrderNumber,
                        o.CustomerName ?? "Walk-in",
                        o.OrderType,
                        Formatter.Currency(o.TotalAmount),
                        o.Status,
                        o.OrderDate.ToString("HH:mm"));

                    // Color-code status
                    grid.Rows[idx].Cells["Status"].Style.ForeColor =
                        Formatter.StatusColor(o.Status);
                }
            });
        }

        // ── Helpers ─────────────────────────────────────────────
        private void AddCardTitle(Panel panel, string title)
        {
            var lbl = new Label
            {
                Text      = title,
                Location  = new Point(16, 12),
                Size      = new Size(panel.Width - 20, 22),
                ForeColor = ThemeManager.ForeText,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lbl);
        }

        private void DrawNoData(Graphics g, Rectangle rect)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center,
                                        LineAlignment = StringAlignment.Center };
            g.DrawString("No data available", new Font("Segoe UI", 10),
                new SolidBrush(ThemeManager.TextMuted),
                new RectangleF(0, 0, rect.Width, rect.Height), sf);
        }

        private GraphicsPath RoundRect(Rectangle bounds, int radius)
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

        private string GetTimeOfDay()
        {
            int h = DateTime.Now.Hour;
            return h < 12 ? "Morning" : h < 17 ? "Afternoon" : "Evening";
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new Timer { Interval = 60000 }; // every minute
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
            _refreshTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
