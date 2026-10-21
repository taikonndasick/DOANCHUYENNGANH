using QuanLyKTX.Data;
using QuanLyKTX.Models;
using QuanLyKTX.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(ApplicationDbContext context,
                                      IWebHostEnvironment hostEnvironment,
                                      IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _hubContext = hubContext;
        }

        // GET: Index
        public IActionResult Index(string? keyword)
        {
            var query = _context.Notifications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(query.OrderByDescending(n => n.CreatedAt).ToList());
        }

        // GET: Create
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notification model, IFormFile? imageFile, IFormFile? pdfFile)
        {
            if (ModelState.IsValid)
            {
                // BỔ SUNG: Thiết lập ngày tạo mặc định nếu Model chưa tự gán
                model.CreatedAt = DateTime.Now;

                string wwwRootPath = _hostEnvironment.WebRootPath;

                if (imageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string path = Path.Combine(wwwRootPath, @"uploads\images\");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    model.ImagePath = @"\uploads\images\" + fileName;
                }

                if (pdfFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(pdfFile.FileName);
                    string path = Path.Combine(wwwRootPath, @"uploads\docs\");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }
                    model.PdfPath = @"\uploads\docs\" + fileName;
                }

                _context.Notifications.Add(model);
                await _context.SaveChangesAsync();

                // SIGNALR: Gửi thông báo đến tất cả user online
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", model.Title, model.Content);

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Notification model, IFormFile? imageFile, IFormFile? pdfFile)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(model.ImagePath))
                    {
                        var oldPath = Path.Combine(wwwRootPath, model.ImagePath.TrimStart('\\'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadPath = Path.Combine(wwwRootPath, @"uploads\images\");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    model.ImagePath = @"\uploads\images\" + fileName;
                }

                if (pdfFile != null)
                {
                    if (!string.IsNullOrEmpty(model.PdfPath))
                    {
                        var oldPdfPath = Path.Combine(wwwRootPath, model.PdfPath.TrimStart('\\'));
                        if (System.IO.File.Exists(oldPdfPath)) System.IO.File.Delete(oldPdfPath);
                    }

                    string pdfName = Guid.NewGuid().ToString() + Path.GetExtension(pdfFile.FileName);
                    string pdfUploadPath = Path.Combine(wwwRootPath, @"uploads\docs\");
                    if (!Directory.Exists(pdfUploadPath)) Directory.CreateDirectory(pdfUploadPath);

                    using (var fileStream = new FileStream(Path.Combine(pdfUploadPath, pdfName), FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }
                    model.PdfPath = @"\uploads\docs\" + pdfName;
                }

                _context.Update(model);
                await _context.SaveChangesAsync();

                // TÙY CHỌN: Gửi SignalR thông báo là tin tức đã được cập nhật
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "[Cập nhật] " + model.Title, "Nội dung thông báo này đã được thay đổi.");

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // BỔ SUNG: GET: Delete - Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (!string.IsNullOrEmpty(notification.ImagePath))
                {
                    var imagePath = Path.Combine(wwwRootPath, notification.ImagePath.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }
                if (!string.IsNullOrEmpty(notification.PdfPath))
                {
                    var pdfPath = Path.Combine(wwwRootPath, notification.PdfPath.TrimStart('\\'));
                    if (System.IO.File.Exists(pdfPath)) System.IO.File.Delete(pdfPath);
                }
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(m => m.Id == id);
            if (notification == null) return NotFound();
            return View(notification);
        }
    }
}