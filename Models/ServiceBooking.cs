using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class ServiceBooking
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey("ResidentProfileId")]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Done

        public string? Note { get; set; }
    }
}