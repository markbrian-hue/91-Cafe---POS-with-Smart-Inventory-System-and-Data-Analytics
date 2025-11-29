using System.Collections.Generic;

namespace POS_91Cafe.ViewModels
{
    public class AnalyticsViewModel
    {
        public decimal TodaysSales { get; set; }
        public int TodaysTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public string TopProduct { get; set; }
        public int TopProductUnits { get; set; }
        public decimal NextSevenDayForecast { get; set; }

        // === NEW: List for Product-Level Predictions ===
        public List<ProductForecast> ProductForecasts { get; set; } = new List<ProductForecast>();
    }

    public class ProductForecast
    {
        public string ProductName { get; set; }
        public double DailyAverage { get; set; } // Avg sold per day
        public int PredictedNextDay { get; set; }
        public int PredictedNextWeek { get; set; }
        public int PredictedNextMonth { get; set; }
        public string Trend { get; set; } // "Up", "Down", "Stable"
    }
}