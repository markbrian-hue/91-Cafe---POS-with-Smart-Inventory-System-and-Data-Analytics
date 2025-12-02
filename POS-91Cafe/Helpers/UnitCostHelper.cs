using System;

namespace POS_91Cafe.Helpers
{
    public static class UnitCostHelper
    {
        // NOTE:
        // After this change we treat Ingredient.CostPrice as "cost per stored unit".
        // E.g. if Unit is "grams" and CostPrice = 0.021 then that means ₱0.021 per gram.
        //
        // The Save flow in IngredientsController now supports converting a package total
        // (e.g. ₱200 for 300 grams) into a per-stored-unit cost during Save.
        //
        // If you still have legacy entries where CostPrice was entered as "per 1000g"
        // you'll want to run the one-time SQL migration that was discussed earlier.
        public static decimal GetUnitDivisor(string unit)
        {
            // Keep divisor = 1 because CostPrice is expected to already be per stored unit.
            // This helper is kept so we can expand logic later if you want automatic
            // handling for "kg" / "L" typed units.
            return 1m;
        }

        public static decimal GetCostPerStoredUnit(decimal storedCostPrice, string unit)
        {
            var divisor = GetUnitDivisor(unit);
            if (divisor <= 0) divisor = 1;
            return decimal.Round(storedCostPrice / divisor, 6);
        }
    }
}