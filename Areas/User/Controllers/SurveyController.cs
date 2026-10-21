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
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SurveyController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<SurveyResponse>());

            var data = await _context.SurveyResponses
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
        public async Task<IActionResult> Create(string surveyTitle, int satisfactionLevel, string? feedback)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể gửi khảo sát.";
                return RedirectToAction(nameof(Index));
            }

            if (satisfactionLevel < 1 || satisfactionLevel > 5) satisfactionLevel = 5;

            _context.SurveyResponses.Add(new SurveyResponse
            {
                ResidentProfileId = resident.Id,
                SurveyTitle = string.IsNullOrWhiteSpace(surveyTitle) ? "Khảo sát mức độ hài lòng" : surveyTitle.Trim(),
                SatisfactionLevel = satisfactionLevel,
                Feedback = feedback?.Trim(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã gửi khảo sát.";
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
