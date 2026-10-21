using QuanLyKTX.Data;
using QuanLyKTX.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace QuanLyKTX.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InvoiceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return Challenge();

            var invoices = await _context.Invoices
                .Where(i => i.ResidentProfileId == resident.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return Challenge();

            var invoice = await _context.Invoices
                .Include(i => i.ResidentProfile)
                    .ThenInclude(r => r!.Apartment)
                .Include(i => i.ServiceBooking)
                    .ThenInclude(sb => sb!.Service)
                .FirstOrDefaultAsync(i => i.Id == id && i.ResidentProfileId == resident.Id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(
            int invoiceId,
            string? paymentType,
            string? method,
            string? momoPhone,
            string? bankTransferCode,
            string? cardHolderName,
            string? cardNumber,
            string? cardExpiry,
            string? cardCvv)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return Challenge();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.ResidentProfileId == resident.Id);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn cần thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.Status == "Paid")
            {
                TempData["Success"] = "Hóa đơn này đã được thanh toán trước đó.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.Status == "Cancelled")
            {
                TempData["Error"] = "Không thể thanh toán hóa đơn đã hủy.";
                return RedirectToAction(nameof(Index));
            }

            var paymentInfo = BuildPaymentInfo(
                paymentType,
                method,
                momoPhone,
                bankTransferCode,
                cardHolderName,
                cardNumber,
                cardExpiry,
                cardCvv);

            if (!paymentInfo.IsValid)
            {
                TempData["Error"] = paymentInfo.Message;
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.ResidentProfileId == resident.Id);

                if (invoice == null)
                {
                    TempData["Error"] = "Không tìm thấy hóa đơn cần thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                if (invoice.Status == "Paid")
                {
                    TempData["Success"] = "Hóa đơn này đã được thanh toán trước đó.";
                    return RedirectToAction(nameof(Index));
                }

                if (invoice.Status == "Cancelled")
                {
                    TempData["Error"] = "Không thể thanh toán hóa đơn đã hủy.";
                    return RedirectToAction(nameof(Index));
                }

                var payment = new Payment
                {
                    InvoiceId = invoice.Id,
                    PaymentMethod = paymentInfo.Method,
                    Amount = invoice.TotalAmount,
                    TransferCode = paymentInfo.TransferCode,
                    PaidAt = DateTime.Now,
                    Status = "Success"
                };

                invoice.Status = "Paid";

                if (resident.Status == "Chờ thanh toán" &&
                    (invoice.Description?.Contains("Phí đăng ký nhận phòng", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    resident.Status = "Đang ở";
                }

                // Nếu hóa đơn có liên kết yêu cầu dịch vụ cụ thể thì kích hoạt đúng yêu cầu đó.
                if (invoice.ServiceBookingId.HasValue)
                {
                    var booking = await _context.ServiceBookings.FindAsync(invoice.ServiceBookingId.Value);
                    if (booking != null && booking.ResidentProfileId == resident.Id)
                    {
                        booking.Status = "Active";
                    }
                }

                _context.Payments.Add(payment);
                _context.Invoices.Update(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Thanh toán hóa đơn thành công!";
                if (!string.IsNullOrWhiteSpace(paymentInfo.Message))
                {
                    TempData["Success"] = paymentInfo.Message;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static (bool IsValid, string Method, string TransferCode, string Message) BuildPaymentInfo(
            string? paymentType,
            string? method,
            string? momoPhone,
            string? bankTransferCode,
            string? cardHolderName,
            string? cardNumber,
            string? cardExpiry,
            string? cardCvv)
        {
            paymentType = string.IsNullOrWhiteSpace(paymentType) ? method : paymentType;
            paymentType = paymentType?.Trim();

            return paymentType switch
            {
                "Momo" => BuildMomoPayment(momoPhone),
                "BankTransfer" => BuildBankTransferPayment(bankTransferCode),
                "Card" => BuildCardPayment(cardHolderName, cardNumber, cardExpiry, cardCvv),
                _ => (false, "", "", "Vui lòng chọn MoMo, chuyển khoản ngân hàng hoặc thẻ ngân hàng.")
            };
        }

        private static (bool IsValid, string Method, string TransferCode, string Message) BuildMomoPayment(string? momoPhone)
        {
            momoPhone = momoPhone?.Trim();
            if (string.IsNullOrWhiteSpace(momoPhone) || !Regex.IsMatch(momoPhone, @"^0\d{9}$"))
            {
                return (false, "", "", "Số điện thoại MoMo phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");
            }

            return (true, "MoMo", "MOMO" + DateTime.Now.Ticks.ToString()[^8..], "Đã mô phỏng thanh toán qua MoMo thành công.");
        }

        private static (bool IsValid, string Method, string TransferCode, string Message) BuildBankTransferPayment(string? bankTransferCode)
        {
            bankTransferCode = bankTransferCode?.Trim();
            var transferCode = string.IsNullOrWhiteSpace(bankTransferCode)
                ? "BANK" + DateTime.Now.Ticks.ToString()[^8..]
                : bankTransferCode;

            return (true, "Chuyển khoản ngân hàng", transferCode, "Đã mô phỏng ghi nhận chuyển khoản ngân hàng thành công.");
        }

        private static (bool IsValid, string Method, string TransferCode, string Message) BuildCardPayment(
            string? cardHolderName,
            string? cardNumber,
            string? cardExpiry,
            string? cardCvv)
        {
            cardHolderName = cardHolderName?.Trim();
            var digits = Regex.Replace(cardNumber ?? "", @"\D", "");
            cardExpiry = cardExpiry?.Trim();
            cardCvv = cardCvv?.Trim();

            if (string.IsNullOrWhiteSpace(cardHolderName))
            {
                return (false, "", "", "Vui lòng nhập tên chủ thẻ.");
            }

            if (!Regex.IsMatch(digits, @"^\d{16}$"))
            {
                return (false, "", "", "Số thẻ ngân hàng phải gồm đúng 16 chữ số.");
            }

            if (string.IsNullOrWhiteSpace(cardExpiry) || !Regex.IsMatch(cardExpiry, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            {
                return (false, "", "", "Ngày hết hạn thẻ phải có định dạng MM/YY.");
            }

            if (string.IsNullOrWhiteSpace(cardCvv) || !Regex.IsMatch(cardCvv, @"^\d{3}$"))
            {
                return (false, "", "", "Mã CVV phải gồm đúng 3 chữ số.");
            }

            return (true, "Thẻ ngân hàng", "CARD****" + digits[^4..], "Đã mô phỏng thanh toán bằng thẻ ngân hàng thành công.");
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.ResidentProfiles
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.UserId == userId);
        }
    }
}
