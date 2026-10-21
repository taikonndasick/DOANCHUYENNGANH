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
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<StudentRequestForm>());

            var data = await _context.StudentRequestForms
                .Where(x => x.ResidentProfileId == resident.Id && (x.FormType == "Trả phòng" || x.FormType == "Sửa chữa" || x.FormType == "Yêu cầu"))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            return View(data);
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(DateTime? expectedCheckoutDate, string reason, string? roomCondition)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể gửi yêu cầu trả phòng.";
                return RedirectToAction(nameof(Index));
            }

            if (expectedCheckoutDate == null) ModelState.AddModelError(nameof(expectedCheckoutDate), "Vui lòng chọn ngày trả phòng dự kiến.");
            if (string.IsNullOrWhiteSpace(reason)) ModelState.AddModelError(nameof(reason), "Vui lòng nhập lý do trả phòng.");
            if (!ModelState.IsValid) return View();

            var content = $"Ngày trả dự kiến: {expectedCheckoutDate:dd/MM/yyyy}\nLý do: {reason.Trim()}\nTình trạng phòng: {(string.IsNullOrWhiteSpace(roomCondition) ? "Chưa ghi nhận" : roomCondition.Trim())}";
            _context.StudentRequestForms.Add(new StudentRequestForm
            {
                ResidentProfileId = resident.Id,
                FormType = "Trả phòng",
                Title = "Yêu cầu trả phòng",
                Content = content,
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi yêu cầu trả phòng. Vui lòng chờ admin duyệt.";
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
