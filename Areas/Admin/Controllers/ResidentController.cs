using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;
using System.Text.RegularExpressions;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ResidentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResidentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? status, string? gender, int? buildingId, string? roomState, int page = 1, int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var query = _context.ResidentProfiles
                .Include(r => r.Apartment)
                    .ThenInclude(a => a!.Building)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(r =>
                    r.FullName.Contains(keyword) ||
                    (r.StudentCode != null && r.StudentCode.Contains(keyword)) ||
                    (r.NationalId != null && r.NationalId.Contains(keyword)) ||
                    (r.PhoneNumber != null && r.PhoneNumber.Contains(keyword)) ||
                    (r.Email != null && r.Email.Contains(keyword)) ||
                    (r.ClassName != null && r.ClassName.Contains(keyword)) ||
                    (r.Faculty != null && r.Faculty.Contains(keyword)) ||
                    (r.Apartment != null && r.Apartment.ApartmentCode.Contains(keyword)) ||
                    (r.Apartment != null && r.Apartment.Building != null && r.Apartment.Building.Code.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(gender) && gender != "Tất cả")
            {
                query = query.Where(r => r.Gender == gender);
            }

            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(r => r.Apartment != null && r.Apartment.BuildingId == buildingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(roomState) && roomState != "Tất cả")
            {
                if (roomState == "Đã phân phòng")
                {
                    query = query.Where(r => r.ApartmentId != null);
                }
                else if (roomState == "Chưa phân phòng")
                {
                    query = query.Where(r => r.ApartmentId == null);
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var data = await query
                .OrderBy(r => r.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PortraitPhotoPaths = await LoadPortraitPhotoPathsAsync(data);
            await LoadResidentFilters(buildingId);
            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.Gender = gender;
            ViewBag.BuildingId = buildingId;
            ViewBag.RoomState = roomState;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(data);
        }

        public IActionResult Create()
        {
            LoadApartmentSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ResidentProfile model)
        {
            NormalizeResident(model);
            ValidateResidentProfile(model);

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.Status)) model.Status = "Chưa ở";
                model.CreatedAt = DateTime.Now;
                _context.ResidentProfiles.Add(model);
                _context.SaveChanges();
                SyncRoomOccupancy(model.ApartmentId);
                return RedirectToAction(nameof(Index));
            }
            LoadApartmentSelectList(model.ApartmentId);
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var data = _context.ResidentProfiles
                .Include(r => r.User)
                .Include(r => r.Apartment)
                .FirstOrDefault(r => r.Id == id);
            if (data == null) return NotFound();

            LoadApartmentSelectList(data.ApartmentId);

            var otherLinkedUserIds = _context.ResidentProfiles
                .Where(r => r.UserId != null && r.Id != id)
                .Select(r => r.UserId)
                .ToList();

            var availableUsers = _context.Users
                .Where(u => !otherLinkedUserIds.Contains(u.Id))
                .Select(u => new { Value = u.Id, Text = u.Username })
                .ToList();

            ViewBag.UserId = new SelectList(availableUsers, "Value", "Text", data.UserId);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ResidentProfile model)
        {
            NormalizeResident(model);
            var oldApartmentId = _context.ResidentProfiles
                .AsNoTracking()
                .Where(x => x.Id == model.Id)
                .Select(x => x.ApartmentId)
                .FirstOrDefault();

            ValidateResidentProfile(model, model.Id);

            if (ModelState.IsValid)
            {
                _context.ResidentProfiles.Update(model);
                _context.SaveChanges();
                SyncRoomOccupancy(oldApartmentId, model.ApartmentId);
                return RedirectToAction(nameof(Index));
            }
            LoadApartmentSelectList(model.ApartmentId);
            LoadUserSelectList(model.Id, model.UserId);
            return View(model);
        }

        public IActionResult Details(int id)
        {
            var data = _context.ResidentProfiles
                .Include(r => r.Apartment).ThenInclude(a => a!.Building)
                .Include(r => r.Vehicles)
                .Include(r => r.ResidentCards)
                .FirstOrDefault(r => r.Id == id);
            if (data == null) return NotFound();

            ViewBag.PortraitPhotoPath = GetPortraitPhotoPath(data);
            return View(data);
        }

        private void LoadApartmentSelectList(int? selectedApartmentId = null)
        {
            var rooms = _context.Apartments
                .Include(a => a.Building)
                .OrderBy(a => a.ApartmentCode)
                .Select(a => new
                {
                    a.Id,
                    DisplayName = a.Building != null ? $"{a.ApartmentCode} - {a.Building.Name}" : a.ApartmentCode
                })
                .ToList();
            ViewBag.ApartmentId = new SelectList(rooms, "Id", "DisplayName", selectedApartmentId);
        }

        private async Task LoadResidentFilters(int? selectedBuildingId = null)
        {
            var buildings = await _context.Buildings.OrderBy(b => b.Code).ToListAsync();
            ViewBag.Buildings = new SelectList(buildings, "Id", "Name", selectedBuildingId);
        }

        private void LoadUserSelectList(int id, string? selectedUserId)
        {
            var otherLinkedUserIds = _context.ResidentProfiles
                .Where(r => r.UserId != null && r.Id != id)
                .Select(r => r.UserId)
                .ToList();

            var availableUsers = _context.Users
                .Where(u => !otherLinkedUserIds.Contains(u.Id))
                .Select(u => new { Value = u.Id, Text = u.Username })
                .ToList();

            ViewBag.UserId = new SelectList(availableUsers, "Value", "Text", selectedUserId);
        }

        private void ValidateResidentProfile(ResidentProfile model, int? ignoreId = null)
        {
            if (!string.IsNullOrWhiteSpace(model.StudentCode) && !Regex.IsMatch(model.StudentCode, @"^\d{10}$"))
            {
                ModelState.AddModelError(nameof(model.StudentCode), "Mã số sinh viên phải gồm đúng 10 chữ số.");
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !Regex.IsMatch(model.PhoneNumber, @"^0\d{9}$"))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");
            }

            var duplicateQuery = _context.ResidentProfiles.AsQueryable();
            if (ignoreId.HasValue)
            {
                duplicateQuery = duplicateQuery.Where(x => x.Id != ignoreId.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.StudentCode) && duplicateQuery.Any(x => x.StudentCode == model.StudentCode))
            {
                ModelState.AddModelError(nameof(model.StudentCode), "Mã số sinh viên đã tồn tại trong danh sách sinh viên.");
            }

            if (!string.IsNullOrWhiteSpace(model.NationalId) && duplicateQuery.Any(x => x.NationalId == model.NationalId))
            {
                ModelState.AddModelError(nameof(model.NationalId), "Số căn cước đã tồn tại trong danh sách sinh viên.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && duplicateQuery.Any(x => x.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại trong danh sách sinh viên.");
            }

            if (model.ApartmentId.HasValue && IsOccupyingStatus(model.Status))
            {
                var room = _context.Apartments.FirstOrDefault(x => x.Id == model.ApartmentId.Value);
                if (room == null)
                {
                    ModelState.AddModelError(nameof(model.ApartmentId), "Phòng KTX không tồn tại.");
                }
                else if (room.Status == "Bảo trì" || room.Status == "Ngưng sử dụng")
                {
                    ModelState.AddModelError(nameof(model.ApartmentId), "Không thể gán sinh viên vào phòng đang bảo trì hoặc ngưng sử dụng.");
                }
                else
                {
                    var currentOccupancy = _context.ResidentProfiles.Count(x =>
                        x.ApartmentId == room.Id &&
                        (!ignoreId.HasValue || x.Id != ignoreId.Value) &&
                        (x.Status == "Đang ở" || x.Status == "Chờ thanh toán" || x.Status == "Active"));

                    if (currentOccupancy >= room.MaxOccupancy)
                    {
                        ModelState.AddModelError(nameof(model.ApartmentId), "Phòng đã đủ số lượng sinh viên tối đa.");
                    }
                }
            }
        }

        private void NormalizeResident(ResidentProfile model)
        {
            model.FullName = model.FullName.Trim();
            model.StudentCode = model.StudentCode?.Trim();
            model.NationalId = model.NationalId?.Trim();
            model.PhoneNumber = model.PhoneNumber?.Trim();
            model.Email = model.Email?.Trim();
            model.Address = model.Address?.Trim();
            model.ClassName = model.ClassName?.Trim();
            model.Faculty = model.Faculty?.Trim();
        }

        private void SyncRoomOccupancy(params int?[] roomIds)
        {
            var ids = roomIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            if (!ids.Any()) return;

            var rooms = _context.Apartments.Where(x => ids.Contains(x.Id)).ToList();
            foreach (var room in rooms)
            {
                room.CurrentOccupancy = _context.ResidentProfiles.Count(x =>
                    x.ApartmentId == room.Id &&
                    (x.Status == "Đang ở" || x.Status == "Chờ thanh toán" || x.Status == "Active"));

                if (room.Status != "Bảo trì" && room.Status != "Ngưng sử dụng")
                {
                    room.Status = room.CurrentOccupancy >= room.MaxOccupancy ? "Đã đầy" : "Còn chỗ";
                }
            }

            _context.SaveChanges();
        }

        private async Task<Dictionary<string, string>> LoadPortraitPhotoPathsAsync(IEnumerable<ResidentProfile> residents)
        {
            var userIds = residents
                .Select(x => x.UserId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!userIds.Any()) return new Dictionary<string, string>();

            var registrations = await _context.StudentRegistrations
                .Include(x => x.Documents)
                .Where(x => userIds.Contains(x.UserId))
                .ToListAsync();

            return registrations
                .Select(x => new
                {
                    x.UserId,
                    PortraitPath = x.Documents.FirstOrDefault(d => d.DocumentType == "PortraitPhoto")?.FilePath
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.PortraitPath))
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.First().PortraitPath!);
        }

        private string? GetPortraitPhotoPath(ResidentProfile resident)
        {
            if (string.IsNullOrWhiteSpace(resident.UserId) && string.IsNullOrWhiteSpace(resident.StudentCode))
            {
                return null;
            }

            var registration = _context.StudentRegistrations
                .Include(x => x.Documents)
                .Where(x => (!string.IsNullOrWhiteSpace(resident.UserId) && x.UserId == resident.UserId)
                    || (!string.IsNullOrWhiteSpace(resident.StudentCode) && x.StudentCode == resident.StudentCode))
                .OrderByDescending(x => x.SubmittedAt)
                .FirstOrDefault();

            return registration?.Documents.FirstOrDefault(x => x.DocumentType == "PortraitPhoto")?.FilePath;
        }

        private static bool IsOccupyingStatus(string? status)
        {
            return status == "Đang ở" || status == "Chờ thanh toán" || status == "Active";
        }
    }
}
