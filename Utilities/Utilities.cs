// ============================================================
// SmartCafe Pro - Utilities & Helpers
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SmartCafePro.Models;

namespace SmartCafePro.Utilities
{
    // ==========================================================
    // Input Validation Helper
    // ==========================================================
    public static class Validator
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email,
                @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        }

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return Regex.IsMatch(phone.Replace("-","").Replace(" ",""), @"^\+?[0-9]{10,15}$");
        }

        public static bool IsValidCNIC(string cnic)
        {
            if (string.IsNullOrWhiteSpace(cnic)) return false;
            return Regex.IsMatch(cnic.Replace("-",""), @"^\d{13}$");
        }

        public static bool IsValidPassword(string pass, out string message)
        {
            if (string.IsNullOrWhiteSpace(pass))
            { message = "Password is required."; return false; }
            if (pass.Length < 6)
            { message = "Password must be at least 6 characters."; return false; }
            message = "";
            return true;
        }

        public static bool IsPositiveDecimal(string val, out decimal result)
            => decimal.TryParse(val, out result) && result >= 0;

        public static bool IsPositiveInt(string val, out int result)
            => int.TryParse(val, out result) && result > 0;

        public static string SanitizeInput(string input)
            => input?.Trim().Replace("'", "''") ?? "";
    }

    // ==========================================================
    // Receipt Printer Helper
    // ==========================================================
    public static class ReceiptPrinter
    {
        private static Order   _order;
        private static string  _cafeName;
        private static string  _cafeAddress;
        private static string  _footer;

        public static void PrintReceipt(Order order, string cafeName,
                                         string address, string footer)
        {
            _order       = order;
            _cafeName    = cafeName;
            _cafeAddress = address;
            _footer      = footer;

            var doc = new PrintDocument();
            doc.PrintPage += PrintPage;
            var receiptHeight = Math.Max(430, 290 + (order.Items?.Count ?? 0) * 24);
            doc.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 330, receiptHeight);

            var dlg = new PrintPreviewDialog
            {
                Document    = doc,
                WindowState = FormWindowState.Maximized,
                Text        = $"Receipt - {order.OrderNumber}"
            };
            dlg.ShowDialog();
        }

        private static void PrintPage(object sender, PrintPageEventArgs e)
        {
            var g   = e.Graphics;
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var x = 14f;
            var y = 14f;
            var w = e.PageBounds.Width - 28f;

            using var titleFont  = new Font("Segoe UI", 15, FontStyle.Bold);
            using var normalFont = new Font("Segoe UI", 8.8f);
            using var labelFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var itemFont   = new Font("Consolas", 8.6f);
            using var totalFont  = new Font("Segoe UI", 11, FontStyle.Bold);
            using var smallFont  = new Font("Segoe UI", 8);

            using var ink        = new SolidBrush(Color.FromArgb(24, 31, 45));
            using var muted      = new SolidBrush(Color.FromArgb(92, 101, 120));
            using var accent     = new SolidBrush(Color.FromArgb(99, 102, 241));
            using var softAccent = new SolidBrush(Color.FromArgb(239, 240, 255));
            using var borderPen  = new Pen(Color.FromArgb(210, 214, 245));
            using var darkPen    = new Pen(Color.FromArgb(99, 102, 241));
            using var whiteBrush = new SolidBrush(Color.White);

            using var center = new StringFormat { Alignment = StringAlignment.Center };
            using var right  = new StringFormat { Alignment = StringAlignment.Far };

            g.FillRectangle(Brushes.White, e.PageBounds);
            var headerRect = new RectangleF(x, y, w, 64);
            using (var headerBrush = new LinearGradientBrush(headerRect,
                       Color.FromArgb(99, 102, 241),
                       Color.FromArgb(139, 92, 246),
                       LinearGradientMode.Horizontal))
            {
                g.FillRectangle(headerBrush, headerRect);
            }
            using (var shine = new SolidBrush(Color.FromArgb(42, Color.White)))
            {
                g.FillEllipse(shine, x + w - 72, y - 26, 96, 76);
                g.FillEllipse(shine, x - 26, y + 34, 80, 54);
            }
            g.DrawRectangle(borderPen, x, y, w, 64);

            g.DrawString(_cafeName, titleFont, whiteBrush, new RectangleF(x, y + 8, w, 26), center);
            g.DrawString(_cafeAddress, smallFont, whiteBrush, new RectangleF(x + 8, y + 38, w - 16, 18), center);
            y += 76;

            DrawInfoRow(g, "Receipt", _order.OrderNumber, labelFont, normalFont, ink, x, ref y, w);
            DrawInfoRow(g, "Date", _order.OrderDate.ToString("dd MMM yyyy  hh:mm tt"), labelFont, normalFont, ink, x, ref y, w);
            DrawInfoRow(g, "Type", _order.OrderType, labelFont, normalFont, ink, x, ref y, w);
            if (!string.IsNullOrWhiteSpace(_order.TableNumber))
                DrawInfoRow(g, "Table", _order.TableNumber, labelFont, normalFont, ink, x, ref y, w);

            y += 4;
            DrawDivider(g, darkPen, x, y, w); y += 10;

            var qtyX = x + w - 92;
            var amountX = x + w;
            g.FillRectangle(softAccent, x, y - 3, w, 22);
            g.DrawString("Item", labelFont, muted, x, y);
            g.DrawString("Qty", labelFont, muted, new RectangleF(qtyX, y, 30, 16), right);
            g.DrawString("Amount", labelFont, muted, new RectangleF(x, y, w, 16), right);
            y += 17;
            DrawDivider(g, borderPen, x, y, w); y += 7;

            foreach (var item in _order.Items)
            {
                var name = item.ProductName ?? "Item";
                if (name.Length > 23) name = name[..23] + ".";

                g.DrawString(name, itemFont, ink, x, y);
                g.DrawString(item.Quantity.ToString("N0"), itemFont, ink, new RectangleF(qtyX, y, 30, 16), right);
                g.DrawString(item.LineTotal.ToString("N0"), itemFont, ink, new RectangleF(x, y, w, 16), right);
                y += 18;
            }

            y += 3;
            DrawDivider(g, borderPen, x, y, w); y += 10;

            DrawMoneyRow(g, "Subtotal", _order.SubTotal, normalFont, ink, x, ref y, w);
            if (_order.DiscountAmt > 0)
                DrawMoneyRow(g, "Discount", _order.DiscountAmt, normalFont, ink, x, ref y, w);
            DrawMoneyRow(g, "Tax", _order.TaxAmt, normalFont, ink, x, ref y, w);

            y += 4;
            var totalRect = new RectangleF(x, y, w, 38);
            using (var totalBrush = new LinearGradientBrush(totalRect,
                       Color.FromArgb(34, 197, 94),
                       Color.FromArgb(16, 185, 129),
                       LinearGradientMode.Horizontal))
            {
                g.FillRectangle(totalBrush, totalRect);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(20, 150, 95)), x, y, w, 38);
            g.DrawString("TOTAL", totalFont, whiteBrush, x + 8, y + 8);
            g.DrawString($"PKR {_order.TotalAmount:N0}", totalFont, whiteBrush,
                new RectangleF(x + 8, y + 7, w - 16, 18), right);
            y += 50;

            DrawDivider(g, darkPen, x, y, w); y += 12;
            g.DrawString(_footer, smallFont, muted, new RectangleF(x + 8, y, w - 16, 36), center);
        }

        private static void DrawInfoRow(Graphics g, string label, string value,
                                        Font labelFont, Font valueFont, Brush brush,
                                        float x, ref float y, float width)
        {
            using var right = new StringFormat { Alignment = StringAlignment.Far };

            g.DrawString($"{label}:", labelFont, brush, x, y);
            g.DrawString(value, valueFont, brush, new RectangleF(x + 70, y, width - 70, 16), right);
            y += 17;
        }

        private static void DrawMoneyRow(Graphics g, string label, decimal amount,
                                         Font font, Brush brush, float x,
                                         ref float y, float width)
        {
            using var right = new StringFormat { Alignment = StringAlignment.Far };

            g.DrawString(label, font, brush, x + 8, y);
            g.DrawString($"PKR {amount:N0}", font, brush,
                new RectangleF(x + 8, y, width - 16, 16), right);
            y += 17;
        }

        private static void DrawDivider(Graphics g, Pen pen, float x, float y, float width)
        {
            g.DrawLine(pen, x, y, x + width, y);
        }
    }

    // ==========================================================
    // Theme / Color Helper
    // ==========================================================
    public static class ThemeManager
    {
        public static bool IsDarkMode { get; private set; } = true;

        // Soft brown + white theme
        public static Color DarkBackground    => Color.FromArgb(248, 244, 238);
        public static Color DarkSurface       => Color.FromArgb(255, 255, 255);
        public static Color DarkCard          => Color.FromArgb(255, 255, 255);
        public static Color DarkSidebar       => Color.FromArgb(76,  48,  35);
        public static Color AccentPrimary     => Color.FromArgb(111, 78,  55);
        public static Color AccentSecondary   => Color.FromArgb(163, 119, 77);
        public static Color AccentGreen       => Color.FromArgb(34,  197, 94);
        public static Color AccentRed         => Color.FromArgb(239, 68,  68);
        public static Color AccentOrange      => Color.FromArgb(249, 115, 22);
        public static Color AccentBlue        => Color.FromArgb(59,  130, 246);
        public static Color TextPrimary       => IsDarkMode ? Color.FromArgb(18,  18,  18) : Color.FromArgb(18,  18,  18);
        public static Color TextSecondary     => IsDarkMode ? Color.FromArgb(86,  64,  50) : Color.FromArgb(76,  64,  56);
        public static Color TextMuted         => IsDarkMode ? Color.FromArgb(124, 104, 88) : Color.FromArgb(116, 104, 96);
        public static Color RowText           => IsDarkMode ? Color.FromArgb(128, 92,  66) : Color.FromArgb(128, 92,  66);
        public static Color BorderColor       => IsDarkMode ? Color.FromArgb(226, 216, 204) : Color.FromArgb(226, 216, 204);

        // Light Theme Colors
        public static Color LightBackground   => Color.FromArgb(252, 249, 245);
        public static Color LightSurface      => Color.White;
        public static Color LightCard         => Color.White;
        public static Color LightSidebar      => Color.FromArgb(62,  39,  28);
        public static Color LightTextPrimary  => Color.FromArgb(18,  18,  18);
        public static Color LightTextSecond   => Color.FromArgb(76,  64,  56);

        public static Color Background   => IsDarkMode ? DarkBackground   : LightBackground;
        public static Color Surface      => IsDarkMode ? DarkSurface      : LightSurface;
        public static Color Card         => IsDarkMode ? DarkCard         : LightCard;
        public static Color Sidebar      => IsDarkMode ? DarkSidebar      : LightSidebar;
        public static Color ForeText     => IsDarkMode ? TextPrimary      : LightTextPrimary;
        public static Color ForeSecond   => IsDarkMode ? TextSecondary    : LightTextSecond;

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SetTheme(bool dark)
        {
            IsDarkMode = dark;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static event EventHandler ThemeChanged;
    }

    // ==========================================================
    // Format Helper
    // ==========================================================
    public static class Formatter
    {
        public static string Currency(decimal amount, string symbol = "PKR")
            => $"{symbol} {amount:N0}";

        public static string ShortDate(DateTime d)     => d.ToString("dd MMM yyyy");
        public static string LongDate(DateTime d)      => d.ToString("dd MMMM yyyy hh:mm tt");
        public static string TimeOnly(DateTime d)      => d.ToString("hh:mm tt");
        public static string DateTimeShort(DateTime d) => d.ToString("dd-MMM HH:mm");

        public static string TruncateText(string text, int maxLen)
            => text?.Length > maxLen ? text[..maxLen] + "..." : text;

        public static Color StatusColor(string status) => status switch
        {
            "Completed"  => ThemeManager.AccentGreen,
            "Pending"    => ThemeManager.AccentOrange,
            "Preparing"  => ThemeManager.AccentBlue,
            "Cancelled"  => ThemeManager.AccentRed,
            "Ready"      => Color.FromArgb(20, 184, 166),
            _            => ThemeManager.TextSecondary
        };
    }

    // ==========================================================
    // MessageHelper - stylized dialogs
    // ==========================================================
    public static class MessageHelper
    {
        public static void ShowSuccess(string message, string title = "Success")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void ShowError(string message, string title = "Error")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static void ShowWarning(string message, string title = "Warning")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static bool Confirm(string message, string title = "Confirm")
            => MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
               == DialogResult.Yes;
    }
}

