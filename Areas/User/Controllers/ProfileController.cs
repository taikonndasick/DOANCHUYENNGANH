using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;
using System.Text.RegularExpressions;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private static readonly string[] AllowedPortraitExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxPortraitUploadSize = 5 * 1024 * 1024;

        private static readonly Dictionary<string, DateTime> AllowedStartDates = new()
        {
            { "2026-06-01", new DateTime(2026, 6, 1) },
            { "2026-07-01", new DateTime(2026, 7, 1) },
            { "2026-08-01", new DateTime(2026, 8, 1) }
        };

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync(includeResidence: false);
            ViewBag.PortraitPhotoPath = await GetPortraitPhotoPathAsync(resident);
            return View(resident);
        }

        public async Task<IActionResult> Residence()
        {
            var resident = await GetCurrentResidentAsync(includeResidence: true);

            var availableRooms = await _context.Apartments
                .Include(x => x.Building)
                .Where(x => x.Status != "Bảo trì" && x.Status != "Ngưng sử dụng" && x.CurrentOccupancy < x.MaxOccupancy)
                .OrderBy(x => x.Building != null ? x.Building.Name : "")
                .ThenBy(x => x.ApartmentCode)
                .ToListAsync();

            var pendingRoomRequest = false;
            if (resident != null)
            {
                pendingRoomRequest = await _context.StudentRequestForms.AnyAsync(x =>
                    x.ResidentProfileId == resident.Id &&
                    x.FormType == "Đăng ký phòng" &&
                    x.Status == "Chờ duyệt");
            }

            ViewBag.AvailableRooms = availableRooms;
            ViewBag.PendingRoomRequest = pendingRoomRequest;
            ViewBag.AllowedStartDates = AllowedStartDates.Values.ToList();

            return View(resident);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterRoom(int apartmentId, string startDateOption, string? note)
        {
            var resident = await GetCurrentResidentAsync(includeResidence: false);
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa được gắn với hồ sơ sinh viên nên chưa thể đăng ký phòng.";
                return RedirectToAction(nameof(Residence));
            }

            if (resident.ApartmentId.HasValue && (resident.Status == "Đang ở" || resident.Status == "Chờ thanh toán"))
            {
                TempData["Error"] = "Sinh viên đã có phòng hoặc đang chờ thanh toán nhận phòng nên không thể gửi thêm yêu cầu đăng ký phòng mới.";
                return RedirectToAction(nameof(Residence));
            }

            var hasPending = await _context.StudentRequestForms.AnyAsync(x =>
                x.ResidentProfileId == resident.Id &&
                x.FormType == "Đăng ký phòng" &&
                x.Status == "Chờ duyệt");

            if (hasPending)
            {
                TempData["Error"] = "Bạn đang có một yêu cầu đăng ký phòng chờ duyệt. Vui lòng chờ admin xử lý trước khi gửi yêu cầu mới.";
                return RedirectToAction(nameof(Residence));
            }

            if (string.IsNullOrWhiteSpace(startDateOption) || !AllowedStartDates.TryGetValue(startDateOption, out var startDate))
            {
                TempData["Error"] = "Thời gian lưu trú không hợp lệ. Sinh viên chỉ được chọn đợt 01/06, 01/07 hoặc 01/08/2026.";
                return RedirectToAction(nameof(Residence));
            }

            var endDate = startDate.AddYears(1);
            var numberOfMonths = CalculateMonthCount(startDate, endDate);

            var room = await _context.Apartments
                .Include(x => x.Building)
                .FirstOrDefaultAsync(x => x.Id == apartmentId);

            if (room == null)
            {
                TempData["Error"] = "Phòng bạn chọn không tồn tại.";
                return RedirectToAction(nameof(Residence));
            }

            if (room.Status == "Bảo trì" || room.Status == "Ngưng sử dụng" || room.CurrentOccupancy >= room.MaxOccupancy)
            {
                TempData["Error"] = "Phòng này hiện không còn khả dụng. Vui lòng chọn phòng khác.";
                return RedirectToAction(nameof(Residence));
            }

            var totalAmount = room.RoomPrice * numberOfMonths;

            var content =
                $"MaPhong: {room.Id}\n" +
                $"NgayBatDau: {startDate:yyyy-MM-dd}\n" +
                $"NgayKetThuc: {endDate:yyyy-MM-dd}\n" +
                $"SoThang: {numberOfMonths}\n" +
                $"DonGiaThang: {room.RoomPrice}\n" +
                $"TongTienDuKien: {totalAmount}\n" +
                $"Tòa nhà: {room.Building?.Name ?? "-"}\n" +
                $"Phòng: {room.ApartmentCode}\n" +
                $"Loại phòng: {room.ApartmentType}\n" +
                $"Số lượng hiện tại: {room.CurrentOccupancy}/{room.MaxOccupancy}\n" +
                $"Ngày bắt đầu dự kiến: {startDate:dd/MM/yyyy}\n" +
                $"Ngày kết thúc dự kiến: {endDate:dd/MM/yyyy}\n" +
                $"Đơn giá phòng: {room.RoomPrice:N0} đ/tháng\n" +
                $"Số tháng đăng ký: {numberOfMonths}\n" +
                $"Tổng tiền dự kiến: {totalAmount:N0} đ\n" +
                $"Ghi chú: {(string.IsNullOrWhiteSpace(note) ? "Không có" : note.Trim())}";

            _context.StudentRequestForms.Add(new StudentRequestForm
            {
                ResidentProfileId = resident.Id,
                FormType = "Đăng ký phòng",
                Title = $"Đăng ký phòng {room.ApartmentCode}",
                Content = content,
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi yêu cầu đăng ký phòng. Sau khi admin duyệt, hệ thống sẽ tạo hóa đơn nhận phòng ở trạng thái chờ thanh toán.";
            return RedirectToAction(nameof(Residence));
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var resident = await GetCurrentResidentAsync(includeResidence: false);
            return View(resident);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(string? phoneNumber, string? email, string? address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var resident = await _context.ResidentProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa được gắn với hồ sơ sinh viên. Vui lòng chờ admin duyệt hồ sơ.";
                return RedirectToAction(nameof(Settings));
            }

            phoneNumber = phoneNumber?.Trim();
            email = email?.Trim();
            address = address?.Trim();

            if (!string.IsNullOrWhiteSpace(phoneNumber) && !Regex.IsMatch(phoneNumber, @"^0\d{9}$"))
            {
                TempData["Error"] = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.";
                return RedirectToAction(nameof(Settings));
            }

            resident.PhoneNumber = phoneNumber;
            resident.Email = email;
            resident.Address = address;
            user.PhoneNumber = phoneNumber;
            user.Email = email;

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Đã cập nhật thông tin liên hệ.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePortraitPhoto(IFormFile? portraitPhoto)
        {
            var resident = await GetCurrentResidentAsync(includeResidence: false);
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên để cập nhật ảnh thẻ.";
                return RedirectToAction(nameof(Index));
            }

            if (portraitPhoto == null || portraitPhoto.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh thẻ mới trước khi lưu.";
                return RedirectToAction(nameof(Index));
            }

            if (portraitPhoto.Length > MaxPortraitUploadSize)
            {
                TempData["Error"] = "Dung lượng ảnh thẻ không được vượt quá 5MB.";
                return RedirectToAction(nameof(Index));
            }

            var extension = Path.GetExtension(portraitPhoto.FileName).ToLowerInvariant();
            if (!AllowedPortraitExtensions.Contains(extension))
            {
                TempData["Error"] = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.";
                return RedirectToAction(nameof(Index));
            }

            var registration = await FindCurrentRegistrationAsync(resident);
            if (registration == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ đăng ký gốc để cập nhật ảnh thẻ.";
                return RedirectToAction(nameof(Index));
            }

            var filePath = await SavePortraitPhotoAsync(portraitPhoto, registration.Id);
            var portraitDocument = registration.Documents
                .FirstOrDefault(x => x.DocumentType == "PortraitPhoto");

            if (portraitDocument == null)
            {
                registration.Documents.Add(new StudentRegistrationDocument
                {
                    StudentRegistrationId = registration.Id,
                    DocumentType = "PortraitPhoto",
                    FilePath = filePath,
                    OriginalFileName = portraitPhoto.FileName,
                    UploadedAt = DateTime.Now
                });
            }
            else
            {
                portraitDocument.FilePath = filePath;
                portraitDocument.OriginalFileName = portraitPhoto.FileName;
                portraitDocument.UploadedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật ảnh thẻ sinh viên.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync(bool includeResidence)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var query = _context.ResidentProfiles.AsQueryable();

            if (includeResidence)
            {
                query = query
                    .Include(x => x.Apartment)
                        .ThenInclude(a => a!.Building)
                    .Include(x => x.Apartment)
                        .ThenInclude(a => a!.Residents);
            }
            else
            {
                query = query.Include(x => x.Apartment);
            }

            return await query.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        private async Task<string?> GetPortraitPhotoPathAsync(ResidentProfile? resident)
        {
            var registration = await FindCurrentRegistrationAsync(resident);

            return registration?.Documents.FirstOrDefault(x => x.DocumentType == "PortraitPhoto")?.FilePath;
        }

        private async Task<StudentRegistration?> FindCurrentRegistrationAsync(ResidentProfile? resident)
        {
            if (resident == null) return null;

            return await _context.StudentRegistrations
                .Include(x => x.Documents)
                .Where(x => (!string.IsNullOrWhiteSpace(resident.UserId) && x.UserId == resident.UserId)
                    || (!string.IsNullOrWhiteSpace(resident.StudentCode) && x.StudentCode == resident.StudentCode))
                .OrderByDescending(x => x.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<string> SavePortraitPhotoAsync(IFormFile file, int registrationId)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "student-registrations", registrationId.ToString());
            Directory.CreateDirectory(folder);

            var safeFileName = $"PortraitPhoto_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, safeFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/student-registrations/{registrationId}/{safeFileName}";
        }

        private static int CalculateMonthCount(DateTime startDate, DateTime endDate)
        {
            var months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
            if (endDate.Day > startDate.Day) months++;
            return Math.Max(months, 1);
        }
    }
}
