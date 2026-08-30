using System.ComponentModel.DataAnnotations;

namespace Qaydak.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العميل مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم هاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }
}