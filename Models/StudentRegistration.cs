using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class StudentRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã số sinh viên")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã số sinh viên phải gồm đúng 10 chữ số.")]
        [Display(Name = "Mã số sinh viên")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên sinh viên")]
        [Display(Name = "Họ tên sinh viên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số căn cước")]
        [Display(Name = "Số căn cước")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lớp")]
        [Display(Name = "Lớp")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập khoa")]
        [Display(Name = "Khoa")]
        public string Faculty { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản ngân hàng")]
        [Display(Name = "Số tài khoản ngân hàng")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng")]
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; } = string.Empty;

        // Chờ duyệt / Đã duyệt / Từ chối
        [Display(Name = "Trạng thái hồ sơ")]
        public string Status { get; set; } = "Chờ duyệt";

        [Display(Name = "Lý do từ chối")]
        public string? RejectReason { get; set; }

        [Display(Name = "Ngày gửi")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Display(Name = "Người duyệt")]
        public string? ReviewedByUserId { get; set; }

        [Display(Name = "Ngày duyệt")]
        public DateTime? ReviewedAt { get; set; }

        public ICollection<StudentRegistrationDocument> Documents { get; set; } = new List<StudentRegistrationDocument>();
    }
}
