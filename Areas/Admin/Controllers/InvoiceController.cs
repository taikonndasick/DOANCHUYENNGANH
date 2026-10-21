using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? keyword)
        {
            var query = _context.Invoices
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.User)
                .Include(x => x.ServiceBooking)
                    .ThenInclude(b => b!.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.InvoiceCode.Contains(keyword) ||
                    x.Status.Contains(keyword) ||
                    (x.Description != null && x.Description.Contains(keyword)) ||
                    (x.ResidentProfile != null && x.ResidentProfile.FullName.Contains(keyword)) ||
                    (x.ResidentProfile != null && x.ResidentProfile.StudentCode != null && x.ResidentProfile.StudentCode.Contains(keyword)) ||
                    (x.ResidentProfile != null && x.ResidentProfile.User != null && x.ResidentProfile.User.UserName != null && x.ResidentProfile.User.UserName.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            var data = query.OrderByDescending(x => x.CreatedAt).ToList();
            return View(data);
        }

        public IActionResult Create()
        {
            PopulateCreateViewData();

            return View(new Invoice
            {
                InvoiceCode = "INV" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                DueDate = DateTime.Now.AddDays(7),
                Status = "Unpaid"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice model, int? serviceId, int quantity = 1, decimal? unitPrice = null, string? serviceNote = null)
        {
            if (string.IsNullOrWhiteSpace(model.InvoiceCode))
            {
                model.InvoiceCode = "INV" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            if (model.DueDate == null)
            {
                model.DueDate = DateTime.Now.AddDays(7);
            }

            if (string.IsNullOrWhiteSpace(model.Status))
            {
                model.Status = "Unpaid";
            }

            if (quantity <= 0) quantity = 1;

            // Xóa lỗi validation cũ do các giá trị được hệ thống tự sinh/tự tính lại.
            ModelState.Remove(nameof(model.InvoiceCode));
            ModelState.Remove(nameof(model.TotalAmount));
            ModelState.Remove(nameof(model.Description));
            ModelState.Remove(nameof(model.Status));

            var resident = await _context.ResidentProfiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == model.ResidentProfileId);

            if (resident == null)
            {
                ModelState.AddModelError(nameof(model.ResidentProfileId), "Vui lòng tìm và chọn sinh viên nhận hóa đơn.");
            }

            Service? service = null;
            if (serviceId.HasValue && serviceId.Value > 0)
            {
                service = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceId.Value);
            }

            if (service != null)
            {
                var price = unitPrice.HasValue && unitPrice.Value > 0 ? unitPrice.Value : service.Price;
                model.TotalAmount = price * quantity;

                var description = $"Hóa đơn dịch vụ: {service.Name} - SL {quantity} x {price:N0} đ";
                if (!string.IsNullOrWhiteSpace(serviceNote))
                {
                    description += $". Ghi chú: {serviceNote.Trim()}";
                }

                model.Description = description;
                model.ServiceBookingId = null; // Hóa đơn do admin lập trực tiếp, không bắt buộc liên kết booking.
            }
            else
            {
                if (model.TotalAmount <= 0)
                {
                    ModelState.AddModelError(nameof(model.TotalAmount), "Vui lòng nhập tổng tiền hoặc chọn dịch vụ cần lập hóa đơn.");
                }

                if (string.IsNullOrWhiteSpace(model.Description))
                {
                    model.Description = "Hóa đơn dịch vụ ký túc xá";
                }
            }

            model.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Invoices.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Phát hành hóa đơn dịch vụ cho sinh viên thành công!";
                return RedirectToAction(nameof(Index));
            }

            PopulateCreateViewData(serviceId);
            ViewBag.SelectedServiceId = serviceId;
            ViewBag.Quantity = quantity;
            ViewBag.UnitPrice = unitPrice;
            ViewBag.ServiceNote = serviceNote;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchStudent(string? username, string? studentCode, string? fullName)
        {
            username = username?.Trim();
            studentCode = studentCode?.Trim();
            fullName = fullName?.Trim();

            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(studentCode) && string.IsNullOrWhiteSpace(fullName))
            {
                return Json(Array.Empty<object>());
            }

            var query = _context.ResidentProfiles
                .Include(r => r.User)
                .Include(r => r.Apartment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(r => r.User != null && r.User.UserName != null && r.User.UserName.Contains(username));
            }

            if (!string.IsNullOrWhiteSpace(studentCode))
            {
                query = query.Where(r => r.StudentCode != null && r.StudentCode.Contains(studentCode));
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                query = query.Where(r => r.FullName.Contains(fullName));
            }

            var data = await query
                .OrderBy(r => r.FullName)
                .Take(10)
                .Select(r => new
                {
                    id = r.Id,
                    username = r.User != null ? r.User.UserName : "",
                    studentCode = r.StudentCode ?? "",
                    fullName = r.FullName,
                    email = r.Email ?? "",
                    phoneNumber = r.PhoneNumber ?? "",
                    room = r.Apartment != null ? r.Apartment.ApartmentCode : "Chưa phân phòng",
                    status = r.Status
                })
                .ToListAsync();

            return Json(data);
        }

        public IActionResult Details(int id)
        {
            var data = _context.Invoices
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.User)
                .Include(x => x.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                .Include(x => x.ServiceBooking)
                    .ThenInclude(b => b!.Service)
                .FirstOrDefault(x => x.Id == id);

            if (data == null) return NotFound();

            return View(data);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.ToList(), "Id", "FullName", invoice.ResidentProfileId);
            ViewBag.Bookings = new SelectList(_context.ServiceBookings.ToList(), "Id", "Id", invoice.ServiceBookingId);

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Invoice model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ServiceBookingId.HasValue)
                    {
                        var booking = await _context.ServiceBookings
                            .Include(x => x.Service)
                            .FirstOrDefaultAsync(x => x.Id == model.ServiceBookingId.Value);

                        if (booking != null)
                        {
                            model.TotalAmount = booking.Service?.Price ?? 0;
                            model.ResidentProfileId = booking.ResidentProfileId;
                        }
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật hóa đơn thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Invoices.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.ToList(), "Id", "FullName", model.ResidentProfileId);
            ViewBag.Bookings = new SelectList(_context.ServiceBookings.ToList(), "Id", "Id", model.ServiceBookingId);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.ResidentProfile)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                var hasPayments = await _context.Payments.AnyAsync(p => p.InvoiceId == id);
                if (hasPayments)
                {
                    TempData["Error"] = "Không thể xóa hóa đơn đã có dữ liệu thanh toán!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa hóa đơn thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateCreateViewData(int? selectedServiceId = null)
        {
            ViewBag.Services = _context.Services
                .OrderBy(s => s.Name)
                .ToList();

            ViewBag.Residents = new SelectList(_context.ResidentProfiles.OrderBy(r => r.FullName).ToList(), "Id", "FullName");
            ViewBag.Bookings = new SelectList(_context.ServiceBookings.ToList(), "Id", "Id");
            ViewBag.SelectedServiceId = selectedServiceId;
        }
    }
}
