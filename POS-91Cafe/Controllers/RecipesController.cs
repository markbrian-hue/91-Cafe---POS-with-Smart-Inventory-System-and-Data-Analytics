using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show ALL products always; we no longer filter server-side by category.
        // We still accept ?cat but only to remember what the user last chose if you later want to use it.
        public async Task<IActionResult> Index(string? cat = null)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductIngredients)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Build full category list (unfiltered) and remove "add-on" style categories
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Add Ons","Add Ons ","Add ons","Add-ons","AddOn","Addon","Add-Ons","Add Ons / Extras","Extras","Add Ons & Extras"
            };

            var categories = products
                .Select(p => p.Category?.Name?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(n => !excluded.Contains(n!) && !n!.Contains("add on", StringComparison.OrdinalIgnoreCase) && !n!.Contains("addon", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n)
                .ToList();

            // Include "Uncategorized" if any product has no category and it's not excluded
            if (products.Any(p => p.Category == null))
            {
                if (!categories.Contains("Uncategorized", StringComparer.OrdinalIgnoreCase))
                    categories.Insert(0, "Uncategorized");
            }

            ViewBag.AllCategories = categories;

            // We intentionally do NOT pre-select a category (so all items show after returning from Manage)
            ViewBag.InitialCategory = null;

            return View(products);
        }

        public async Task<IActionResult> Manage(int id, string? returnCategory = null)
        {
            var product = await _context.Products
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.Ingredient)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            ViewBag.AllIngredients = await _context.Ingredients
                .OrderBy(i => i.Name)
                .ToListAsync();

            // We pass returnCategory only if you later want to use it (not needed for back now)
            ViewBag.ReturnCategory = returnCategory;
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient(int ProductID, int IngredientID, decimal QuantityUsed, string? returnCategory = null)
        {
            var existing = await _context.ProductIngredients
                .FirstOrDefaultAsync(pi => pi.ProductID == ProductID && pi.IngredientID == IngredientID);

            if (existing != null)
            {
                existing.QuantityUsed = QuantityUsed;
                _context.Update(existing);
            }
            else
            {
                _context.ProductIngredients.Add(new ProductIngredient
                {
                    ProductID = ProductID,
                    IngredientID = IngredientID,
                    QuantityUsed = QuantityUsed
                });
            }

            await _context.SaveChangesAsync();
            await UpdateProductCost(ProductID);

            return RedirectToAction("Manage", new { id = ProductID, returnCategory });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveIngredient(int id, string? returnCategory = null)
        {
            var link = await _context.ProductIngredients
                .Include(pi => pi.Product)
                .FirstOrDefaultAsync(pi => pi.ProductIngredientID == id);

            if (link != null)
            {
                int productId = link.ProductID;
                _context.ProductIngredients.Remove(link);
                await _context.SaveChangesAsync();
                await UpdateProductCost(productId);
                return RedirectToAction("Manage", new { id = productId, returnCategory });
            }

            return RedirectToAction("Index");
        }

        private async Task UpdateProductCost(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.Ingredient)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product != null)
            {
                decimal newCost = product.ProductIngredients
                    .Sum(pi => pi.QuantityUsed * pi.Ingredient.CostPrice);

                if (product.ProductIngredients.Any())
                {
                    product.CostPrice = decimal.Round(newCost, 4);
                    product.IsComposite = true;
                }
                else
                {
                    product.IsComposite = false;
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}