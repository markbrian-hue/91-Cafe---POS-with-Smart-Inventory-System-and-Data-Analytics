using POS_91Cafe.Models;
using System.Collections.Generic;

namespace POS_91Cafe.ViewModels
{
    // Helper class to hold grouped products
    public class ProductGroup
    {
        public string BaseName { get; set; }
        public List<Product> Products { get; set; }
    }

    public class SalesViewModel
    {
        // This new property replaces ViewBag.ProductGroups
        public List<ProductGroup> ProductGroups { get; set; }

        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Sale> RecentSales { get; set; }
    }
}