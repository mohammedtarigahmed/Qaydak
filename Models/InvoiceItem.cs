using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qaydak.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "وصف البند مطلوب")]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "الكمية لازم تكون أكبر من صفر")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        public decimal UnitPrice { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }
}