using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? type, decimal? minPrice, decimal? maxPrice)
        {
            var query = _context.Services.AsQueryable();

            // Tiền phòng đã được xử lý ở nghiệp vụ đăng ký cư trú / hóa đơn nhận phòng,
            // nên không hiển thị trong danh mục dịch vụ phát sinh.
            query = query.Where(x => !x.Name.Contains("Tiền phòng") && !x.Name.Contains("Phí phòng"));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(keyword)
                    || x.Type.Contains(keyword)
                    || (x.Description != null && x.Description.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
            {
                query = query.Where(x => x.Type == type);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(x => x.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(x => x.Price <= maxPrice.Value);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Type = type;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            var data = await query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return View(data);
        }

        public IActionResult Create()
        {
            return View(new Service { Type = "Optional", Price = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (service.Name != null &&
                (service.Name.Contains("Tiền phòng", StringComparison.OrdinalIgnoreCase)
                 || service.Name.Contains("Phí phòng", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(service.Name), "Tiền phòng được xử lý ở quy trình đăng ký cư trú, không thêm vào danh mục dịch vụ.");
            }

            if (ModelState.IsValid)
            {
                service.Name = service.Name.Trim();
                service.Type = string.IsNullOrWhiteSpace(service.Type) ? "Optional" : service.Type.Trim();
                service.Description = string.IsNullOrWhiteSpace(service.Description) ? null : service.Description.Trim();

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã thêm dịch vụ mới.";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services.FirstOrDefaultAsync(m => m.Id == id);
            if (service == null) return NotFound();

            return View(service);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id) return NotFound();

            if (service.Name != null &&
                (service.Name.Contains("Tiền phòng", StringComparison.OrdinalIgnoreCase)
                 || service.Name.Contains("Phí phòng", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(service.Name), "Tiền phòng được xử lý ở quy trình đăng ký cư trú, không đưa vào danh mục dịch vụ.");
            }

            if (ModelState.IsValid)
            {
                service.Name = service.Name.Trim();
                service.Type = string.IsNullOrWhiteSpace(service.Type) ? "Optional" : service.Type.Trim();
                service.Description = string.IsNullOrWhiteSpace(service.Description) ? null : service.Description.Trim();

                _context.Update(service);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã cập nhật dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services.FirstOrDefaultAsync(m => m.Id == id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                var hasBooking = await _context.ServiceBookings.AnyAsync(x => x.ServiceId == id);
                if (hasBooking)
                {
                    TempData["Error"] = "Dịch vụ đã có yêu cầu đăng ký nên không thể xóa. Có thể đổi mô tả hoặc ngưng sử dụng trong quy trình demo.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa dịch vụ.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
