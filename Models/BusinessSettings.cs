using System.ComponentModel.DataAnnotations;

namespace Qaydak.Models
{
    public class BusinessSettings
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم النشاط مطلوب")]
        [StringLength(100)]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم الضريبي مطلوب")]
        [RegularExpression(@"^3\d{13}3$", ErrorMessage = "الرقم الضريبي لازم يكون 15 رقم، يبدأ وينتهي بالرقم 3")]
        public string VatNumber { get; set; } = string.Empty;
    }
}