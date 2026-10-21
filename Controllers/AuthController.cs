using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;
using QuanLyKTX.ViewModels;
using System.Security.Claims;

namespace QuanLyKTX.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedFileExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxUploadSize = 5 * 1024 * 1024;

        public AuthController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectByRoleAsync().Result;
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang chờ admin duyệt hoặc đang bị khóa. Sinh viên chỉ đăng nhập được sau khi hồ sơ đăng ký được duyệt.");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return await RedirectByRoleAsync();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Username = model.Username.Trim();
            model.StudentCode = model.StudentCode.Trim();
            model.NationalId = model.NationalId.Trim();
            model.PhoneNumber = model.PhoneNumber.Trim();
            model.Email = model.Email.Trim();

            if (await _userManager.FindByNameAsync(model.Username) != null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng cho tài khoản khác.");
                return View(model);
            }

            var hasResident = await _context.ResidentProfiles
                .AnyAsync(x => x.StudentCode == model.StudentCode || x.NationalId == model.NationalId);

            if (hasResident)
            {
                ModelState.AddModelError(string.Empty, "Mã số sinh viên hoặc số căn cước đã tồn tại trong danh sách sinh viên đã duyệt.");
                return View(model);
            }

            var hasWaitingRegistration = await _context.StudentRegistrations
                .AnyAsync(x => (x.StudentCode == model.StudentCode || x.NationalId == model.NationalId || x.Username == model.Username)
                               && x.Status != "Từ chối");

            if (hasWaitingRegistration)
            {
                ModelState.AddModelError(string.Empty, "Sinh viên đã có hồ sơ đăng ký đang chờ duyệt hoặc đã được duyệt.");
                return View(model);
            }

            if (!ValidateFile(model.CitizenIdFile, nameof(model.CitizenIdFile)) ||
                !ValidateFile(model.HealthInsuranceFile, nameof(model.HealthInsuranceFile)) ||
                !ValidateFile(model.AdmissionProofFile, nameof(model.AdmissionProofFile)) ||
                !ValidateFile(model.PortraitPhotoFile, nameof(model.PortraitPhotoFile)))
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                LockoutEnabled = true,
                LockoutEnd = DateTimeOffset.MaxValue
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            try
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _userManager.AddClaimAsync(user, new Claim("FullName", model.FullName.Trim()));

                var registration = new StudentRegistration
                {
                    UserId = user.Id,
                    Username = model.Username,
                    StudentCode = model.StudentCode,
                    FullName = model.FullName.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender.Trim(),
                    NationalId = model.NationalId,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Address = model.Address.Trim(),
                    ClassName = model.ClassName.Trim(),
                    Faculty = model.Faculty.Trim(),
                    BankAccountNumber = model.BankAccountNumber.Trim(),
                    BankName = model.BankName.Trim(),
                    Status = "Chờ duyệt",
                    SubmittedAt = DateTime.Now
                };

                _context.StudentRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                await AddDocumentAsync(registration, "CitizenId", model.CitizenIdFile!);
                await AddDocumentAsync(registration, "HealthInsurance", model.HealthInsuranceFile!);
                await AddDocumentAsync(registration, "AdmissionProof", model.AdmissionProofFile!);
                await AddDocumentAsync(registration, "PortraitPhoto", model.PortraitPhotoFile!);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Gửi hồ sơ đăng ký cư trú thành công. Tài khoản sẽ được kích hoạt sau khi admin duyệt hồ sơ.";
                return RedirectToAction(nameof(Login));
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Có lỗi khi lưu hồ sơ đăng ký. Vui lòng kiểm tra lại thông tin và thử lại.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Landing", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content("Bạn không có quyền truy cập chức năng này.");
        }

        private async Task AddDocumentAsync(StudentRegistration registration, string documentType, IFormFile file)
        {
            registration.Documents.Add(new StudentRegistrationDocument
            {
                StudentRegistrationId = registration.Id,
                DocumentType = documentType,
                FilePath = await SaveFileAsync(file, registration.Id, documentType),
                OriginalFileName = file.FileName
            });
        }

        private bool ValidateFile(IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(fieldName, "Vui lòng tải lên giấy tờ bắt buộc.");
                return false;
            }

            if (file.Length > MaxUploadSize)
            {
                ModelState.AddModelError(fieldName, "Dung lượng file không được vượt quá 5MB.");
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedFileExtensions.Contains(extension))
            {
                ModelState.AddModelError(fieldName, "Chỉ chấp nhận file .jpg, .jpeg, .png hoặc .pdf.");
                return false;
            }

            return true;
        }

        private async Task<string> SaveFileAsync(IFormFile file, int registrationId, string documentType)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "student-registrations", registrationId.ToString());
            Directory.CreateDirectory(folder);

            var safeFileName = $"{documentType}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/student-registrations/{registrationId}/{safeFileName}";
        }

        private async Task<IActionResult> RedirectByRoleAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
