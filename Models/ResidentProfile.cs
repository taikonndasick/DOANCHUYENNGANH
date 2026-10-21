using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace QuanLyKTX.Models
{
    public class ResidentProfile
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên sinh viên")]
        [Display(Name = "Họ tên sinh viên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Mã số sinh viên")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã số sinh viên phải gồm đúng 10 chữ số.")]
        public string? StudentCode { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [Display(Name = "CCCD/CMND")]
        public string? NationalId { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Lớp")]
        public string? ClassName { get; set; }

        [Display(Name = "Khoa")]
        public string? Faculty { get; set; }

        [Display(Name = "Phòng")]
        public int? ApartmentId { get; set; }

        [ForeignKey("ApartmentId")]
        public virtual Apartment? Apartment { get; set; }

        // Chưa ở / Đang ở / Đã rời KTX / Bị khóa
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chưa ở";

        [Display(Name = "Ngày tạo hồ sơ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Vehicle>? Vehicles { get; set; }
        public virtual ICollection<ResidentCard>? ResidentCards { get; set; }
        public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
    }
}
