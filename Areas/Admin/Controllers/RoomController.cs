using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, int? buildingId, string? roomType, string? status, int? floor, int page = 1, int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var query = _context.Apartments
                .Include(r => r.Building)
                .Include(r => r.Residents)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(r =>
                    r.ApartmentCode.Contains(keyword) ||
                    r.ApartmentType.Contains(keyword) ||
                    r.Status.Contains(keyword) ||
                    (r.Note != null && r.Note.Contains(keyword)) ||
                    (r.Building != null && r.Building.Name.Contains(keyword)) ||
                    (r.Building != null && r.Building.Code.Contains(keyword)));
            }

            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(roomType) && roomType != "Tất cả")
            {
                query = query.Where(r => r.ApartmentType == roomType);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(r => r.Status == status);
            }

            if (floor.HasValue && floor.Value > 0)
            {
                query = query.Where(r => r.Floor == floor.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderBy(r => r.ApartmentCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadRoomFilters(buildingId, roomType);
            ViewBag.Keyword = keyword;
            ViewBag.BuildingId = buildingId;
            ViewBag.RoomType = roomType;
            ViewBag.Status = status;
            ViewBag.Floor = floor;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(data);
        }

        public IActionResult Create()
        {
            LoadBuildingSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Apartment model)
        {
            if (ModelState.IsValid)
            {
                if (model.MaxOccupancy <= 0) model.MaxOccupancy = 4;
                if (string.IsNullOrWhiteSpace(model.Status)) model.Status = "Còn chỗ";
                _context.Apartments.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            LoadBuildingSelectList(model.BuildingId);
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var room = _context.Apartments.Find(id);
            if (room == null) return NotFound();
            LoadBuildingSelectList(room.BuildingId);
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Apartment model)
        {
            if (ModelState.IsValid)
            {
                if (model.CurrentOccupancy >= model.MaxOccupancy) model.Status = "Đã đầy";
                else if (model.Status != "Bảo trì" && model.Status != "Ngưng sử dụng") model.Status = "Còn chỗ";
                _context.Apartments.Update(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            LoadBuildingSelectList(model.BuildingId);
            return View(model);
        }

        private void LoadBuildingSelectList(int? selectedBuildingId = null)
        {
            var buildings = _context.Buildings.OrderBy(b => b.Code).ToList();
            ViewBag.BuildingId = new SelectList(buildings, "Id", "Name", selectedBuildingId);
        }

        private async Task LoadRoomFilters(int? selectedBuildingId = null, string? selectedRoomType = null)
        {
            var buildings = await _context.Buildings.OrderBy(b => b.Code).ToListAsync();
            ViewBag.Buildings = new SelectList(buildings, "Id", "Name", selectedBuildingId);

            var roomTypes = await _context.Apartments
                .Select(a => a.ApartmentType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
            ViewBag.RoomTypes = new SelectList(roomTypes, selectedRoomType);
        }
    }
}
