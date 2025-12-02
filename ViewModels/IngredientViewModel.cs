using System.Globalization;

namespace POS_91Cafe.ViewModels
{
    public class IngredientViewModel
    {
        public int IngredientID { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal CurrentQuantity { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; } // raw stored cost from DB (may be per 1000 units)
        public decimal CostPerStoredUnit { get; set; } // computed (CostPrice / divisor)
        public decimal TotalValue { get; set; } // CurrentQuantity * CostPerStoredUnit
        public string Status { get; set; }

        public string CostPriceDisplay => CostPrice.ToString("C2", CultureInfo.GetCultureInfo("en-PH"));
        public string CostPerStoredUnitDisplay => CostPerStoredUnit.ToString("C4", CultureInfo.GetCultureInfo("en-PH"));
        public string TotalValueDisplay => TotalValue.ToString("C2", CultureInfo.GetCultureInfo("en-PH"));

        public decimal LowStockThreshold { get; internal set; }
    }
}