using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;
using System.Data;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FormRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FormRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status = "Chờ duyệt", string? keyword = null)
        {
            var query = _context.StudentRequestForms
                .Include(x => x.ResidentProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x => x.Title.Contains(keyword)
                    || x.FormType.Contains(keyword)
                    || x.Content.Contains(keyword)
                    || (x.ResidentProfile != null && x.ResidentProfile.FullName.Contains(keyword))
                    || (x.ResidentProfile != null && x.ResidentProfile.StudentCode != null && x.ResidentProfile.StudentCode.Contains(keyword)));
            }

            ViewBag.Status = status;
            ViewBag.Keyword = keyword;
            return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var form = await _context.StudentRequestForms
                .Include(x => x.ResidentProfile)
                .ThenInclude(r => r!.Apartment)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var form = await _context.StudentRequestForms
                .Include(x => x.ResidentProfile)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (form == null) return NotFound();

            if (form.FormType == "Đăng ký phòng")
            {
                var result = await ApproveRoomRegistrationAsync(form, adminNote);
                if (!result.Success)
                {
                    TempData["Error"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            if (form.Status != "Chờ duyệt" && form.Status != "Pending")
            {
                TempData["Error"] = "Biểu mẫu này đã được quản lý khác xử lý. Vui lòng tải lại danh sách.";
                return RedirectToAction(nameof(Details), new { id });
            }

            form.Status = "Đã duyệt";
            form.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Đã duyệt biểu mẫu." : adminNote.Trim();
            form.ReviewedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt biểu mẫu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? rejectReason)
        {
            var form = await _context.StudentRequestForms.FindAsync(id);
            if (form == null) return NotFound();

            if (form.Status != "Chờ duyệt" && form.Status != "Pending")
            {
                TempData["Error"] = "Biểu mẫu này đã được xử lý trước đó nên không thể từ chối lại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            form.Status = "Từ chối";
            form.RejectReason = string.IsNullOrWhiteSpace(rejectReason) ? "Không hợp lệ" : rejectReason.Trim();
            form.ReviewedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã từ chối biểu mẫu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, string Message)> ApproveRoomRegistrationAsync(StudentRequestForm form, string? adminNote)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var currentForm = await _context.StudentRequestForms
                    .Include(x => x.ResidentProfile)
                    .FirstOrDefaultAsync(x => x.Id == form.Id);

                if (currentForm == null)
                {
                    return (false, "Không tìm thấy biểu mẫu đăng ký phòng.");
                }

                if (currentForm.Status != "Chờ duyệt" && currentForm.Status != "Pending")
                {
                    return (false, "Biểu mẫu này đã được quản lý khác xử lý. Không tạo thêm hóa đơn hoặc cập nhật phòng lần nữa.");
                }

                if (currentForm.ResidentProfile == null)
                {
                    return (false, "Không tìm thấy hồ sơ sinh viên của biểu mẫu này.");
                }

                var roomId = ExtractIntValue(currentForm.Content, "MaPhong:");
                if (roomId == null)
                {
                    return (false, "Không đọc được mã phòng trong biểu mẫu đăng ký phòng.");
                }

                var room = await _context.Apartments
                    .Include(x => x.Building)
                    .FirstOrDefaultAsync(x => x.Id == roomId.Value);

                if (room == null)
                {
                    return (false, "Phòng sinh viên đăng ký không tồn tại hoặc đã bị xóa.");
                }

                if (room.Status == "Bảo trì" || room.Status == "Ngưng sử dụng")
                {
                    return (false, "Phòng này đang bảo trì/ngưng sử dụng nên không thể duyệt đăng ký.");
                }

                var resident = await _context.ResidentProfiles.FirstOrDefaultAsync(x => x.Id == currentForm.ResidentProfileId);
                if (resident == null)
                {
                    return (false, "Không tìm thấy sinh viên cần phân phòng.");
                }

                if (resident.ApartmentId.HasValue && (resident.Status == "Đang ở" || resident.Status == "Chờ thanh toán"))
                {
                    return (false, "Sinh viên này hiện đã có phòng ở hoặc đang chờ thanh toán nhận phòng. Không thể duyệt thêm phòng mới.");
                }

                var currentOccupancy = await _context.ResidentProfiles
                    .CountAsync(x => x.ApartmentId == room.Id && (x.Status == "Đang ở" || x.Status == "Chờ thanh toán"));

                if (currentOccupancy >= room.MaxOccupancy)
                {
                    room.CurrentOccupancy = currentOccupancy;
                    room.Status = "Đã đầy";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (false, "Phòng đã đủ người nên không thể duyệt đăng ký.");
                }

                var startDate = ExtractDateValue(currentForm.Content, "NgayBatDau:") ?? DateTime.Today;
                var endDate = ExtractDateValue(currentForm.Content, "NgayKetThuc:") ?? startDate.AddYears(1);
                var numberOfMonths = ExtractIntValue(currentForm.Content, "SoThang:") ?? CalculateMonthCount(startDate, endDate);

                var monthlyPrice = room.RoomPrice;
                var totalAmount = monthlyPrice * numberOfMonths;

                resident.ApartmentId = room.Id;
                resident.Status = "Chờ thanh toán";

                room.CurrentOccupancy = currentOccupancy + 1;
                room.Status = room.CurrentOccupancy >= room.MaxOccupancy ? "Đã đầy" : "Còn chỗ";

                var invoice = new Invoice
                {
                    ResidentProfileId = resident.Id,
                    InvoiceCode = "INV" + DateTime.Now.Ticks.ToString()[^8..],
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(5),
                    Status = "Unpaid",
                    Description = $"Phí đăng ký nhận phòng {room.ApartmentCode} - kỳ lưu trú {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy} ({numberOfMonths} tháng x {monthlyPrice:N0} đ)"
                };

                _context.Invoices.Add(invoice);

                currentForm.Status = "Đã duyệt";
                currentForm.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                    ? $"Đã duyệt đăng ký phòng {room.ApartmentCode}. Hệ thống đã tạo hóa đơn nhận phòng ở trạng thái chờ thanh toán."
                    : adminNote.Trim();
                currentForm.ReviewedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Đã duyệt đăng ký phòng và tạo hóa đơn nhận phòng ở trạng thái chờ thanh toán.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Có lỗi xảy ra khi duyệt đăng ký phòng và tạo hóa đơn.");
            }
        }

        private static int? ExtractIntValue(string? content, string prefix)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Replace(prefix, "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (int.TryParse(value, out var result)) return result;
                }
            }

            return null;
        }

        private static decimal? ExtractDecimalValue(string? content, string prefix)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Replace(prefix, "", StringComparison.OrdinalIgnoreCase)
                        .Replace("đ", "")
                        .Replace(",", "")
                        .Trim();

                    if (decimal.TryParse(value, out var result)) return result;
                }
            }

            return null;
        }

        private static DateTime? ExtractDateValue(string? content, string prefix)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Replace(prefix, "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (DateTime.TryParse(value, out var date)) return date;
                }
            }

            return null;
        }

        private static int CalculateMonthCount(DateTime startDate, DateTime endDate)
        {
            var months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
            if (endDate.Day > startDate.Day) months++;
            return Math.Max(months, 1);
        }
    }
}
