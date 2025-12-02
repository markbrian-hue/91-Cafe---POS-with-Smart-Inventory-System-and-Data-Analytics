using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // <--- ADD THIS NAMESPACE
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_91Cafe.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Product Name is required.")]
        public string Name { get; set; }

        // Ensure this is nullable (int?)
        public int? CategoryID { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal CostPrice { get; set; }

        public string Unit { get; set; } = "pc";
        public bool IsComposite { get; set; } = false;

        // === FIX IS HERE: Add [ValidateNever] ===
        [ValidateNever]
        public virtual Category Category { get; set; }

        [ValidateNever]
        public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
    }
}