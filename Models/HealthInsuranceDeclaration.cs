using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class HealthInsuranceDeclaration
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey(nameof(ResidentProfileId))]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        [Display(Name = "Mã thẻ BHYT")]
        public string InsuranceCode { get; set; } = string.Empty;

        [Display(Name = "Nơi đăng ký khám chữa bệnh")]
        public string? HospitalName { get; set; }

        [Display(Name = "Ngày hiệu lực")]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Ngày hết hạn")]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "File minh chứng")]
        public string? AttachmentPath { get; set; }

        public string? OriginalFileName { get; set; }

        // Chờ duyệt / Đã duyệt / Từ chối
        public string Status { get; set; } = "Chờ duyệt";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
