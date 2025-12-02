using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_91Cafe.Models
{
    public class Ingredient
    {
        [Key]
        public int IngredientID { get; set; }

        [Required]
        public string Name { get; set; }

        // === FIX: Make these nullable (int?) so they are OPTIONAL ===
        public int? CategoryID { get; set; }
        public virtual Category? Category { get; set; }

        public int? SupplierID { get; set; }
        public virtual Supplier? Supplier { get; set; }
        // ===========================================================

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CostPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentQuantity { get; set; }

        [Required]
        public string Unit { get; set; } // e.g., kg, liters, pcs

        [Column(TypeName = "decimal(18, 2)")]
        public decimal LowStockThreshold { get; set; }
    }
}