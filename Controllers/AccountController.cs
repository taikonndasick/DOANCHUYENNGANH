using Microsoft.AspNetCore.Mvc;
using QuanLyKTX.Data;
using Microsoft.AspNetCore.Http; // Đừng quên cái này để dùng Session

namespace QuanLyKTX.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Kiểm tra nếu đã login rồi thì đá sang Dashboard luôn
            var role = HttpContext.Session.GetString("Role");
            if (!string.IsNullOrEmpty(role))
            {
                return RedirectByUserRole(role);
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Tìm user
            var user = _context.Users
                .FirstOrDefault(x => x.Username == username && x.Password == password);

            if (user == null)
            {
                // Dùng TempData để hiển thị lỗi ở trang Login cho đồng bộ
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            // Lưu thông tin vào Session
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectByUserRole(user.Role);
        }

        public IActionResult Logout()
        {

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        // Hàm phụ để điều hướng tránh lặp code (DRY - Don't Repeat Yourself)
        private IActionResult RedirectByUserRole(string role)
        {
            if (role == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (role == "Reception")
                return RedirectToAction("Index", "Dashboard", new { area = "Reception" });

            return RedirectToAction("Index", "Home");
        }
    }
}