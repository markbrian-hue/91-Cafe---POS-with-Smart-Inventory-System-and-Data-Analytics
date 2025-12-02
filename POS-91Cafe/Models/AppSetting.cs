using System.ComponentModel.DataAnnotations;
namespace POS_91Cafe.Models
{
    public class AppSetting
    {
        [Key] public string Key { get; set; }
        [Required] public string Value { get; set; }
    }
}