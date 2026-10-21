using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Data;
using QuanLyKTX.Models;

namespace QuanLyKTX.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FormTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };

        public FormTemplateController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(string? keyword = null, string? status = "Tất cả")
        {
            var query = _context.FormTemplates.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.Type.Contains(keyword)
                    || (x.Description != null && x.Description.Contains(keyword)));
            }

            if (status == "Đang sử dụng")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (status == "Tạm ngưng")
            {
                query = query.Where(x => !x.IsActive);
            }
            else if (status == "Tạm thời")
            {
                query = query.Where(x => x.IsTemporary);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;

            var data = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return View(data);
        }

        public IActionResult Create()
        {
            return View(new FormTemplate { IsActive = true, Icon = "bi-journal-text" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormTemplate model, IFormFile? templateFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (templateFile != null && templateFile.Length > 0)
            {
                var fileResult = await SaveTemplateFileAsync(templateFile);
                if (!fileResult.Success)
                {
                    ModelState.AddModelError(nameof(templateFile), fileResult.Message);
                    return View(model);
                }

                model.TemplateFilePath = fileResult.Path;
                model.OriginalFileName = templateFile.FileName;
            }

            model.CreatedAt = DateTime.Now;
            _context.FormTemplates.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm biểu mẫu mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.FormTemplates.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FormTemplate model, IFormFile? templateFile)
        {
            if (id != model.Id) return NotFound();

            var item = await _context.FormTemplates.FindAsync(id);
            if (item == null) return NotFound();

            if (!ModelState.IsValid) return View(model);

            item.Name = model.Name.Trim();
            item.Type = model.Type.Trim();
            item.Description = model.Description?.Trim();
            item.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "bi-journal-text" : model.Icon.Trim();
            item.IsActive = model.IsActive;
            item.IsTemporary = model.IsTemporary;

            if (templateFile != null && templateFile.Length > 0)
            {
                var fileResult = await SaveTemplateFileAsync(templateFile);
                if (!fileResult.Success)
                {
                    ModelState.AddModelError(nameof(templateFile), fileResult.Message);
                    return View(model);
                }

                item.TemplateFilePath = fileResult.Path;
                item.OriginalFileName = templateFile.FileName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật biểu mẫu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var item = await _context.FormTemplates.FindAsync(id);
            if (item == null) return NotFound();

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = item.IsActive ? "Đã bật biểu mẫu." : "Đã tạm ngưng biểu mẫu.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.FormTemplates.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.FormTemplates.FindAsync(id);
            if (item == null) return NotFound();

            _context.FormTemplates.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa biểu mẫu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, string Message, string? Path)> SaveTemplateFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, "Chỉ chấp nhận file .pdf, .doc, .docx, .jpg, .jpeg, .png.", null);
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", "form-templates");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, string.Empty, $"/uploads/form-templates/{fileName}");
        }
    }
}
