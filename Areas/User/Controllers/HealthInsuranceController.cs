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
    public class HealthInsuranceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public HealthInsuranceController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<HealthInsuranceDeclaration>());

            var data = await _context.HealthInsuranceDeclarations
                .Where(x => x.ResidentProfileId == resident.Id)
                .OrderByDescending(x => x.CreatedAt)
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
        public async Task<IActionResult> Create(string insuranceCode, string? hospitalName, DateTime? validFrom, DateTime? validTo, IFormFile? attachment)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể khai báo BHYT.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(insuranceCode))
            {
                ModelState.AddModelError(nameof(insuranceCode), "Vui lòng nhập mã thẻ BHYT.");
                return View();
            }

            string? filePath = null;
            string? original = null;
            if (attachment != null && attachment.Length > 0)
            {
                filePath = await SaveFileAsync(attachment, resident.Id, "health-insurance");
                original = attachment.FileName;
            }

            _context.HealthInsuranceDeclarations.Add(new HealthInsuranceDeclaration
            {
                ResidentProfileId = resident.Id,
                InsuranceCode = insuranceCode.Trim(),
                HospitalName = hospitalName?.Trim(),
                ValidFrom = validFrom,
                ValidTo = validTo,
                AttachmentPath = filePath,
                OriginalFileName = original,
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi khai báo BHYT.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFileAsync(IFormFile file, int residentId, string folderName)
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", folderName, residentId.ToString());
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folderName}/{residentId}/{fileName}";
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _context.ResidentProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
