using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehicleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Mục quản lý danh mục chỉ hiển thị biển số đã duyệt.
        public async Task<IActionResult> Index(string? keyword, string? vehicleType, int? buildingId, int page = 1, int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var query = _context.Vehicles
                .Include(v => v.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                        .ThenInclude(a => a!.Building)
                .Where(v => v.Status == "Active" || v.Status == "Đã duyệt")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(v =>
                    v.LicensePlate.Contains(keyword) ||
                    (v.VehicleType != null && v.VehicleType.Contains(keyword)) ||
                    (v.ResidentProfile != null && v.ResidentProfile.FullName.Contains(keyword)) ||
                    (v.ResidentProfile != null && v.ResidentProfile.StudentCode != null && v.ResidentProfile.StudentCode.Contains(keyword)) ||
                    (v.ResidentProfile != null && v.ResidentProfile.PhoneNumber != null && v.ResidentProfile.PhoneNumber.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(vehicleType) && vehicleType != "Tất cả")
            {
                query = query.Where(v => v.VehicleType == vehicleType);
            }

            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(v => v.ResidentProfile != null
                    && v.ResidentProfile.Apartment != null
                    && v.ResidentProfile.Apartment.BuildingId == buildingId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderBy(v => v.LicensePlate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadVehicleFilters(buildingId, vehicleType);
            ViewBag.Keyword = keyword;
            ViewBag.VehicleType = vehicleType;
            ViewBag.BuildingId = buildingId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Residents = new SelectList(
                _context.ResidentProfiles
                    .Where(x => x.Status == "Đang ở" || x.Status == "Hoạt động" || x.Status == "Đã duyệt")
                    .OrderBy(x => x.FullName)
                    .ToList(),
                "Id",
                "FullName");
            return View(new Vehicle { Status = "Active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle model)
        {
            if (string.IsNullOrWhiteSpace(model.Status)) model.Status = "Active";

            if (ModelState.IsValid)
            {
                _context.Vehicles.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName", model.ResidentProfileId);
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName", vehicle.ResidentProfileId);
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vehicle model)
        {
            if (ModelState.IsValid)
            {
                _context.Vehicles.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName", model.ResidentProfileId);
            return View(model);
        }

        private async Task LoadVehicleFilters(int? selectedBuildingId = null, string? selectedVehicleType = null)
        {
            var buildings = await _context.Buildings.OrderBy(b => b.Code).ToListAsync();
            ViewBag.Buildings = new SelectList(buildings, "Id", "Name", selectedBuildingId);

            var types = await _context.Vehicles
                .Where(v => v.Status == "Active" || v.Status == "Đã duyệt")
                .Where(v => v.VehicleType != null && v.VehicleType != "")
                .Select(v => v.VehicleType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
            ViewBag.VehicleTypes = new SelectList(types, selectedVehicleType);
        }
    }
}
