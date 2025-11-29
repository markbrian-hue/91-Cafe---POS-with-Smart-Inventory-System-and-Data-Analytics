using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace POS_91Cafe.Models
{
    public class Category
    {
        [Key] public int CategoryID { get; set; }
        [Required] public string Name { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}