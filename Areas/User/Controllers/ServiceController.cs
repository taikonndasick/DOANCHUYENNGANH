using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ServiceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? keyword, string? type)
        {
            var userId = _userManager.GetUserId(User);
            var resident = await _context.ResidentProfiles
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resident == null)
            {
                ViewBag.MyServices = new List<ServiceBooking>();
                ViewBag.Keyword = keyword;
                ViewBag.Type = type;
                TempData["Error"] = "Tài khoản của bạn chưa được gán hồ sơ sinh viên. Vui lòng liên hệ ban quản lý.";
                return View(new List<Service>());
            }

            var myServices = await _context.ServiceBookings
                .Include(s => s.Service)
                .Where(s => s.ResidentProfileId == resident.Id && s.Status != "Cancelled")
                .OrderByDescending(s => s.BookingDate)
                .ToListAsync();

            var query = _context.Services.AsQueryable();

            // Không hiển thị tiền phòng trong dịch vụ, vì tiền phòng thuộc hóa đơn cư trú.
            query = query.Where(x => !x.Name.Contains("Tiền phòng") && !x.Name.Contains("Phí phòng"));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.Type.Contains(keyword)
                    || (x.Description != null && x.Description.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
            {
                query = query.Where(x => x.Type == type);
            }

            ViewBag.MyServices = myServices;
            ViewBag.Keyword = keyword;
            ViewBag.Type = type;
            ViewBag.Resident = resident;

            var services = await query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return View(services);
        }

        public async Task<IActionResult> Register(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (service.Type == "Monthly")
            {
                TempData["Error"] = "Khoản dịch vụ này được ban quản lý lập vào hóa đơn tháng, sinh viên không cần đăng ký riêng.";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegister(int serviceId, string? note)
        {
            var userId = _userManager.GetUserId(User);
            var resident = await _context.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
            if (resident == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ sinh viên.";
                return RedirectToAction(nameof(Index));
            }

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            if (service.Type == "Monthly")
            {
                TempData["Error"] = "Khoản dịch vụ này thuộc hóa đơn tháng, không cần đăng ký riêng.";
                return RedirectToAction(nameof(Index));
            }

            var alreadyPendingOrActive = await _context.ServiceBookings.AnyAsync(sb =>
                sb.ResidentProfileId == resident.Id
                && sb.ServiceId == serviceId
                && sb.Status != "Cancelled"
                && sb.Status != "Rejected"
                && sb.BookingDate.Month == DateTime.Now.Month
                && sb.BookingDate.Year == DateTime.Now.Year);

            if (alreadyPendingOrActive)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu dịch vụ này trong tháng hiện tại.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = new ServiceBooking
                {
                    ServiceId = service.Id,
                    ResidentProfileId = resident.Id,
                    BookingDate = DateTime.Now,
                    Status = service.Price > 0 ? "PendingPayment" : "Pending",
                    Note = string.IsNullOrWhiteSpace(note) ? $"Đăng ký dịch vụ {service.Name}" : note.Trim()
                };

                _context.ServiceBookings.Add(booking);
                await _context.SaveChangesAsync();

                if (service.Price > 0)
                {
                    var invoice = new Invoice
                    {
                        ResidentProfileId = resident.Id,
                        ServiceBookingId = booking.Id,
                        InvoiceCode = "DV" + DateTime.Now.ToString("yyyyMMddHHmmss") + booking.Id.ToString("D4"),
                        TotalAmount = service.Price,
                        DueDate = DateTime.Now.AddDays(7),
                        Status = "Unpaid",
                        Description = $"Hóa đơn dịch vụ: {service.Name}"
                    };

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["Success"] = service.Price > 0
                    ? "Đã gửi yêu cầu dịch vụ và tạo hóa đơn. Vui lòng thanh toán để admin duyệt/kích hoạt dịch vụ."
                    : "Đã gửi yêu cầu dịch vụ. Vui lòng chờ admin xử lý.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Không thể đăng ký dịch vụ: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            var resident = await _context.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
            if (resident == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ sinh viên.";
                return RedirectToAction(nameof(Index));
            }

            var history = await _context.ServiceBookings
                .Include(s => s.Service)
                .Where(s => s.ResidentProfileId == resident.Id)
                .OrderByDescending(s => s.BookingDate)
                .ToListAsync();

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelService(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var resident = await _context.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
            if (resident == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ sinh viên.";
                return RedirectToAction(nameof(Index));
            }

            var booking = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .FirstOrDefaultAsync(sb => sb.Id == bookingId && sb.ResidentProfileId == resident.Id);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ để hủy.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.Status == "Approved" || booking.Status == "Active" || booking.Status == "Done")
            {
                TempData["Error"] = "Dịch vụ đã được duyệt/kích hoạt nên không thể hủy trực tiếp. Vui lòng gửi biểu mẫu yêu cầu hỗ trợ.";
                return RedirectToAction(nameof(Index));
            }

            var unpaidInvoice = await _context.Invoices.FirstOrDefaultAsync(i =>
                i.ResidentProfileId == resident.Id
                && i.ServiceBookingId == booking.Id
                && i.Status == "Unpaid");

            if (unpaidInvoice != null)
            {
                unpaidInvoice.Status = "Cancelled";
                _context.Invoices.Update(unpaidInvoice);
            }

            booking.Status = "Cancelled";
            _context.ServiceBookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy yêu cầu dịch vụ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
