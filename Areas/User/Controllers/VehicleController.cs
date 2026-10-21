using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public VehicleController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<Vehicle>());

            var data = await _context.Vehicles
                .Where(x => x.ResidentProfileId == resident.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string licensePlate, string? vehicleType, IFormFile? licensePlateImage)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể đăng ký biển số.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                ModelState.AddModelError(nameof(licensePlate), "Vui lòng nhập biển số xe.");
                return View();
            }

            if (licensePlateImage == null || licensePlateImage.Length == 0)
            {
                ModelState.AddModelError(nameof(licensePlateImage), "Vui lòng tải lên hình ảnh biển số xe.");
                return View();
            }

            var normalizedPlate = licensePlate.Trim().ToUpperInvariant();
            var exists = await _context.Vehicles.AnyAsync(x => x.LicensePlate == normalizedPlate && x.Status != "Từ chối");
            if (exists)
            {
                ModelState.AddModelError(nameof(licensePlate), "Biển số này đã tồn tại hoặc đang chờ duyệt.");
                return View();
            }

            string imagePath;
            try
            {
                imagePath = await SaveVehicleImageAsync(licensePlateImage);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(licensePlateImage), ex.Message);
                return View();
            }

            _context.Vehicles.Add(new Vehicle
            {
                ResidentProfileId = resident.Id,
                LicensePlate = normalizedPlate,
                VehicleType = string.IsNullOrWhiteSpace(vehicleType) ? "Xe máy" : vehicleType.Trim(),
                LicensePlateImagePath = imagePath,
                Status = "Chờ duyệt"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi đăng ký biển số. Vui lòng chờ admin duyệt.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveVehicleImageAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.");
            }

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "vehicles");
            Directory.CreateDirectory(folder);

            var fileName = $"vehicle_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/vehicles/{fileName}";
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _context.ResidentProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
