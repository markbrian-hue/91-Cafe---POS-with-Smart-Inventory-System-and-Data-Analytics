using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_91Cafe.Models
{
    [Table("ProductIngredients")]
    public class ProductIngredient
    {
        [Key]
        public int ProductIngredientID { get; set; }

        public int ProductID { get; set; }
        public int IngredientID { get; set; }

        // === IMPORTANT CHANGE ===
        // This ensures your database supports decimals like 0.05 (e.g., 50ml)
        [Column(TypeName = "decimal(10, 2)")]
        public decimal QuantityUsed { get; set; }

        public virtual Product Product { get; set; }
        public virtual Ingredient Ingredient { get; set; }
    }
}