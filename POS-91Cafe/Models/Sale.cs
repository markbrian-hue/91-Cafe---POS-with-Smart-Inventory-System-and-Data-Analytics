using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace POS_91Cafe.Models
{
    public class Sale
    {
        [Key]
        public int SaleID { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }

        // Removed: public bool IsVoided { get; set; }

        public List<SaleDetail> SaleDetails { get; set; }
    }
}