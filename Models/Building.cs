using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.Models
{
	public class Building
	{
		[Key]
		public int Id { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập mã tòa nhà")]
		[Display(Name = "Mã tòa nhà")]
		public string Code { get; set; } = string.Empty;

		[Required(ErrorMessage = "Vui lòng nhập tên tòa nhà")]
		[Display(Name = "Tên tòa nhà")]
		public string Name { get; set; } = string.Empty;

		[Display(Name = "Địa chỉ")]
		public string? Address { get; set; }

		[Display(Name = "Số tầng")]
		public int? TotalFloors { get; set; }

		[Display(Name = "Mô tả")]
		public string? Description { get; set; }

		[Display(Name = "Trạng thái")]
		public string Status { get; set; } = "Đang hoạt động";

		public virtual ICollection<Apartment>? Apartments { get; set; }
	}
}