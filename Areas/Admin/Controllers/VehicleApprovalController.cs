using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VehicleApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehicleApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.Vehicles
                .Include(x => x.ResidentProfile)
                .Where(x => x.Status == "Chờ duyệt" || x.Status == "Pending")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x => x.LicensePlate.Contains(keyword)
                    || (x.VehicleType != null && x.VehicleType.Contains(keyword))
                    || (x.ResidentProfile != null && x.ResidentProfile.FullName.Contains(keyword))
                    || (x.ResidentProfile != null && x.ResidentProfile.StudentCode != null && x.ResidentProfile.StudentCode.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            return View(await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            vehicle.Status = "Active";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã duyệt đăng ký biển số xe. Dữ liệu sẽ hiển thị ở mục Đăng ký biển số xe.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            vehicle.Status = "Từ chối";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối đăng ký biển số xe.";
            return RedirectToAction(nameof(Index));
        }
    }
}
