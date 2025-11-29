using System.ComponentModel.DataAnnotations;
namespace POS_91Cafe.Models
{
    public class Supplier
    {
        [Key] public int SupplierID { get; set; }
        [Required] public string Name { get; set; }
        public string ContactInfo { get; set; }
    }
}