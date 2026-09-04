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
        [StringLength(15, MinimumLength = 15, ErrorMessage = "الرقم الضريبي لازم يكون 15 رقم")]
        public string VatNumber { get; set; } = string.Empty;
    }
}