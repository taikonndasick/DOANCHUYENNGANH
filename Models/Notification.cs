using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string? ImagePath { get; set; }

        [Display(Name = "Tài liệu (PDF)")]
        public string? PdfPath { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
       

    }
}
