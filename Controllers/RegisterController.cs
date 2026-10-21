using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;

namespace QuanLyKTX.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Rooms(string? keyword, int? buildingId, bool onlyAvailable = false)
        {
            var query = _context.Apartments
                .Include(x => x.Building)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.ApartmentCode.Contains(keyword) ||
                    x.ApartmentType.Contains(keyword) ||
                    (x.Building != null && x.Building.Name.Contains(keyword)) ||
                    (x.Building != null && x.Building.Code.Contains(keyword)));
            }

            if (buildingId.HasValue)
            {
                query = query.Where(x => x.BuildingId == buildingId.Value);
            }

            if (onlyAvailable)
            {
                query = query.Where(x => x.Status != "Bảo trì" && x.CurrentOccupancy < x.MaxOccupancy);
            }

            ViewBag.Keyword = keyword;
            ViewBag.BuildingId = buildingId;
            ViewBag.OnlyAvailable = onlyAvailable;
            ViewBag.Buildings = new SelectList(_context.Buildings.OrderBy(x => x.Name).ToList(), "Id", "Name", buildingId);

            var rooms = query
                .OrderBy(x => x.Building != null ? x.Building.Name : "")
                .ThenBy(x => x.ApartmentCode)
                .ToList();

            return View(rooms);
        }

        [HttpGet]
        public IActionResult ChoosePriority()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Start()
        {
            return RedirectToAction("Register", "Auth");
        }
    }
}
