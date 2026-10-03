// ============================================================
// SmartCafe Pro - ReportExportService.cs
// PDF invoice & Excel report generation
// NuGet: itext7, EPPlus
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartCafePro.BusinessLayer;
using SmartCafePro.Models;
using SmartCafePro.Utilities;

namespace SmartCafePro.Services
{
    // ==========================================================
    // PDF Invoice Generator (iText7)
    // ==========================================================
    public static class PdfInvoiceService
    {
        /// <summary>
        /// Generates a professional PDF invoice for an order.
        /// Returns the file path of the saved PDF.
        /// </summary>
        public static string GenerateInvoice(Order order, string outputFolder = null)
        {
            outputFolder ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SmartCafePro", "Invoices");
            Directory.CreateDirectory(outputFolder);

            var filePath = Path.Combine(outputFolder,
                $"Invoice_{order.OrderNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            try
            {
                // iText7 PDF generation
                using var writer = new iText.Kernel.Pdf.PdfWriter(filePath);
                using var pdf    = new iText.Kernel.Pdf.PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf,
                    iText.Kernel.Geom.PageSize.A5);
                doc.SetMargins(30, 30, 30, 30);

                var cafeName    = ServiceContainer.Settings.Get("CafeName",    "SmartCafe Pro");
                var cafeAddress = ServiceContainer.Settings.Get("CafeAddress", "");
                var cafePhone   = ServiceContainer.Settings.Get("CafePhone",   "");
                var footer      = ServiceContainer.Settings.Get("ReceiptFooter","Thank you!");
                var currency    = ServiceContainer.Settings.Get("CurrencySymbol","PKR");

                // Fonts
                var titleFont   = iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD;
                var normalFont  = iText.IO.Font.Constants.StandardFonts.HELVETICA;
                var fTitle  = iText.Kernel.Font.PdfFontFactory.CreateFont(titleFont);
                var fNormal = iText.Kernel.Font.PdfFontFactory.CreateFont(normalFont);

                var accentColor = new iText.Kernel.Colors.DeviceRgb(99, 102, 241);
                var darkColor   = new iText.Kernel.Colors.DeviceRgb(30,  30,  30);
                var grayColor   = new iText.Kernel.Colors.DeviceRgb(120, 120, 130);

                // ── Header ──────────────────────────────────────
                var headerTable = new iText.Layout.Element.Table(2)
                    .UseAllAvailableWidth();

                // Left: cafe name
                headerTable.AddCell(
                    new iText.Layout.Element.Cell()
                        .Add(new iText.Layout.Element.Paragraph(cafeName)
                            .SetFont(fTitle).SetFontSize(18)
                            .SetFontColor(accentColor))
                        .Add(new iText.Layout.Element.Paragraph(cafeAddress)
                            .SetFont(fNormal).SetFontSize(9)
                            .SetFontColor(grayColor))
                        .Add(new iText.Layout.Element.Paragraph(cafePhone)
                            .SetFont(fNormal).SetFontSize(9)
                            .SetFontColor(grayColor))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                // Right: invoice info
                headerTable.AddCell(
                    new iText.Layout.Element.Cell()
                        .Add(new iText.Layout.Element.Paragraph("INVOICE")
                            .SetFont(fTitle).SetFontSize(20)
                            .SetFontColor(darkColor)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT))
                        .Add(new iText.Layout.Element.Paragraph($"# {order.OrderNumber}")
                            .SetFont(fNormal).SetFontSize(10)
                            .SetFontColor(grayColor)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT))
                        .Add(new iText.Layout.Element.Paragraph($"Date: {order.OrderDate:dd MMM yyyy}")
                            .SetFont(fNormal).SetFontSize(9)
                            .SetFontColor(grayColor)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                doc.Add(headerTable);

                // Divider
                doc.Add(new iText.Layout.Element.LineSeparator(
                    new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f)));
                doc.Add(new iText.Layout.Element.Paragraph(" "));

                // ── Order Info ───────────────────────────────────
                var infoTable = new iText.Layout.Element.Table(2).UseAllAvailableWidth();
                AddInfoRow(infoTable, fNormal, "Customer:",
                    order.CustomerName ?? "Walk-in Customer");
                AddInfoRow(infoTable, fNormal, "Table:",
                    order.TableNumber ?? "—");
                AddInfoRow(infoTable, fNormal, "Order Type:", order.OrderType);
                if (!string.IsNullOrEmpty(order.Notes))
                    AddInfoRow(infoTable, fNormal, "Notes:", order.Notes);
                doc.Add(infoTable);
                doc.Add(new iText.Layout.Element.Paragraph(" "));

                // ── Items Table ──────────────────────────────────
                var itemTable = new iText.Layout.Element.Table(
                    new float[] { 3, 1, 1.5f, 1, 1.5f })
                    .UseAllAvailableWidth();

                // Header row
                var headerBg = new iText.Kernel.Colors.DeviceRgb(40, 40, 55);
                foreach (var h in new[] { "Item", "Qty", "Unit Price", "Disc%", "Amount" })
                {
                    itemTable.AddHeaderCell(
                        new iText.Layout.Element.Cell()
                            .Add(new iText.Layout.Element.Paragraph(h)
                                .SetFont(fTitle).SetFontSize(9).SetFontColor(
                                    iText.Kernel.Colors.ColorConstants.WHITE))
                            .SetBackgroundColor(headerBg)
                            .SetPadding(6));
                }

                bool altRow = false;
                var altBg   = new iText.Kernel.Colors.DeviceRgb(248, 248, 255);

                foreach (var item in order.Items)
                {
                    var rowBg = altRow ? altBg : iText.Kernel.Colors.ColorConstants.WHITE;
                    itemTable.AddCell(ItemCell(item.ProductName,   fNormal, 9, rowBg));
                    itemTable.AddCell(ItemCell(item.Quantity.ToString(), fNormal, 9, rowBg,
                        iText.Layout.Properties.TextAlignment.CENTER));
                    itemTable.AddCell(ItemCell($"{currency} {item.UnitPrice:N0}", fNormal, 9, rowBg,
                        iText.Layout.Properties.TextAlignment.RIGHT));
                    itemTable.AddCell(ItemCell($"{item.Discount}%", fNormal, 9, rowBg,
                        iText.Layout.Properties.TextAlignment.CENTER));
                    itemTable.AddCell(ItemCell($"{currency} {item.LineTotal:N0}", fTitle, 9, rowBg,
                        iText.Layout.Properties.TextAlignment.RIGHT));
                    altRow = !altRow;
                }
                doc.Add(itemTable);
                doc.Add(new iText.Layout.Element.Paragraph(" "));

                // ── Totals ───────────────────────────────────────
                var totTable = new iText.Layout.Element.Table(new float[] { 3, 1.5f })
                    .UseAllAvailableWidth().SetHorizontalAlignment(
                        iText.Layout.Properties.HorizontalAlignment.RIGHT)
                    .SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(50));

                AddTotalRow(totTable, fNormal, "Subtotal:",
                    $"{currency} {order.SubTotal:N0}", grayColor);
                if (order.DiscountAmt > 0)
                    AddTotalRow(totTable, fNormal, "Discount:",
                        $"- {currency} {order.DiscountAmt:N0}",
                        new iText.Kernel.Colors.DeviceRgb(34, 197, 94));
                AddTotalRow(totTable, fNormal, "Tax:",
                    $"{currency} {order.TaxAmt:N0}", grayColor);

                // Grand total row
                totTable.AddCell(
                    new iText.Layout.Element.Cell()
                        .Add(new iText.Layout.Element.Paragraph("TOTAL:")
                            .SetFont(fTitle).SetFontSize(12)
                            .SetFontColor(accentColor))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPaddingTop(6));
                totTable.AddCell(
                    new iText.Layout.Element.Cell()
                        .Add(new iText.Layout.Element.Paragraph($"{currency} {order.TotalAmount:N0}")
                            .SetFont(fTitle).SetFontSize(13)
                            .SetFontColor(accentColor)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPaddingTop(6));

                doc.Add(totTable);
                doc.Add(new iText.Layout.Element.Paragraph(" "));
                doc.Add(new iText.Layout.Element.LineSeparator(
                    new iText.Kernel.Pdf.Canvas.Draw.DashedLine()));

                // ── Footer ───────────────────────────────────────
                doc.Add(new iText.Layout.Element.Paragraph(footer)
                    .SetFont(fNormal).SetFontSize(9)
                    .SetFontColor(grayColor)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginTop(10));

                doc.Add(new iText.Layout.Element.Paragraph(
                        $"Generated by SmartCafe Pro on {DateTime.Now:dd MMM yyyy HH:mm}")
                    .SetFont(fNormal).SetFontSize(7)
                    .SetFontColor(grayColor)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                doc.Close();
                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF generation failed: {ex.Message}", ex);
            }
        }

        private static void AddInfoRow(iText.Layout.Element.Table t,
            iText.Kernel.Font.PdfFont font, string label, string value)
        {
            var gray = new iText.Kernel.Colors.DeviceRgb(120,120,130);
            var dark = new iText.Kernel.Colors.DeviceRgb(30,30,30);
            t.AddCell(new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(label)
                    .SetFont(font).SetFontSize(9).SetFontColor(gray))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            t.AddCell(new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(value)
                    .SetFont(font).SetFontSize(9).SetFontColor(dark))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        }

        private static iText.Layout.Element.Cell ItemCell(string text,
            iText.Kernel.Font.PdfFont font, float size,
            iText.Kernel.Colors.Color bg,
            iText.Layout.Properties.TextAlignment align = iText.Layout.Properties.TextAlignment.LEFT)
        {
            return new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text)
                    .SetFont(font).SetFontSize(size)
                    .SetTextAlignment(align))
                .SetBackgroundColor(bg)
                .SetPadding(5);
        }

