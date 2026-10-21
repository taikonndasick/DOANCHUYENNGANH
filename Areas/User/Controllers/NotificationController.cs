using QuanLyKTX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")] // Rất quan trọng để nhận diện Area
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang danh sách thông báo cho Cư dân
        public async Task<IActionResult> Index()
        {
            // Chỉ lấy thông báo IsActive = true để đồng bộ với quyền của Admin
            var notifications = await _context.Notifications
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        // Trang xem chi tiết một thông báo cụ thể
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.IsActive);

            if (notification == null) return NotFound();

            return View(notification);
        }
    }
}