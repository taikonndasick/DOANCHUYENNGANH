using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số sinh viên là bắt buộc.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã số sinh viên phải gồm đúng 10 chữ số.")]
        [Display(Name = "Mã số sinh viên")]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc.")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số căn cước là bắt buộc.")]
        [Display(Name = "Số căn cước")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lớp là bắt buộc.")]
        [Display(Name = "Lớp")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Khoa là bắt buộc.")]
        [Display(Name = "Khoa")]
        public string Faculty { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tài khoản ngân hàng là bắt buộc.")]
        [Display(Name = "Số tài khoản ngân hàng")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc.")]
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng tải lên căn cước công dân / căn cước.")]
        [Display(Name = "Căn cước công dân / Căn cước")]
        public IFormFile? CitizenIdFile { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên thẻ bảo hiểm y tế.")]
        [Display(Name = "Thẻ bảo hiểm y tế")]
        public IFormFile? HealthInsuranceFile { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên minh chứng trúng tuyển, nhập học hoặc thẻ sinh viên.")]
        [Display(Name = "Minh chứng trúng tuyển / nhập học / thẻ sinh viên")]
        public IFormFile? AdmissionProofFile { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên ảnh thẻ chân dung.")]
        [Display(Name = "Ảnh thẻ chân dung")]
        public IFormFile? PortraitPhotoFile { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
