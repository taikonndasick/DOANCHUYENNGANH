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
    public class MedicalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MedicalController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<MedicalRegistration>());

            var data = await _context.MedicalRegistrations
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
        public async Task<IActionResult> Create(string registrationType, DateTime? preferredDate, string? note)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể đăng ký khám chữa bệnh.";
                return RedirectToAction(nameof(Index));
            }

            _context.MedicalRegistrations.Add(new MedicalRegistration
            {
                ResidentProfileId = resident.Id,
                RegistrationType = string.IsNullOrWhiteSpace(registrationType) ? "Khám sức khỏe" : registrationType.Trim(),
                PreferredDate = preferredDate,
                Note = note?.Trim(),
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi đăng ký khám chữa bệnh.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _context.ResidentProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
