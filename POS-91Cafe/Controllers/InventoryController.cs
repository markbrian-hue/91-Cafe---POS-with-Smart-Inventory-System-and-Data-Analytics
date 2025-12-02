using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.Models;
using POS_91Cafe.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory (Main List)
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            // Grouping logic
            var productGroups = products
                .GroupBy(p =>
                {
                    var name = p.Name;
                    // Clean up suffixes for grouping
                    if (name.Contains(" (M)")) return name.Replace(" (M)", "");
                    if (name.Contains(" (L)")) return name.Replace(" (L)", "");
                    if (name.Contains("(") && name.EndsWith(")"))
                        return name.Substring(0, name.LastIndexOf("(")).Trim();
                    return name;
                })
                .Select(g => new ProductGroup
                {
                    BaseName = g.Key,
                    Products = g.ToList()
                })
                .ToList();

            return View(productGroups);
        }

        // GET: Inventory/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View();
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // 1. Auto-append Unit to Name if not already present
            // e.g. Name="Latte", Unit="Large" => "Latte (Large)"
            if (!string.IsNullOrWhiteSpace(product.Unit) &&
                !product.Name.Contains(product.Unit) &&
                !product.Unit.Equals("pc", StringComparison.OrdinalIgnoreCase))
            {
                product.Name = $"{product.Name} ({product.Unit})";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // Handle duplicate name error
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                    {
                        ModelState.AddModelError("Name", $"The product name '{product.Name}' already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                }
            }

            // If failure, reload categories and show form again
            await PopulateCategoriesAsync(product.CategoryID);
            return View(product);
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            await PopulateCategoriesAsync(product.CategoryID);
            return View(product);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductID == id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                    {
                        ModelState.AddModelError("Name", "A product with this name already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Database error: " + ex.Message);
                    }
                }
            }
            await PopulateCategoriesAsync(product.CategoryID);
            return View(product);
        }

        // POST: Inventory/DeleteGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return BadRequest();

            // 1. Find the products to delete
            var allProducts = await _context.Products.ToListAsync();
            var productsToDelete = allProducts
                .Where(p => p.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase) ||
                            p.Name.StartsWith(baseName + " (", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (productsToDelete.Any())
            {
                var productIds = productsToDelete.Select(p => p.ProductID).ToList();

                // --- STEP 2: Delete Recipe Links (ProductIngredients) ---
                var relatedIngredients = _context.ProductIngredients
                                                 .Where(pi => productIds.Contains(pi.ProductID));
                _context.ProductIngredients.RemoveRange(relatedIngredients);

                // --- STEP 3: Delete Sales History (SaleDetails) ---
                // This fixes the "saledetails_ibfk_2" error
                var relatedSales = _context.SaleDetails
                                           .Where(sd => productIds.Contains(sd.ProductID));
                _context.SaleDetails.RemoveRange(relatedSales);

                // --- STEP 4: Delete the Products ---
                _context.Products.RemoveRange(productsToDelete);

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper Method to Populate Categories Dropdown safely
        private async Task PopulateCategoriesAsync(int? selectedId = null)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            // Pass the list to ViewBag
            ViewBag.CategoryID = new SelectList(categories, "CategoryID", "Name", selectedId);

            // Optional warning if DB is empty (useful for fresh installs)
            if (!categories.Any())
            {
                ViewBag.Warning = "No categories found. Please add a Category in the database first.";
            }
        }
    }
}