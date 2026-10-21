using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ResidentCardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidentCardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? status, string? expiredState, int page = 1, int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var today = DateTime.Today;
            var query = _context.ResidentCards
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.CardCode.Contains(keyword) ||
                    x.Status.Contains(keyword) ||
                    (x.ResidentProfile != null && x.ResidentProfile.FullName.Contains(keyword)) ||
                    (x.ResidentProfile != null && x.ResidentProfile.StudentCode != null && x.ResidentProfile.StudentCode.Contains(keyword)) ||
                    (x.ResidentProfile != null && x.ResidentProfile.PhoneNumber != null && x.ResidentProfile.PhoneNumber.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(expiredState) && expiredState != "Tất cả")
            {
                if (expiredState == "Còn hạn")
                {
                    query = query.Where(x => x.ExpiryDate == null || x.ExpiryDate >= today);
                }
                else if (expiredState == "Hết hạn")
                {
                    query = query.Where(x => x.ExpiryDate != null && x.ExpiryDate < today);
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.ExpiredState = expiredState;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(r => r.FullName).ToList(), "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ResidentCard model)
        {
            if (ModelState.IsValid)
            {
                _context.ResidentCards.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(r => r.FullName).ToList(), "Id", "FullName", model.ResidentProfileId);
            return View(model);
        }

        public IActionResult Details(int id)
        {
            var data = _context.ResidentCards
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.Vehicles)
                .FirstOrDefault(x => x.Id == id);

            if (data == null) return NotFound();
            return View(data);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var card = await _context.ResidentCards.FindAsync(id);
            if (card == null) return NotFound();

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(r => r.FullName).ToList(), "Id", "FullName", card.ResidentProfileId);
            return View(card);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResidentCard model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thẻ thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(r => r.FullName).ToList(), "Id", "FullName", model.ResidentProfileId);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.ResidentCards
                .Include(r => r.ResidentProfile)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (card == null) return NotFound();

            return View(card);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var card = await _context.ResidentCards.FindAsync(id);
            if (card != null)
            {
                _context.ResidentCards.Remove(card);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thẻ KTX khỏi hệ thống!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
