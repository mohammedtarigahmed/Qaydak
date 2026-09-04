using System.ComponentModel.DataAnnotations;

namespace Qaydak.Models
{
    public class InvoiceEditViewModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "لازم تختار عميل")]
        public int CustomerId { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الضريبة لازم تكون بين 0 و100")]
        public decimal VatRate { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}