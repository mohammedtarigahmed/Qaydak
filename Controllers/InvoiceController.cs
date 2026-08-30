using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qaydak.Data;
using Qaydak.Models;
using QRCoder;
using QuestPDF.Fluent;

namespace Qaydak.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;

        public InvoiceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .ToListAsync();

            return View(invoices);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _context.Customers.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(InvoiceCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _context.Customers.ToListAsync();
                return View(vm);
            }

            var invoice = new Invoice
            {
                InvoiceNumber = vm.InvoiceNumber,
                CustomerId = vm.CustomerId,
                VatRate = vm.VatRate,
                IssueDate = DateTime.Now
                // IsPaid و PaidAmount بياخدوا القيم الافتراضية من الموديل، ومحدش يقدر يتحكم فيهم من الفورم
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(InvoiceItem item)
        {
            if (!ModelState.IsValid)
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == item.InvoiceId);

                if (invoice == null)
                {
                    return NotFound();
                }

                ViewBag.NewItem = item;
                return View("Details", invoice);
            }

            _context.InvoiceItems.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = item.InvoiceId });
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            decimal subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
            decimal vatAmount = subtotal * (invoice.VatRate / 100);
            decimal total = subtotal + vatAmount;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text($"فاتورة رقم: {invoice.InvoiceNumber}")
                        .FontSize(18).Bold();

                    page.Content().Column(column =>
                    {
                        column.Item().Text($"العميل: {invoice.Customer?.Name}");
                        column.Item().Text($"التاريخ: {invoice.IssueDate:yyyy-MM-dd}");
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("الوصف").Bold();
                                header.Cell().Text("الكمية").Bold();
                                header.Cell().Text("السعر").Bold();
                                header.Cell().Text("الإجمالي").Bold();
                            });

                            foreach (var item in invoice.Items)
                            {
                                table.Cell().Text(item.Description);
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text(item.UnitPrice.ToString());
                                table.Cell().Text((item.Quantity * item.UnitPrice).ToString());
                            }
                        });

                        column.Item().PaddingVertical(10);
                        column.Item().Text($"المجموع الفرعي: {subtotal}");
                        column.Item().Text($"الضريبة ({invoice.VatRate}%): {vatAmount}");
                        column.Item().Text($"الإجمالي: {total}").FontSize(14).Bold();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Invoice-{invoice.InvoiceNumber}.pdf");
        }

        public async Task<IActionResult> QrCode(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            decimal subtotal = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
            decimal vatAmount = subtotal * (invoice.VatRate / 100);
            decimal total = subtotal + vatAmount;

            string base64Tlv = ZatcaQrHelper.GenerateBase64Tlv(
                sellerName: "قيدك - نشاط تجريبي",
                vatNumber: "300000000000003",
                timestamp: invoice.IssueDate,
                invoiceTotal: total,
                vatAmount: vatAmount
            );

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(base64Tlv, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(10);

            return File(qrBytes, "image/png");
        }
    }
}