using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class MedicalRegistration
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey(nameof(ResidentProfileId))]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        [Display(Name = "Loại đăng ký")]
        public string RegistrationType { get; set; } = "Khám sức khỏe";

        [Display(Name = "Ngày mong muốn")]
        public DateTime? PreferredDate { get; set; }

        [Display(Name = "Triệu chứng / ghi chú")]
        public string? Note { get; set; }

        // Chờ duyệt / Đã duyệt / Từ chối / Đã khám
        public string Status { get; set; } = "Chờ duyệt";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
