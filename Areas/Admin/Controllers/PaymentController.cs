using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? keyword)
        {
            var query = _context.Payments
                .Include(x => x.Invoice)
                    .ThenInclude(i => i.ResidentProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.PaymentMethod.Contains(keyword) ||
                    x.Status.Contains(keyword) ||
                    (x.TransferCode != null && x.TransferCode.Contains(keyword)) ||
                    (x.Invoice != null && x.Invoice.InvoiceCode.Contains(keyword)) ||
                    (x.Invoice != null && x.Invoice.ResidentProfile != null && x.Invoice.ResidentProfile.FullName.Contains(keyword)));
            }

            ViewBag.Keyword = keyword;
            var data = query.OrderByDescending(x => x.PaidAt).ToList();
            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Invoices = new SelectList(_context.Invoices.ToList(), "Id", "InvoiceCode");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Payment model)
        {
            if (model.InvoiceId > 0)
            {
                var invoice = _context.Invoices.FirstOrDefault(x => x.Id == model.InvoiceId);
                if (invoice != null)
                {
                    model.Amount = invoice.TotalAmount;

                    if (model.Status == "Success")
                    {
                        invoice.Status = "Paid";
                        _context.Invoices.Update(invoice);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Payments.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Invoices = new SelectList(_context.Invoices.ToList(), "Id", "InvoiceCode", model.InvoiceId);
            return View(model);
        }

        // GET: Admin/Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.ResidentProfile)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // GET: Admin/Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            return View(payment);
        }

        // POST: Admin/Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        // GET: Admin/Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // POST: Admin/Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}