        private static void AddTotalRow(iText.Layout.Element.Table t,
            iText.Kernel.Font.PdfFont font, string label, string value,
            iText.Kernel.Colors.Color color)
        {
            t.AddCell(new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(label)
                    .SetFont(font).SetFontSize(9).SetFontColor(color))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            t.AddCell(new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(value)
                    .SetFont(font).SetFontSize(9).SetFontColor(color)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        }

        /// <summary>Opens the generated PDF with the system default viewer.</summary>
        public static void OpenPdf(string filePath)
        {
            if (File.Exists(filePath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = filePath,
                    UseShellExecute = true
                });
        }
    }

    // ==========================================================
    // Excel Report Exporter (EPPlus)
    // ==========================================================
    public static class ExcelReportService
    {
        /// <summary>
        /// Exports a DataTable to a styled Excel file using EPPlus.
        /// Returns the output file path.
        /// </summary>
        public static string ExportToExcel(
            System.Data.DataTable data,
            string reportTitle,
            string outputFolder = null)
        {
            // EPPlus 5+ requires license config for non-commercial use
            OfficeOpenXml.ExcelPackage.LicenseContext =
                OfficeOpenXml.LicenseContext.NonCommercial;

            outputFolder ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SmartCafePro", "Reports");
            Directory.CreateDirectory(outputFolder);

            var safeTitle = string.Concat(
                reportTitle.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(outputFolder,
                $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            using var pkg = new OfficeOpenXml.ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add(reportTitle);

            // ── Title row ────────────────────────────────────────
            ws.Cells[1, 1].Value = ServiceContainer.Settings.Get("CafeName", "SmartCafe Pro");
            ws.Cells[1, 1].Style.Font.Size  = 16;
            ws.Cells[1, 1].Style.Font.Bold  = true;
            ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(99,102,241));
            ws.Cells[1, 1, 1, data.Columns.Count].Merge = true;

            ws.Cells[2, 1].Value = $"{reportTitle}   |   Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
            ws.Cells[2, 1].Style.Font.Size = 9;
            ws.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            ws.Cells[2, 1, 2, data.Columns.Count].Merge = true;

            // ── Column headers ───────────────────────────────────
            int headerRow = 4;
            for (int c = 0; c < data.Columns.Count; c++)
            {
                var cell = ws.Cells[headerRow, c + 1];
                cell.Value = data.Columns[c].ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(
                    System.Drawing.Color.FromArgb(40, 40, 60));
                cell.Style.HorizontalAlignment =
                    OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // ── Data rows ────────────────────────────────────────
            var altBg = System.Drawing.Color.FromArgb(245, 245, 255);
            for (int r = 0; r < data.Rows.Count; r++)
            {
                var dataRow = data.Rows[r];
                bool isAlt  = r % 2 == 1;

                for (int c = 0; c < data.Columns.Count; c++)
                {
                    var cell  = ws.Cells[headerRow + 1 + r, c + 1];
                    var value = dataRow[c];

                    // Smart type conversion
                    if (value is decimal d)
                    {
                        cell.Value = d;
                        cell.Style.Numberformat.Format = "#,##0.00";
                    }
                    else if (value is int i)
                    {
                        cell.Value = i;
                    }
                    else if (value is DateTime dt)
                    {
                        cell.Value = dt;
                        cell.Style.Numberformat.Format = "dd-mmm-yyyy hh:mm";
                    }
                    else if (value is bool b)
                    {
                        cell.Value = b ? "Yes" : "No";
                    }
                    else
                    {
                        cell.Value = value?.ToString() ?? "";
                    }

                    if (isAlt)
                    {
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(altBg);
                    }
                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Hair,
                        System.Drawing.Color.FromArgb(200,200,210));
                }
            }

            // ── Summary footer ───────────────────────────────────
            int sumRow = headerRow + data.Rows.Count + 2;
            ws.Cells[sumRow, 1].Value = $"Total Records: {data.Rows.Count}";
            ws.Cells[sumRow, 1].Style.Font.Italic = true;
            ws.Cells[sumRow, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);

            // Auto-fit columns
            ws.Cells[ws.Dimension?.Address ?? "A1"].AutoFitColumns(8, 40);

            // Freeze header rows
            ws.View.FreezePanes(headerRow + 1, 1);

            // Auto-filter
            ws.Cells[headerRow, 1, headerRow, data.Columns.Count].AutoFilter = true;

            pkg.SaveAs(new FileInfo(filePath));
            return filePath;
        }

        /// <summary>Opens the Excel file with the system default application.</summary>
        public static void OpenExcel(string filePath)
        {
            if (File.Exists(filePath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = filePath,
                    UseShellExecute = true
                });
        }

        /// <summary>
        /// Convenience: export today's sales to Excel and open it.
        /// </summary>
        public static void QuickExportTodaySales()
        {
            try
            {
                using var conn = DataAccessLayer.DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT * FROM vw_DailySalesSummary ORDER BY SaleDate DESC", conn);
                var dt = new System.Data.DataTable();
                new Microsoft.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
                var path = ExportToExcel(dt, "Daily Sales Summary");
                OpenExcel(path);
            }
            catch (Exception ex)
            {
                Utilities.MessageHelper.ShowError($"Export failed: {ex.Message}");
            }
        }
    }
}
