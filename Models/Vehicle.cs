using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey("ResidentProfileId")]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        [Display(Name = "Biển số xe")]
        public string LicensePlate { get; set; } = string.Empty;

        [Display(Name = "Loại phương tiện")]
        public string? VehicleType { get; set; }

        [Display(Name = "Ảnh biển số")]
        public string? LicensePlateImagePath { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";
    }
}
