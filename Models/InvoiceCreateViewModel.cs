using System.ComponentModel.DataAnnotations;

namespace Qaydak.Models
{
    public class InvoiceCreateViewModel
    {
        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "لازم تختار عميل")]
        public int CustomerId { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الضريبة لازم تكون بين 0 و100")]
        public decimal VatRate { get; set; } = 15;
    }
}