using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class OrderTicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderTicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /OrderTickets
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("Index");
        }

        // GET: /OrderTickets/GetTickets
        [HttpGet("GetTickets")]
        public async Task<IActionResult> GetTickets(
            [FromQuery] string? search,
            [FromQuery] string? payment,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Sales
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .AsQueryable();

            // === 1. DATE FILTER (UTC+8 PH Time) ===
            // Get current PH Time
            var phNow = DateTime.UtcNow.AddHours(8);

            // Determine Start/End dates (Default to today if null)
            var start = startDate ?? phNow.Date;
            var end = endDate ?? phNow.Date;

            // Convert PH Time input back to UTC for database query
            // Start of day in PH is -8 hours in UTC
            var startUtc = start.Date.AddHours(-8);
            // End of day in PH is start of next day -8 hours in UTC
            var endUtc = end.Date.AddDays(1).AddHours(-8);

            query = query.Where(s => s.SaleDate >= startUtc && s.SaleDate < endUtc);

            // === 2. SEARCH FILTER (ID or Item Name) ===
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                var cleanTerm = term.Replace("#", "");

                if (int.TryParse(cleanTerm, out int saleId))
                {
                    // Exact ID match
                    query = query.Where(s => s.SaleID == saleId);
                }
                else
                {
                    // Product Name match
                    query = query.Where(s => s.SaleDetails.Any(sd => sd.Product.Name.ToLower().Contains(term)));
                }
            }

            // === 3. PAYMENT FILTER (FIXED LOGIC) ===
            if (!string.IsNullOrWhiteSpace(payment) && payment != "All")
            {
                var p = payment.ToLower();

                if (p == "cash")
                {
                    // Strict check for Cash
                    query = query.Where(s => s.PaymentMethod == "Cash");
                }
                else if (p.Contains("gcash") || p.Contains("maya") || p == "digital")
                {
                    // Check for any digital wallet variation EXCEPT Cash
                    query = query.Where(s => s.PaymentMethod != "Cash");
                }
            }

            // === EXECUTE QUERY ===
            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Take(50) // Limit results
                .ToListAsync();

            // === MAP RESULTS ===
            var tickets = sales.Select(sale => new
            {
                saleId = sale.SaleID,
                saleDate = sale.SaleDate, // Javascript handles timezone
                // Normalize payment string for frontend consistency
                paymentMethod = sale.PaymentMethod == "Cash" ? "Cash" : "GCash / Maya",
                totalAmount = sale.TotalAmount,
                items = sale.SaleDetails.Select(sd => new
                {
                    productName = sd.Product?.Name ?? "(deleted)",
                    quantity = sd.Quantity,
                    lineTotal = sd.LineTotal
                })
            });

            return Ok(tickets);
        }
    }
}