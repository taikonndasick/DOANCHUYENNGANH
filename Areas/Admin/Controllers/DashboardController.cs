using Microsoft.AspNetCore.Mvc;
using QuanLyKTX.Data;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context) { _context = context; }

        public IActionResult Index()
        {
            ViewBag.TotalResidents = _context.ResidentProfiles.Count();
            ViewBag.TotalVehicles = _context.Vehicles.Count(x => x.Status == "Active" || x.Status == "Đã duyệt");
            ViewBag.PendingRegistrations = _context.StudentRegistrations.Count(x => x.Status == "Chờ duyệt");
            ViewBag.PendingVehicles = _context.Vehicles.Count(x => x.Status == "Chờ duyệt" || x.Status == "Pending");
            ViewBag.PendingForms = _context.StudentRequestForms.Count(x => x.Status == "Chờ duyệt");
            ViewBag.UnpaidInvoices = _context.Invoices.Count(x => x.Status == "Unpaid");
            ViewBag.TotalRooms = _context.Apartments.Count();
            ViewBag.AvailableRooms = _context.Apartments.Count(x => x.Status != "Bảo trì" && x.CurrentOccupancy < x.MaxOccupancy);
            ViewBag.RecentNotifications = _context.Notifications.OrderByDescending(n => n.CreatedAt).Take(3).ToList();
            return View();
        }
    }
}
