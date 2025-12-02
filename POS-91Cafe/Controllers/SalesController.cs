using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.Models;
using POS_91Cafe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SalesController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            // UPDATED: Include Ingredients to calculate MaxStock in View
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.Ingredient)
                .ToListAsync();

            // Normalize base name logic
            string NormalizeBaseName(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                var n = name.Trim();
                var pattern = @"\s*(?:[\-\(]?\s*(?:M|L|Medium|Large)\s*\)?)\s*$";
                return Regex.Replace(n, pattern, "", RegexOptions.IgnoreCase).Trim();
            }

            var productGroups = products
                .GroupBy(p => NormalizeBaseName(p.Name ?? string.Empty))
                .Select(g => new ProductGroup { BaseName = g.Key, Products = g.ToList() })
                .OrderBy(g => g.BaseName)
                .ToList();

            // Filter Categories
            var salesCategories = await _context.Categories
                .Where(c => c.Name != "Add-ons" && c.Name != "General Supplies" && c.Name != "Packaging")
                .ToListAsync();

            var viewModel = new SalesViewModel
            {
                ProductGroups = productGroups,
                Categories = salesCategories
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSale([FromBody] Sale saleData)
        {
            if (saleData == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            if (saleData.SaleDetails == null || !saleData.SaleDetails.Any())
                return BadRequest(new { success = false, message = "Cart is empty." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                saleData.SaleDate = DateTime.UtcNow;

                // 1. Normalize Payment Method (Merge GCash/Maya)
                if (!string.IsNullOrEmpty(saleData.PaymentMethod))
                {
                    var pm = saleData.PaymentMethod.ToLower();
                    if (pm.Contains("gcash") || pm.Contains("maya") || pm.Contains("digital"))
                    {
                        saleData.PaymentMethod = "GCash / Maya";
                    }
                }

                decimal calculatedTotal = 0;

                // Dictionary to aggregate total demand for each ingredient across the entire cart
                // Key: IngredientID, Value: Total Quantity Needed
                var ingredientDemand = new Dictionary<int, decimal>();

                // 2. First Loop: Calculate Totals & Aggregate Ingredient Needs
                foreach (var detail in saleData.SaleDetails)
                {
                    var product = await _context.Products
                        .Include(p => p.ProductIngredients)
                        .FirstOrDefaultAsync(p => p.ProductID == detail.ProductID);

                    if (product == null)
                        throw new Exception($"Product ID {detail.ProductID} not found.");

                    // Set Price from Database (Security)
                    detail.UnitPrice = product.SellingPrice;
                    detail.LineTotal = product.SellingPrice * detail.Quantity;
                    calculatedTotal += detail.LineTotal;

                    // Aggregate Ingredient Usage
                    if (product.ProductIngredients != null)
                    {
                        foreach (var pi in product.ProductIngredients)
                        {
                            if (!ingredientDemand.ContainsKey(pi.IngredientID))
                            {
                                ingredientDemand[pi.IngredientID] = 0;
                            }
                            // Add usage for this line item to the total demand
                            ingredientDemand[pi.IngredientID] += pi.QuantityUsed * detail.Quantity;
                        }
                    }
                }

                // 3. Second Loop: Validate Stock & Deduct
                // We check the TOTAL aggregated demand against the database to prevent split-item bypass
                foreach (var demand in ingredientDemand)
                {
                    var ingredientId = demand.Key;
                    var totalRequired = demand.Value;

                    var ingredient = await _context.Ingredients.FindAsync(ingredientId);

                    if (ingredient == null) continue;

                    if (ingredient.CurrentQuantity < totalRequired)
                    {
                        // Explicit error message showing the shortfall
                        throw new Exception($"Insufficient stock for '{ingredient.Name}'.\nTotal Required: {totalRequired:0.##} {ingredient.Unit}\nAvailable: {ingredient.CurrentQuantity:0.##} {ingredient.Unit}");
                    }

                    // Deduct stock
                    ingredient.CurrentQuantity -= totalRequired;
                    _context.Ingredients.Update(ingredient);
                }

                // 4. Save Sale
                saleData.TotalAmount = calculatedTotal;
                _context.Sales.Add(saleData);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Sale recorded successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}