using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace POS_91Cafe.Models
{
    public class SaleDetail
    {
        [Key] public int SaleDetailID { get; set; }
        public int SaleID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(10, 2)")] public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(10, 2)")] public decimal LineTotal { get; set; }
        public virtual Sale Sale { get; set; }
        public virtual Product Product { get; set; }
    }
}