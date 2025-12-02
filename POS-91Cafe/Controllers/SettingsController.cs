using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using POS_91Cafe.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS_91Cafe.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.AppSettings.ToDictionaryAsync(s => s.Key, s => s.Value);

            // Provide default values if they don't exist
            ViewBag.ShopName = settings.GetValueOrDefault("ShopName", "91 Cafe");
            ViewBag.ShopAddress = settings.GetValueOrDefault("ShopAddress", "");
            ViewBag.ContactInfo = settings.GetValueOrDefault("ContactInfo", "");
            ViewBag.CurrencySymbol = settings.GetValueOrDefault("CurrencySymbol", "₱");
            ViewBag.TaxRate = decimal.Parse(settings.GetValueOrDefault("TaxRate", "0.00"));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Dictionary<string, string> settings)
        {
            foreach (var (key, value) in settings)
            {
                var setting = await _context.AppSettings.FindAsync(key);
                if (setting == null)
                {
                    _context.AppSettings.Add(new AppSetting { Key = key, Value = value ?? "" });
                }
                else
                {
                    setting.Value = value ?? "";
                    _context.AppSettings.Update(setting);
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Settings saved successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}