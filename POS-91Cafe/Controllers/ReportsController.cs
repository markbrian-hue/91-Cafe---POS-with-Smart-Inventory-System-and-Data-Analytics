using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.ViewModels;
using OfficeOpenXml; // Ensure you have EPPlus package installed
using POS_91Cafe.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
namespace POS_91Cafe.Controllers;

    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports
        public async Task<IActionResult> Index(string preset = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            // ... (Date logic remains the same) ...
            DateTime phNow = DateTime.UtcNow.AddHours(8);
            DateTime today = phNow.Date;
            DateTime start = today;
            DateTime end = today.AddDays(1).AddTicks(-1);

            // 1. Determine Date Range
            if (startDate.HasValue && endDate.HasValue)
            {
                start = startDate.Value.Date;
                end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                preset = "Custom";
            }
            else
            {
                if (string.IsNullOrEmpty(preset)) preset = "This Month";
                switch (preset)
                {
                    case "Today": start = today; break;
                    case "Yesterday":
                        start = today.AddDays(-1);
                        end = today.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        break;
                    case "This Week":
                        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                        start = today.AddDays(-1 * diff);
                        break;
                    case "This Month":
                        start = new DateTime(today.Year, today.Month, 1);
                        break;
                    case "Last Month":
                        var firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                        start = firstDayLastMonth;
                        end = firstDayLastMonth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        break;
                    case "This Year":
                        start = new DateTime(today.Year, 1, 1);
                        break;
                }
            }

            // 2. Query DB (Convert Local to UTC for search)
            DateTime dbStart = start.AddHours(-8);
            DateTime dbEnd = end.AddHours(-8);

            // Base query for Sales in range
            var query = _context.Sales
                 .Where(s => s.SaleDate >= dbStart && s.SaleDate <= dbEnd);

            // Calculate Sales Totals
            decimal totalRevenue = await query.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            int transactionCount = await query.CountAsync();

            // Calculate Cost 
            // FIX: Query SaleDetails directly to avoid complex SelectMany translation issues
            decimal totalCost = await _context.SaleDetails
                .Where(sd => sd.Sale.SaleDate >= dbStart && sd.Sale.SaleDate <= dbEnd)
                .SumAsync(sd => (decimal?)(sd.Quantity * sd.Product.CostPrice)) ?? 0;

            decimal totalProfit = totalRevenue - totalCost;
            decimal avgSale = transactionCount > 0 ? totalRevenue / transactionCount : 0;

            // Fetch Data for View (Limit to 1000 to prevent browser crash)
            // We perform the Includes here for the list display
            var recentSales = await query
                .Include(s => s.SaleDetails)
                .OrderByDescending(s => s.SaleDate)
                .Take(1000)
                .Select(s => new ReportSaleItem
                {
                    SaleID = s.SaleID,
                    Date = s.SaleDate,
                    ItemCount = s.SaleDetails.Sum(i => i.Quantity),
                    Total = s.TotalAmount,
                    PaymentMethod = s.PaymentMethod
                }).ToListAsync();

            var model = new ReportsViewModel
            {
                DatePreset = preset,
                StartDate = start,
                EndDate = end,
                TotalRevenue = totalRevenue,
                TotalProfit = totalProfit,
                TransactionCount = transactionCount,
                AverageTicket = avgSale,
                RecentSales = recentSales
            };

            return View(model);
        }

        // ... (Rest of the controller remains the same) ...

        // NEW: Server-Side Export (Gets ALL data, no limits)
        public async Task<IActionResult> ExportTransactions(DateTime startDate, DateTime endDate)
        {
            // Convert to UTC for DB
            DateTime dbStart = startDate.AddHours(-8);
            DateTime dbEnd = endDate.AddDays(1).AddHours(-8).AddTicks(-1);

            var sales = await _context.Sales
                .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
                .Where(s => s.SaleDate >= dbStart && s.SaleDate <= dbEnd)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Transactions");

                // Headers
                ws.Cells[1, 1].Value = "Sale ID";
                ws.Cells[1, 2].Value = "Date (PH Time)";
                ws.Cells[1, 3].Value = "Items Summary";
                ws.Cells[1, 4].Value = "Payment Method";
                ws.Cells[1, 5].Value = "Total Amount";

                // Styling
                using (var range = ws.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var s in sales)
                {
                    var localDate = s.SaleDate.AddHours(8); // UTC to PH
                    var itemsSummary = string.Join(", ", s.SaleDetails.Select(d => $"{d.Quantity}x {d.Product?.Name}"));

                    ws.Cells[row, 1].Value = s.SaleID;
                    ws.Cells[row, 2].Value = localDate.ToString("yyyy-MM-dd HH:mm");
                    ws.Cells[row, 3].Value = itemsSummary;
                    ws.Cells[row, 4].Value = s.PaymentMethod;
                    ws.Cells[row, 5].Value = s.TotalAmount;
                    row++;
                }

                ws.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Transactions_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public async Task<IActionResult> Inventory()
        {
            // ... (Keep your existing Inventory action code here) ...
            // Re-pasting the inventory logic to ensure the file is complete
            var ingredients = await _context.Ingredients
               .Include(i => i.Category)
               .OrderBy(i => i.Category.Name)
               .ThenBy(i => i.Name)
               .ToListAsync();

            var vm = ingredients.Select(i => new ViewModels.IngredientViewModel
            {
                IngredientID = i.IngredientID,
                Name = i.Name,
                CurrentQuantity = i.CurrentQuantity,
                Unit = i.Unit,
                CostPrice = i.CostPrice,
                CostPerStoredUnit = UnitCostHelper.GetCostPerStoredUnit(i.CostPrice, i.Unit),
                TotalValue = decimal.Round(i.CurrentQuantity * UnitCostHelper.GetCostPerStoredUnit(i.CostPrice, i.Unit), 2),
                Status = i.CurrentQuantity <= i.LowStockThreshold ? "Low Stock" : "Good"
            }).ToList();

            return View(vm);
        }
    }
