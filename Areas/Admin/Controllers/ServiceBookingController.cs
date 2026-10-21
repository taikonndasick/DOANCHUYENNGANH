using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServiceBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? status, string? type)
        {
            var query = _context.ServiceBookings
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                .Include(x => x.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
            {
                query = query.Where(x => x.Service != null && x.Service.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    (x.ResidentProfile != null && x.ResidentProfile.FullName.Contains(keyword))
                    || (x.ResidentProfile != null && x.ResidentProfile.StudentCode != null && x.ResidentProfile.StudentCode.Contains(keyword))
                    || (x.Service != null && x.Service.Name.Contains(keyword))
                    || x.Status.Contains(keyword)
                    || (x.Note != null && x.Note.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.Type = type;

            var data = await query
                .OrderByDescending(x => x.BookingDate)
                .ToListAsync();

            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName");
            ViewBag.Services = new SelectList(_context.Services.OrderBy(x => x.Name).ToList(), "Id", "Name");
            return View(new ServiceBooking { BookingDate = DateTime.Now, Status = "Pending" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceBooking model)
        {
            if (ModelState.IsValid)
            {
                model.BookingDate = model.BookingDate == default ? DateTime.Now : model.BookingDate;
                model.Status = string.IsNullOrWhiteSpace(model.Status) ? "Pending" : model.Status;
                _context.ServiceBookings.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName");
            ViewBag.Services = new SelectList(_context.Services.OrderBy(x => x.Name).ToList(), "Id", "Name");
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var data = await _context.ServiceBookings.FindAsync(id);
            if (data == null) return NotFound();

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName", data.ResidentProfileId);
            ViewBag.Services = new SelectList(_context.Services.OrderBy(x => x.Name).ToList(), "Id", "Name", data.ServiceId);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceBooking model)
        {
            if (ModelState.IsValid)
            {
                _context.ServiceBookings.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(x => x.FullName).ToList(), "Id", "FullName", model.ResidentProfileId);
            ViewBag.Services = new SelectList(_context.Services.OrderBy(x => x.Name).ToList(), "Id", "Name", model.ServiceId);
            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var serviceBooking = await _context.ServiceBookings
                .Include(s => s.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (serviceBooking == null) return NotFound();
            return View(serviceBooking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var booking = await _context.ServiceBookings
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (booking == null) return NotFound();

            if (booking.Status == "Cancelled" || booking.Status == "Rejected")
            {
                TempData["Error"] = "Yêu cầu này đã bị hủy/từ chối nên không thể duyệt.";
                return RedirectToAction(nameof(Index));
            }

            var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.ServiceBookingId == booking.Id);

            if (booking.Service != null && booking.Service.Price > 0)
            {
                if (invoice == null)
                {
                    TempData["Error"] = "Yêu cầu có phí nhưng chưa có hóa đơn. Vui lòng kiểm tra lại dữ liệu.";
                    return RedirectToAction(nameof(Index));
                }

                if (invoice.Status != "Paid" && invoice.Status != "Đã thanh toán")
                {
                    TempData["Error"] = "Sinh viên chưa thanh toán hóa đơn dịch vụ nên chưa thể duyệt/kích hoạt.";
                    return RedirectToAction(nameof(Index));
                }
            }

            booking.Status = "Approved";
            _context.ServiceBookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt/kích hoạt dịch vụ cho sinh viên.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var booking = await _context.ServiceBookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = "Rejected";

            var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.ServiceBookingId == booking.Id && x.Status == "Unpaid");
            if (invoice != null)
            {
                invoice.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã từ chối yêu cầu dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.ServiceBookings
                .Include(s => s.ResidentProfile)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.ServiceBookings.FindAsync(id);
            if (booking != null)
            {
                _context.ServiceBookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
