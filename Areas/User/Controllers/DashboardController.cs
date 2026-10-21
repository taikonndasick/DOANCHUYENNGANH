using QuanLyKTX.Data;
using QuanLyKTX.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy ID của người dùng đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth", new { area = "" });

            // 2. Lấy thông tin cư dân
            var resident = await _context.ResidentProfiles
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            // KIỂM TRA: Nếu tài khoản này chưa được liên kết với hồ sơ cư dân
            if (resident == null)
            {
                // Bạn có thể thông báo lỗi hoặc gán giá trị mặc định để không bị crash
                ViewBag.FullName = "Tài khoản chưa gán cư dân";
                ViewBag.TotalUnpaid = 0;
                ViewBag.ActiveServices = 0;
                ViewBag.Notifications = new List<Notification>(); // Tránh null cho view
                return View();
            }

            // 3. Lấy tổng tiền phí chưa thanh toán (Dùng resident.Id an toàn vì đã check null ở trên)
            var totalUnpaid = await _context.Invoices
                .Where(i => i.ResidentProfileId == resident.Id && i.Status == "Unpaid")
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0; // Thêm (decimal?) để tránh lỗi nếu không có dòng nào

            // 4. Đếm số lượng dịch vụ đang sử dụng
            var activeServices = await _context.ServiceBookings
                .CountAsync(s => s.ResidentProfileId == resident.Id && s.Status == "Active");

            // 5. Lấy danh sách thông báo
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.IsActive == true)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Truyền dữ liệu qua ViewBag
            ViewBag.FullName = resident.FullName;
            ViewBag.TotalUnpaid = totalUnpaid;
            ViewBag.ActiveServices = activeServices;

            return View();
        }
    }
}