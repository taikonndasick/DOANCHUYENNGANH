using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.ViewModels;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pendingStatuses = new[] { "Chờ duyệt", "Pending" };

            var model = new ApprovalDashboardViewModel
            {
                PendingStudentRegistrationCount = await _context.StudentRegistrations
                    .CountAsync(x => x.Status == "Chờ duyệt"),

                PendingFormCount = await _context.StudentRequestForms
                    .CountAsync(x => pendingStatuses.Contains(x.Status)),

                PendingVehicleCount = await _context.Vehicles
                    .CountAsync(x => pendingStatuses.Contains(x.Status)),

                PendingStudentRegistrations = await _context.StudentRegistrations
                    .Where(x => x.Status == "Chờ duyệt")
                    .OrderByDescending(x => x.SubmittedAt)
                    .Select(x => new ApprovalListItemViewModel
                    {
                        Id = x.Id,
                        Title = x.FullName,
                        Subtitle = x.StudentCode + " - " + x.Email,
                        Status = x.Status,
                        CreatedAt = x.SubmittedAt
                    })
                    .Take(5)
                    .ToListAsync(),

                PendingForms = await _context.StudentRequestForms
                    .Where(x => pendingStatuses.Contains(x.Status))
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new ApprovalListItemViewModel
                    {
                        Id = x.Id,
                        Title = x.FormType + " - " + x.Title,
                        Subtitle = x.ResidentProfile != null ? x.ResidentProfile.FullName : "Chưa có thông tin sinh viên",
                        Status = x.Status,
                        CreatedAt = x.CreatedAt
                    })
                    .Take(5)
                    .ToListAsync(),

                PendingVehicles = await _context.Vehicles
                    .Where(x => pendingStatuses.Contains(x.Status))
                    .OrderByDescending(x => x.Id)
                    .Select(x => new ApprovalListItemViewModel
                    {
                        Id = x.Id,
                        Title = x.LicensePlate,
                        Subtitle = x.ResidentProfile != null ? x.ResidentProfile.FullName : "Chưa có thông tin sinh viên",
                        Status = x.Status,
                        CreatedAt = null
                    })
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
