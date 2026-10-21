using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BuildingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuildingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? status, string? genderTarget, int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var query = _context.Buildings
                .Include(b => b.Apartments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(b =>
                    b.Code.Contains(keyword) ||
                    b.Name.Contains(keyword) ||
                    (b.Address != null && b.Address.Contains(keyword)) ||
                    (b.Description != null && b.Description.Contains(keyword)) ||
                    b.Status.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(b => b.Status == status);
            }

            // Theo dữ liệu seed: A1, A2, C1 là tòa nam; B1, B2, C2 là tòa nữ.
            if (!string.IsNullOrWhiteSpace(genderTarget) && genderTarget != "Tất cả")
            {
                if (genderTarget == "Nam")
                {
                    query = query.Where(b => b.Code == "A1" || b.Code == "A2" || b.Code == "C1");
                }
                else if (genderTarget == "Nữ")
                {
                    query = query.Where(b => b.Code == "B1" || b.Code == "B2" || b.Code == "C2");
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderBy(b => b.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.GenderTarget = genderTarget;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(data);
        }

        public IActionResult Create() => View(new Building { Status = "Đang hoạt động" });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Building model)
        {
            if (ModelState.IsValid)
            {
                _context.Buildings.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var building = _context.Buildings.Find(id);
            if (building == null) return NotFound();
            return View(building);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Building model)
        {
            if (ModelState.IsValid)
            {
                _context.Buildings.Update(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
