using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class StudentRequestForm
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey(nameof(ResidentProfileId))]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        [Display(Name = "Loại biểu mẫu")]
        public string FormType { get; set; } = "Khác";

        [Required]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "File đính kèm")]
        public string? AttachmentPath { get; set; }

        [Display(Name = "Tên file gốc")]
        public string? OriginalFileName { get; set; }

        // Chờ duyệt / Đã duyệt / Từ chối / Đã xử lý
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chờ duyệt";

        [Display(Name = "Ngày gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày duyệt")]
        public DateTime? ReviewedAt { get; set; }

        [Display(Name = "Ghi chú xử lý")]
        public string? AdminNote { get; set; }

        [Display(Name = "Lý do từ chối")]
        public string? RejectReason { get; set; }
    }
}
