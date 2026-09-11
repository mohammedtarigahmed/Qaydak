using Microsoft.AspNetCore.Mvc;
using Qaydak.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Qaydak.Data;

namespace Qaydak.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard(string period = "month")
        {
            DateTime startDate = period switch
            {
                "today" => DateTime.Now.Date,
                "week" => DateTime.Now.Date.AddDays(-7),
                "month" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                "year" => new DateTime(DateTime.Now.Year, 1, 1),
                "all" => DateTime.MinValue,
                _ => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            };

            var invoices = await _context.Invoices
                .Where(i => !i.IsVoided && i.IssueDate >= startDate)
                .Include(i => i.Items)
                .Include(i => i.Customer)
                .ToListAsync();

            var totalSales = invoices.Sum(i => i.GetTotal());
            var totalPaid = invoices.Sum(i => i.PaidAmount);
            var totalDue = totalSales - totalPaid;
            var invoiceCount = invoices.Count;
            var customerCount = await _context.Customers.CountAsync();
            var overdueCount = invoices.Count(i => i.IsOverdue());

            var recentInvoices = invoices.OrderByDescending(i => i.IssueDate).Take(5).ToList();

            ViewBag.TotalSales = totalSales;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalDue = totalDue;
            ViewBag.InvoiceCount = invoiceCount;
            ViewBag.CustomerCount = customerCount;
            ViewBag.OverdueCount = overdueCount;
            ViewBag.RecentInvoices = recentInvoices;
            ViewBag.SelectedPeriod = period;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionFeature != null)
            {
                _logger.LogError(exceptionFeature.Error, "Unhandled exception occurred at path {Path}", exceptionFeature.Path);
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
