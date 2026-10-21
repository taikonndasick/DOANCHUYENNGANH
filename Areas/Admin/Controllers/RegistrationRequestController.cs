using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;
using System.Data;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RegistrationRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RegistrationRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "Chờ duyệt", string? keyword = null)
        {
            ViewBag.Status = status;
            ViewBag.Keyword = keyword;

            var query = _context.StudentRegistrations
                .Include(x => x.Documents)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.FullName.Contains(keyword) ||
                    x.StudentCode.Contains(keyword) ||
                    x.NationalId.Contains(keyword) ||
                    x.Email.Contains(keyword) ||
                    x.PhoneNumber.Contains(keyword) ||
                    x.Username.Contains(keyword));
            }

            var registrations = await query.OrderByDescending(x => x.SubmittedAt).ToListAsync();
            return View(registrations);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var registration = await _context.StudentRegistrations
                .Include(x => x.Documents)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null) return NotFound();
            return View(registration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var registration = await _context.StudentRegistrations
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (registration == null) return NotFound();

                if (registration.Status != "Chờ duyệt")
                {
                    TempData["ErrorMessage"] = "Hồ sơ này đã được xử lý bởi quản lý khác. Vui lòng tải lại danh sách.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var existedResident = await _context.ResidentProfiles
                    .AnyAsync(x => x.StudentCode == registration.StudentCode || x.NationalId == registration.NationalId);

                if (existedResident)
                {
                    TempData["ErrorMessage"] = "Mã số sinh viên hoặc số căn cước đã tồn tại trong danh sách sinh viên đã được duyệt.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var user = registration.User ?? await _userManager.FindByIdAsync(registration.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản đăng nhập gắn với hồ sơ này.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                user.LockoutEnd = null;
                user.LockoutEnabled = true;
                user.EmailConfirmed = true;
                user.PhoneNumberConfirmed = true;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Không thể kích hoạt tài khoản đăng nhập của sinh viên.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                var resident = new ResidentProfile
                {
                    UserId = user.Id,
                    FullName = registration.FullName,
                    StudentCode = registration.StudentCode,
                    DateOfBirth = registration.DateOfBirth,
                    Gender = registration.Gender,
                    NationalId = registration.NationalId,
                    PhoneNumber = registration.PhoneNumber,
                    Email = registration.Email,
                    Address = registration.Address,
                    ClassName = registration.ClassName,
                    Faculty = registration.Faculty,
                    Status = "Chờ đăng ký phòng",
                    CreatedAt = DateTime.Now
                };

                _context.ResidentProfiles.Add(resident);

                registration.Status = "Đã duyệt";
                registration.ReviewedByUserId = _userManager.GetUserId(User);
                registration.ReviewedAt = DateTime.Now;
                registration.RejectReason = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Đã duyệt hồ sơ, kích hoạt tài khoản và đưa sinh viên vào danh sách sinh viên đã duyệt.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi duyệt hồ sơ. Vui lòng kiểm tra lại dữ liệu.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectReason)
        {
            var registration = await _context.StudentRegistrations
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null) return NotFound();

            if (registration.Status != "Chờ duyệt")
            {
                TempData["ErrorMessage"] = "Hồ sơ này đã được xử lý trước đó nên không thể từ chối lại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(Details), new { id });
            }

            registration.Status = "Từ chối";
            registration.RejectReason = rejectReason.Trim();
            registration.ReviewedByUserId = _userManager.GetUserId(User);
            registration.ReviewedAt = DateTime.Now;

            var user = registration.User ?? await _userManager.FindByIdAsync(registration.UserId);
            if (user != null)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã từ chối hồ sơ đăng ký.";
            return RedirectToAction(nameof(Index));
        }
    }
}