namespace SmartCafePro.Helpers
{
    // ==========================================================
    // Database Backup Helper
    // ==========================================================
    public static class BackupHelper
    {
        public static bool BackupDatabase(string backupPath, string dbName = "SmartCafeProDB")
        {
            try
            {
                var fileName = Path.Combine(backupPath,
                    $"SmartCafe_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                Directory.CreateDirectory(backupPath);

                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    $"BACKUP DATABASE [{dbName}] TO DISK = N'{fileName}' WITH NOFORMAT, NOINIT, COMPRESSION",
                    conn) { CommandTimeout = 120 };
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }
    }

    // ==========================================================
    // QR Code Generator (using basic encoding)
    // ==========================================================
    public static class QRHelper
    {
        /// <summary>
        /// Generates a simple QR bitmap using ZXing.Net (add NuGet: ZXing.Net)
        /// Falls back to placeholder if library not available.
        /// </summary>
        public static Bitmap GenerateQR(string content, int size = 200)
        {
            try
            {
                // ZXing.Net implementation:
                // var writer = new ZXing.BarcodeWriter {
                //     Format = ZXing.BarcodeFormat.QR_CODE,
                //     Options = new ZXing.QrCode.QrCodeEncodingOptions { Width=size, Height=size }
                // };
                // return writer.Write(content);

                // Placeholder bitmap
                var bmp = new Bitmap(size, size);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.White);
                g.DrawString("QR\n" + content[..Math.Min(content.Length,10)],
                    new Font("Arial", 8), Brushes.Black, 5, size/2 - 15);
                return bmp;
            }
            catch { return new Bitmap(size, size); }
        }
    }
}
