using System;
using System.ComponentModel.DataAnnotations;

namespace POS_91Cafe.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}