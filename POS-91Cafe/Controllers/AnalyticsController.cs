using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- HELPER: CENTRALIZED DATE LOGIC ---
        // Updated to accept optional start/end strings
        private (DateTime Start, DateTime End) GetDateRange(string period, string startStr = null, string endStr = null)
        {
            var now = DateTime.Now;
            var today = now.Date;

            // 1. Check if period is a specific date (Format: yyyy-MM-dd)
            if (DateTime.TryParse(period, out DateTime specificDate))
            {
                // Return start and end of that specific day
                return (specificDate.Date, specificDate.Date.AddDays(1).AddTicks(-1));
            }

            // 2. Priority: Explicit Date Range (from custom picker)
            if (!string.IsNullOrEmpty(startStr) && DateTime.TryParse(startStr, out DateTime sDate))
            {
                DateTime eDate = now;
                if (!string.IsNullOrEmpty(endStr) && DateTime.TryParse(endStr, out DateTime parsedEnd))
                {
                    eDate = parsedEnd.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    eDate = sDate.Date.AddDays(1).AddTicks(-1);
                }
                return (sDate.Date, eDate);
            }

            // 3. Handle keywords
            return period?.ToLower() switch
            {
                "today" => (today, today.AddDays(1).AddTicks(-1)),
                "yesterday" => (today.AddDays(-1), today.AddTicks(-1)),
                "week" => (today.AddDays(-7), now),
                "month" => (today.AddDays(-30), now),
                "all" => (DateTime.MinValue, now), // <--- NEW: All Time
                _ => (DateTime.MinValue, now) // <--- UPDATED: Default is now All Time
            };
        }

        public async Task<IActionResult> Index(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            // 1. KPI Data
            var salesQuery = _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end);

            var totalSales = await salesQuery.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            var totalTxns = await salesQuery.CountAsync();
            var avgTicket = totalTxns > 0 ? totalSales / totalTxns : 0;

            // 2. Top Product
            var topProductQuery = await _context.SaleDetails
                .Include(sd => sd.Product)
                .Where(sd => sd.Sale.SaleDate >= start && sd.Sale.SaleDate <= end)
                .GroupBy(sd => sd.Product.Name)
                .Select(g => new { Name = g.Key, Count = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            // 3. Forecast Logic (Always based on historical context)
            var forecastDate = DateTime.Now.Date.AddDays(-30);
            var productStats = await _context.SaleDetails
                .Include(sd => sd.Product)
                .Where(sd => sd.Sale.SaleDate >= forecastDate)
                .GroupBy(sd => sd.Product.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    TotalSold = g.Sum(x => x.Quantity),
                    RecentSold = g.Where(x => x.Sale.SaleDate >= DateTime.Now.Date.AddDays(-7)).Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var forecasts = new List<ProductForecast>();
            foreach (var item in productStats)
            {
                double dailyAvg = (double)item.TotalSold / 30.0;
                double recentDailyAvg = (double)item.RecentSold / 7.0;

                string trend = "Stable";
                if (recentDailyAvg > dailyAvg * 1.1) trend = "Rising 🔥";
                else if (recentDailyAvg < dailyAvg * 0.9) trend = "Cooling ❄️";

                forecasts.Add(new ProductForecast
                {
                    ProductName = item.Name,
                    Trend = trend,
                    PredictedNextDay = (int)Math.Ceiling(dailyAvg),
                    PredictedNextWeek = (int)Math.Ceiling(dailyAvg * 7),
                    PredictedNextMonth = (int)Math.Ceiling(dailyAvg * 30)
                });
            }

            decimal dailyRunRate = (decimal)((end - start).TotalDays > 0
                ? (double)totalSales / (end - start).TotalDays
                : (double)totalSales);

            var revenueForecast = dailyRunRate * 7;

            var model = new AnalyticsViewModel
            {
                TodaysSales = totalSales,
                TodaysTransactions = totalTxns,
                AverageTransactionValue = avgTicket,
                TopProduct = topProductQuery?.Name ?? "No Data",
                TopProductUnits = topProductQuery?.Count ?? 0,
                NextSevenDayForecast = revenueForecast,
                ProductForecasts = forecasts
            };

            return View(model);
        }

        // --- API ENDPOINTS ---

        [HttpGet]
        public async Task<JsonResult> GetSalesGrowthData(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            var data = await _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { date = g.Key, total = g.Sum(s => s.TotalAmount) })
                .OrderBy(x => x.date)
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetHourlyTrafficData(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            var sales = await _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .Select(s => s.SaleDate)
                .ToListAsync();

            var result = sales
                .Select(d => d.AddHours(8)) // Adjust to PH Time
                .GroupBy(d => d.Hour)
                .Select(g => new {
                    hour = g.Key > 12 ? $"{g.Key - 12} PM" : (g.Key == 12 ? "12 PM" : (g.Key == 0 ? "12 AM" : $"{g.Key} AM")),
                    count = g.Count(),
                    sortKey = g.Key
                })
                .OrderBy(x => x.sortKey)
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetBestSellersData(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            var data = await _context.SaleDetails
                .Include(sd => sd.Product)
                .Where(sd => sd.Sale.SaleDate >= start && sd.Sale.SaleDate <= end)
                .GroupBy(sd => sd.Product.Name)
                .Select(g => new { productName = g.Key, quantitySold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.quantitySold)
                .Take(5)
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetProfitByProductData(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            var data = await _context.SaleDetails
                .Include(sd => sd.Product)
                .Where(sd => sd.Sale.SaleDate >= start && sd.Sale.SaleDate <= end)
                .GroupBy(sd => new { sd.Product.Name, sd.Product.CostPrice })
                .Select(g => new
                {
                    productName = g.Key.Name,
                    totalProfit = g.Sum(x => (x.UnitPrice - g.Key.CostPrice) * x.Quantity)
                })
                .OrderByDescending(x => x.totalProfit)
                .Take(5)
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetSalesByDayOfWeekData(string period = "month", string startDate = null, string endDate = null)
        {
            var (start, end) = GetDateRange(period, startDate, endDate);

            var data = await _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .ToListAsync();

            var result = data
                .GroupBy(s => s.SaleDate.DayOfWeek)
                .Select(g => new { day = g.Key.ToString(), total = g.Count() })
                .ToList();

            return Json(result);
        }
    }
}