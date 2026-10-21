using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceCode { get; set; } = string.Empty;

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey("ResidentProfileId")]
        public ResidentProfile? ResidentProfile { get; set; }

        public int? ServiceBookingId { get; set; }

        [ForeignKey("ServiceBookingId")]
        public ServiceBooking? ServiceBooking { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Cancelled

        public string? Description { get; set; }
    }
}