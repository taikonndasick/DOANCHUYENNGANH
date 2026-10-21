using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.Models
{
    public class FormTemplate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên biểu mẫu")]
        [Display(Name = "Tên biểu mẫu")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập loại biểu mẫu")]
        [Display(Name = "Loại biểu mẫu")]
        public string Type { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Icon Bootstrap")]
        public string Icon { get; set; } = "bi-journal-text";

        [Display(Name = "Đường dẫn file mẫu")]
        public string? TemplateFilePath { get; set; }

        [Display(Name = "Tên file gốc")]
        public string? OriginalFileName { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Biểu mẫu tạm thời")]
        public bool IsTemporary { get; set; } = false;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
