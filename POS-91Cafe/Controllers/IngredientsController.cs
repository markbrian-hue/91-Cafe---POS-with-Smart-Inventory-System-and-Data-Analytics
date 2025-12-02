using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.Models;
using POS_91Cafe.ViewModels;
using POS_91Cafe.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace POS_91Cafe.Controllers
{
    [Authorize]
    public class IngredientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IngredientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ingredients
        public async Task<IActionResult> Index()
        {
            var ingredients = await _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var model = ingredients.Select(i =>
            {
                var costPerStored = UnitCostHelper.GetCostPerStoredUnit(i.CostPrice, i.Unit);
                var totalValue = i.CurrentQuantity * costPerStored;

                return new IngredientViewModel
                {
                    IngredientID = i.IngredientID,
                    Name = i.Name,
                    CategoryName = i.Category?.Name,
                    CurrentQuantity = i.CurrentQuantity,
                    Unit = i.Unit,
                    CostPrice = i.CostPrice,
                    CostPerStoredUnit = costPerStored,
                    TotalValue = decimal.Round(totalValue, 2),
                    Status = i.CurrentQuantity <= i.LowStockThreshold ? "Low Stock" : "Good",
                    LowStockThreshold = i.LowStockThreshold
                };
            }).ToList();

            return View(model);
        }

        // POST: Ingredients/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Ingredient ingredient)
        {
            // Remove fields not bound by the modal form
            ModelState.Remove("Category");
            ModelState.Remove("Supplier");
            ModelState.Remove("CategoryID");
            ModelState.Remove("SupplierID");
            ModelState.Remove("ProductIngredients");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = "Validation Failed: " + errors;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Read package inputs from the form (optional)
                var isPackage = (Request.Form["IsPackagePrice"].FirstOrDefault() ?? "").ToLower() == "on";
                var packageQtyStr = Request.Form["PackageQuantity"].FirstOrDefault() ?? "";
                var packageUnitStr = Request.Form["PackageUnit"].FirstOrDefault() ?? "";
                decimal packageQty = 0;

                if (isPackage && decimal.TryParse(packageQtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out packageQty) && packageQty > 0)
                {
                    // Convert package quantity to the stored unit (e.g., packageUnit=kg -> stored unit = g)
                    // This normalizes the package quantity into the same unit type as ingredient.Unit
                    decimal adjustedPackageQty = ConvertPackageQuantityToStoredUnit(packageQty, packageUnitStr, ingredient.Unit);

                    if (adjustedPackageQty <= 0)
                    {
                        TempData["ErrorMessage"] = "Unable to convert package quantity to ingredient unit. Please check Package Unit and Ingredient Unit.";
                        return RedirectToAction(nameof(Index));
                    }

                    // ingredient.CostPrice posted is treated as the package total price.
                    // Convert to cost per stored unit: perUnit = packagePrice / adjustedPackageQty
                    var perUnit = ingredient.CostPrice / adjustedPackageQty;
                    ingredient.CostPrice = decimal.Round(perUnit, 6); // store per stored unit
                    TempData["SuccessMessage"] = "Saved and converted package price to per-unit cost.";
                }

                if (ingredient.IngredientID == 0)
                {
                    _context.Add(ingredient);
                    if (TempData["SuccessMessage"] == null)
                        TempData["SuccessMessage"] = "Ingredient added successfully!";
                }
                else
                {
                    _context.Update(ingredient);
                    if (TempData["SuccessMessage"] == null)
                        TempData["SuccessMessage"] = "Ingredient updated successfully!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving ingredient: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Ingredients/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                var isUsed = await _context.ProductIngredients.AnyAsync(pi => pi.IngredientID == id);
                if (isUsed)
                {
                    TempData["ErrorMessage"] = "Cannot delete: This ingredient is used in a product recipe.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ingredient deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Ingredient not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper: converts a quantity declared in packageUnit into the same "stored unit" as storedUnit
        // e.g. packageQty=0.3, packageUnit="kg", storedUnit="g" => returns 300
        private decimal ConvertPackageQuantityToStoredUnit(decimal packageQty, string packageUnit, string storedUnit)
        {
            if (string.IsNullOrWhiteSpace(packageUnit) || string.IsNullOrWhiteSpace(storedUnit))
                return packageQty; // nothing to convert

            var p = packageUnit.Trim().ToLowerInvariant();
            var s = storedUnit.Trim().ToLowerInvariant();

            // Normalized base units: grams <-> kg, ml <-> l
            // Map packageUnit to grams/ml/pcs baseline
            decimal qtyInBase; // quantity expressed in base "smallest" unit (grams or ml or pcs)
            if (p == "kg" || p == "kilogram" || p == "kilograms")
                qtyInBase = packageQty * 1000m;
            else if (p == "g" || p == "gram" || p == "grams")
                qtyInBase = packageQty;
            else if (p == "l" || p == "liter" || p == "litre")
                qtyInBase = packageQty * 1000m; // liters -> ml base
            else if (p == "ml" || p == "milliliter" || p == "milliliters")
                qtyInBase = packageQty;
            else
                // For pcs, btl, etc. treat as 1:1
                qtyInBase = packageQty;

            // Now convert base to stored unit
            if (s == "kg" || s == "kilogram" || s == "kilograms")
                return qtyInBase / 1000m;
            if (s == "g" || s == "gram" || s == "grams")
                return qtyInBase;
            if (s == "l" || s == "liter" || s == "litre")
                return qtyInBase / 1000m;
            if (s == "ml" || s == "milliliter" || s == "milliliters")
                return qtyInBase;

            // fallback: treat as 1:1 for unit types we don't recognize
            return packageQty;
        }
    }
}