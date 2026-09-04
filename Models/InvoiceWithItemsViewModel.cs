using System.ComponentModel.DataAnnotations;

namespace Qaydak.Models
{
    public class InvoiceWithItemsViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "لازم تختار عميل")]
        public int CustomerId { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الضريبة لازم تكون بين 0 و100")]
        public decimal VatRate { get; set; } = 15;

        public List<InvoiceItemViewModel> Items { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        [Required(ErrorMessage = "وصف البند مطلوب")]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "الكمية لازم تكون أكبر من صفر")]
        public decimal Quantity { get; set; } = 1;

        [Range(0.01, double.MaxValue, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        public decimal UnitPrice { get; set; }
    }
}