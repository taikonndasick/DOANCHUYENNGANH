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
    public class FormController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };

        public FormController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Templates));
        }

        public async Task<IActionResult> Templates(string? keyword = null, string? type = null)
        {
            var query = _context.FormTemplates.Where(x => x.IsActive);

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

            var templates = await query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .ToListAsync();

            if (!templates.Any() && string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(type))
            {
                templates = GetDefaultTemplates();
            }

            ViewBag.Keyword = keyword;
            ViewBag.Type = string.IsNullOrWhiteSpace(type) ? "Tất cả" : type;
            ViewBag.Types = GetCoreFormTypes();
            return View(templates);
        }

        public async Task<IActionResult> Submitted()
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null) return View(new List<StudentRequestForm>());

            var data = await _context.StudentRequestForms
                .Where(x => x.ResidentProfileId == resident.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? type = null, int? templateId = null)
        {
            FormTemplate? template = null;
            if (templateId.HasValue)
            {
                template = await _context.FormTemplates
                    .FirstOrDefaultAsync(x => x.Id == templateId.Value && x.IsActive);
            }

            var resident = await GetCurrentResidentAsync();
            var formType = template?.Type ?? (string.IsNullOrWhiteSpace(type) ? "Khác" : type.Trim());
            var title = template?.Name ?? GetSuggestedTitle(formType);

            ViewBag.Template = template;
            ViewBag.FormType = formType;
            ViewBag.SuggestedTitle = title;
            ViewBag.DefaultContent = GetDefaultContent(formType, title, resident);
            ViewBag.FormTypes = GetCoreFormTypes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string formType, string title, string content, IFormFile? attachment)
        {
            var resident = await GetCurrentResidentAsync();
            if (resident == null)
            {
                TempData["Error"] = "Tài khoản chưa có hồ sơ sinh viên chính thức nên chưa thể gửi biểu mẫu.";
                return RedirectToAction(nameof(Submitted));
            }

            if (string.IsNullOrWhiteSpace(formType)) ModelState.AddModelError(nameof(formType), "Vui lòng chọn loại biểu mẫu.");
            if (string.IsNullOrWhiteSpace(title)) ModelState.AddModelError(nameof(title), "Vui lòng nhập tiêu đề.");
            if (string.IsNullOrWhiteSpace(content)) ModelState.AddModelError(nameof(content), "Vui lòng nhập nội dung.");
            if (!ModelState.IsValid)
            {
                ViewBag.FormType = formType;
                ViewBag.SuggestedTitle = string.IsNullOrWhiteSpace(title) ? GetSuggestedTitle(formType) : title;
                ViewBag.DefaultContent = content;
                ViewBag.FormTypes = GetCoreFormTypes();
                return View();
            }

            string? attachmentPath = null;
            string? originalFileName = null;
            if (attachment != null && attachment.Length > 0)
            {
                var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(attachment), "Chỉ chấp nhận file .jpg, .jpeg, .png, .pdf, .doc hoặc .docx.");
                    ViewBag.FormType = formType;
                    ViewBag.SuggestedTitle = title;
                    ViewBag.DefaultContent = content;
                    ViewBag.FormTypes = GetCoreFormTypes();
                    return View();
                }

                attachmentPath = await SaveFileAsync(attachment, resident.Id, "forms");
                originalFileName = attachment.FileName;
            }

            _context.StudentRequestForms.Add(new StudentRequestForm
            {
                ResidentProfileId = resident.Id,
                FormType = formType.Trim(),
                Title = title.Trim(),
                Content = content.Trim(),
                AttachmentPath = attachmentPath,
                OriginalFileName = originalFileName,
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi biểu mẫu. Vui lòng chờ admin xử lý.";
            return RedirectToAction(nameof(Submitted));
        }

        private static List<FormTemplate> GetDefaultTemplates()
        {
            return new List<FormTemplate>
            {
                new() { Name = "Đơn đăng ký ở ký túc xá", Type = "Đăng ký ở", Description = "Mẫu tham khảo cho sinh viên đăng ký lưu trú ký túc xá.", Icon = "bi-house-add", IsActive = true },
                new() { Name = "Đơn xin chuyển phòng", Type = "Chuyển phòng", Description = "Dành cho sinh viên đang lưu trú có nhu cầu chuyển sang phòng khác.", Icon = "bi-arrow-left-right", IsActive = true },
                new() { Name = "Đơn xin trả phòng / chuyển ra trước hạn", Type = "Trả phòng", Description = "Dùng khi sinh viên muốn kết thúc lưu trú và trả phòng ký túc xá.", Icon = "bi-box-arrow-right", IsActive = true },
                new() { Name = "Đơn đề nghị miễn, giảm lệ phí phòng ở", Type = "Miễn giảm phí", Description = "Dành cho sinh viên thuộc diện chính sách, khó khăn hoặc có minh chứng hợp lệ.", Icon = "bi-cash-coin", IsActive = true },
                new() { Name = "Phiếu phản ánh sửa chữa", Type = "Sửa chữa", Description = "Gửi phản ánh khi phòng ở, thiết bị hoặc khu vực sinh hoạt có hư hỏng.", Icon = "bi-tools", IsActive = true },
                new() { Name = "Tờ khai thay đổi thông tin cư trú", Type = "Thay đổi cư trú", Description = "Dùng khi sinh viên thay đổi thông tin cư trú, liên hệ hoặc thông tin hộ gia đình.", Icon = "bi-person-lines-fill", IsActive = true },
                new() { Name = "Khai báo bảo hiểm y tế", Type = "BHYT", Description = "Sinh viên cập nhật thông tin thẻ BHYT để phục vụ quản lý và hỗ trợ y tế.", Icon = "bi-shield-plus", IsActive = true },
                new() { Name = "Đơn đăng ký cấp lại thẻ KTX", Type = "Cấp lại thẻ", Description = "Dùng khi sinh viên mất, hỏng hoặc cần cấp lại thẻ nội trú.", Icon = "bi-credit-card-2-front", IsActive = true },
                new() { Name = "Đơn xin về trễ", Type = "Về trễ", Description = "Dành cho sinh viên cần xin phép về sau giờ quy định của ký túc xá.", Icon = "bi-clock-history", IsActive = true },
                new() { Name = "Bản cam kết thực hiện nội quy phòng ở", Type = "Cam kết", Description = "Mẫu cam kết thực hiện nội quy sinh hoạt, vệ sinh và bảo quản tài sản phòng ở.", Icon = "bi-clipboard-check", IsActive = true },
                new() { Name = "Đơn đề nghị xác nhận lưu trú", Type = "Xác nhận lưu trú", Description = "Dùng khi sinh viên cần xác nhận đang lưu trú tại ký túc xá.", Icon = "bi-file-earmark-check", IsActive = true },
                new() { Name = "Đơn kiến nghị / phản ánh", Type = "Kiến nghị", Description = "Gửi góp ý, phản ánh hoặc đề xuất đến ban quản lý ký túc xá.", Icon = "bi-chat-left-text", IsActive = true }
            };
        }

        private static List<string> GetCoreFormTypes()
        {
            return new List<string>
            {
                "Tất cả",
                "Đăng ký ở",
                "Chuyển phòng",
                "Trả phòng",
                "Miễn giảm phí",
                "Sửa chữa",
                "Thay đổi cư trú",
                "BHYT",
                "Cấp lại thẻ",
                "Về trễ",
                "Cam kết",
                "Xác nhận lưu trú",
                "Kiến nghị",
                "Khác"
            };
        }

        private static string GetSuggestedTitle(string? formType)
        {
            return formType switch
            {
                "Đăng ký ở" => "Đơn đăng ký ở ký túc xá",
                "Chuyển phòng" => "Đơn xin chuyển phòng ký túc xá",
                "Trả phòng" => "Đơn xin trả phòng / chuyển ra khỏi ký túc xá",
                "Miễn giảm phí" => "Đơn đề nghị miễn, giảm lệ phí phòng ở",
                "Sửa chữa" => "Phiếu phản ánh sửa chữa phòng ở",
                "Thay đổi cư trú" => "Tờ khai thay đổi thông tin cư trú",
                "BHYT" => "Khai báo bảo hiểm y tế",
                "Cấp lại thẻ" => "Đơn đăng ký cấp lại thẻ KTX",
                "Về trễ" => "Đơn xin về trễ",
                "Cam kết" => "Bản cam kết thực hiện nội quy phòng ở",
                "Xác nhận lưu trú" => "Đơn đề nghị xác nhận lưu trú",
                "Kiến nghị" => "Đơn kiến nghị / phản ánh",
                _ => "Biểu mẫu sinh viên"
            };
        }

        private static string GetDefaultContent(string formType, string title, ResidentProfile? resident)
        {
            var fullName = resident?.FullName ?? "";
            var studentCode = resident?.StudentCode ?? "";
            var faculty = resident?.Faculty ?? "";
            var email = resident?.Email ?? "";
            var phone = resident?.PhoneNumber ?? "";
            var gender = resident?.Gender ?? "";
            var dob = resident?.DateOfBirth.HasValue == true
                ? resident.DateOfBirth.Value.ToString("dd/MM/yyyy")
                  : "";
            var room = resident?.Apartment?.ApartmentCode ?? "";
            var building = resident?.Apartment?.Building?.Name ?? resident?.Apartment?.Building?.Code ?? "";

            var header = "Kính gửi: Ban quản lý ký túc xá KTX HUTECH\n\n" +
                         $"Em tên là: {fullName}\n" +
                         $"Mã số sinh viên: {studentCode}\n" +
                         $"Ngày sinh: {dob}\n" +
                         $"Giới tính: {gender}\n" +
                         $"Khoa/Ngành: {faculty}\n" +
                         $"Email: {email}\n" +
                         $"Số điện thoại: {phone}\n" +
                         $"Hiện đang ở: Tòa {building} - Phòng {room}\n\n";

            return formType switch
            {
                "BHYT" => header +
                    "NỘI DUNG KHAI BÁO BẢO HIỂM Y TẾ\n" +
                    "1. Mã số thẻ BHYT: ........................................................\n" +
                    "2. Nơi đăng ký khám chữa bệnh ban đầu: ..................................\n" +
                    "3. Thời hạn sử dụng thẻ: Từ ....../....../...... đến ....../....../......\n" +
                    "4. Đối tượng tham gia BHYT: ................................................\n" +
                    "5. Tình trạng thẻ hiện tại: Còn hạn / Hết hạn / Đang chờ cấp mới\n" +
                    "6. Ghi chú nếu có: ..........................................................\n\n" +
                    "Em cam kết thông tin khai báo trên là đúng sự thật. Em đồng ý chịu trách nhiệm nếu thông tin BHYT không chính xác hoặc giấy tờ đính kèm không hợp lệ.\n\n" +
                    "File đính kèm đề nghị: ảnh thẻ BHYT hoặc file PDF minh chứng BHYT.",

                "Chuyển phòng" => header +
                    "NỘI DUNG XIN CHUYỂN PHÒNG\n" +
                    "Phòng mong muốn chuyển đến: ..............................................\n" +
                    "Loại phòng mong muốn: ...................................................\n" +
                    "Lý do xin chuyển phòng: ..................................................\n\n" +
                    "Em cam kết thực hiện theo sự sắp xếp của Ban quản lý và hoàn tất các nghĩa vụ tài chính nếu có phát sinh.",

                "Trả phòng" => header +
                    "NỘI DUNG XIN TRẢ PHÒNG / CHUYỂN RA KHỎI KÝ TÚC XÁ\n" +
                    "Ngày dự kiến trả phòng: ....../....../......\n" +
                    "Lý do trả phòng: ........................................................\n" +
                    "Tình trạng phòng khi bàn giao: ............................................\n\n" +
                    "Em cam kết thanh toán đầy đủ các khoản phí còn nợ, vệ sinh phòng ở sạch sẽ và bàn giao tài sản đúng quy định.",

                "Miễn giảm phí" => header +
                    "NỘI DUNG ĐỀ NGHỊ MIỄN, GIẢM LỆ PHÍ PHÒNG Ở\n" +
                    "Đối tượng cụ thể: ........................................................\n" +
                    "Lý do đề nghị miễn/giảm: .................................................\n" +
                    "Các giấy tờ nộp kèm: .....................................................\n\n" +
                    "Em kính mong Ban quản lý xem xét hỗ trợ miễn/giảm lệ phí phòng ở theo quy định. Em cam kết các thông tin và giấy tờ minh chứng là đúng sự thật.",

                "Sửa chữa" => header +
                    "NỘI DUNG PHẢN ÁNH SỬA CHỮA\n" +
                    "Khu vực/thiết bị hư hỏng: ................................................\n" +
                    "Mức độ: Nhẹ / Trung bình / Khẩn cấp\n" +
                    "Mô tả chi tiết: ..........................................................\n" +
                    "Thời điểm phát hiện: ....../....../......\n\n" +
                    "Kính mong Ban quản lý kiểm tra và hỗ trợ xử lý trong thời gian sớm nhất.",

                "Thay đổi cư trú" => header +
                    "NỘI DUNG THAY ĐỔI THÔNG TIN CƯ TRÚ\n" +
                    "Thông tin cần thay đổi: ..................................................\n" +
                    "Nội dung cũ: ............................................................\n" +
                    "Nội dung mới: ...........................................................\n" +
                    "Lý do thay đổi: ..........................................................\n\n" +
                    "Em cam kết thông tin thay đổi là chính xác và chịu trách nhiệm về nội dung đã khai báo.",

                "Cấp lại thẻ" => header +
                    "NỘI DUNG ĐĂNG KÝ CẤP LẠI THẺ KTX\n" +
                    "Lý do cấp lại: Mất thẻ / Hỏng thẻ / Cập nhật thông tin / Khác\n" +
                    "Mô tả lý do: ............................................................\n\n" +
                    "Em cam kết sử dụng thẻ đúng quy định sau khi được cấp lại.",

                "Về trễ" => header +
                    "NỘI DUNG XIN VỀ TRỄ\n" +
                    "Ngày xin về trễ: ....../....../......\n" +
                    "Thời gian dự kiến về KTX: ........ giờ ........ phút\n" +
                    "Lý do về trễ: ...........................................................\n" +
                    "Địa chỉ/địa điểm đi: .....................................................\n\n" +
                    "Em cam kết chấp hành quy định an ninh, trật tự và chịu trách nhiệm về việc đi lại của bản thân.",

                "Cam kết" => header +
                    "NỘI DUNG CAM KẾT\n" +
                    "Em cam kết thực hiện nghiêm túc các quy định của ký túc xá, bao gồm:\n" +
                    "1. Giữ gìn vệ sinh phòng ở và khu vực sinh hoạt chung.\n" +
                    "2. Không gây mất trật tự, không tự ý thay đổi kết cấu hoặc di chuyển tài sản chung.\n" +
                    "3. Bảo quản tài sản được bàn giao và sử dụng điện, nước đúng quy định.\n" +
                    "4. Chấp hành nội quy về giờ giấc, an ninh và phòng cháy chữa cháy.\n\n" +
                    "Nếu vi phạm, em xin chịu trách nhiệm theo quy định của Ban quản lý ký túc xá.",

                "Xác nhận lưu trú" => header +
                    "NỘI DUNG ĐỀ NGHỊ XÁC NHẬN LƯU TRÚ\n" +
                    "Mục đích xin xác nhận: ..................................................\n" +
                    "Đơn vị/cơ quan cần nộp nếu có: ..........................................\n\n" +
                    "Kính mong Ban quản lý xác nhận thông tin lưu trú để em hoàn thiện thủ tục cần thiết.",

                "Kiến nghị" => header +
                    "NỘI DUNG KIẾN NGHỊ / PHẢN ÁNH\n" +
                    "Vấn đề cần phản ánh: ....................................................\n" +
                    "Thời gian xảy ra: ........................................................\n" +
                    "Địa điểm: ................................................................\n" +
                    "Nội dung chi tiết: .......................................................\n" +
                    "Đề xuất hướng xử lý: .....................................................",

                _ => header +
                    $"NỘI DUNG {title.ToUpper()}\n" +
                    "Nội dung đề nghị: .......................................................\n" +
                    "Lý do: ..................................................................\n\n" +
                    "Em cam kết các thông tin trên là đúng sự thật và chịu trách nhiệm về nội dung đã gửi."
            };
        }

        private async Task<string> SaveFileAsync(IFormFile file, int residentId, string folderName)
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", folderName, residentId.ToString());
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(folder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folderName}/{residentId}/{fileName}";
        }

        private async Task<ResidentProfile?> GetCurrentResidentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.ResidentProfiles
                .Include(x => x.Apartment)
                    .ThenInclude(a => a!.Building)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
