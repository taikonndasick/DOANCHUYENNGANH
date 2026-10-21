using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username là bắt buộc.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}