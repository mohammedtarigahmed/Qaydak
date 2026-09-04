using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qaydak.Data;
using Qaydak.Models;

namespace Qaydak.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
            }

            int totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Search = search;

            return View(customers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "تم تعديل هذا العميل من مكان آخر في نفس الوقت. حدّث الصفحة وحاول تاني.");
                return View(customer);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasInvoices = await _context.Invoices.AnyAsync(i => i.CustomerId == id);
            if (hasInvoices)
            {
                TempData["Error"] = "لا يمكن حذف عميل لديه فواتير مسجلة";
                return RedirectToAction("Index");
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> SearchPartial(string? search, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
            }

            int totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Search = search;

            return PartialView("_CustomerTablePartial", customers);
        }
    }
}