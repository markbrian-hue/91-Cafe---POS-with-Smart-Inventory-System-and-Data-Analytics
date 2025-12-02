using System;
using System.Collections.Generic;

namespace POS_91Cafe.ViewModels
{
    public class ReportsViewModel
    {
        public string DatePreset { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTicket { get; set; }

        public List<ReportSaleItem> RecentSales { get; set; }
    }

    public class ReportSaleItem
    {
        public int SaleID { get; set; }
        public DateTime Date { get; set; }
        public int ItemCount { get; set; }
        public string PaymentMethod { get; set; } // Added
        public decimal Total { get; set; }
    }
}