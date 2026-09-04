using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qaydak.Data;
using Qaydak.Models;

namespace Qaydak.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
            return View(settings ?? new BusinessSettings());
        }

        [HttpPost]
        public async Task<IActionResult> Index(BusinessSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return View(settings);
            }

            var existing = await _context.BusinessSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.BusinessSettings.Add(settings);
            }
            else
            {
                existing.BusinessName = settings.BusinessName;
                existing.VatNumber = settings.VatNumber;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حفظ بيانات النشاط بنجاح";
            return RedirectToAction("Index");
        }
    }
}