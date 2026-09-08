using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qaydak.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Range(1, int.MaxValue, ErrorMessage = "لازم تختار عميل")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100, ErrorMessage = "نسبة الضريبة لازم تكون بين 0 و100")]
        public decimal VatRate { get; set; } = 15;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "الخصم لازم يكون صفر أو أكتر")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المدفوع لازم يكون صفر أو أكتر")]
        public decimal PaidAmount { get; set; } = 0;

        public bool IsPaid { get; set; } = false;
        public bool IsVoided { get; set; } = false;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public List<InvoiceItem> Items { get; set; } = new();

        public decimal GetSubtotal() => Items.Sum(i => i.Quantity * i.UnitPrice);

        public decimal GetDiscountedSubtotal()
        {
            var discounted = GetSubtotal() - DiscountAmount;
            return discounted < 0 ? 0 : discounted;
        }

        public decimal GetVatAmount() => GetDiscountedSubtotal() * (VatRate / 100);
        public decimal GetTotal() => GetDiscountedSubtotal() + GetVatAmount();
    }
}