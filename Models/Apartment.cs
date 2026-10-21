using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
	public class Apartment
	{
		[Key]
		public int Id { get; set; }

		[Display(Name = "Tòa nhà")]
		public int BuildingId { get; set; }

		[ForeignKey("BuildingId")]
		public virtual Building? Building { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập mã phòng")]
		[Display(Name = "Mã phòng")]
		public string ApartmentCode { get; set; } = string.Empty;

		[Required(ErrorMessage = "Vui lòng nhập loại phòng")]
		[Display(Name = "Loại phòng")]
		public string ApartmentType { get; set; } = string.Empty;

		[Display(Name = "Tầng")]
		public int? Floor { get; set; }

		[Display(Name = "Số lượng hiện tại")]
		public int CurrentOccupancy { get; set; } = 0;

		[Display(Name = "Số lượng tối đa")]
		public int MaxOccupancy { get; set; } = 4;

		[Display(Name = "Giá phòng")]
		public decimal RoomPrice { get; set; } = 0;

		[Display(Name = "Trạng thái")]
		public string Status { get; set; } = "Còn chỗ";

		[Display(Name = "Ghi chú")]
		public string? Note { get; set; }

		public virtual ICollection<ResidentProfile>? Residents { get; set; }
	}
}