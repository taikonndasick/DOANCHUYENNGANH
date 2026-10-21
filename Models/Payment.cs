using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "VietQR"; // VietQR, MoMo, Card

        public decimal Amount { get; set; }

        public string? TransferCode { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
    }
}