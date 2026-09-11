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
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(AppDbContext context, ILogger<InvoiceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Invoices.Include(i => i.Customer).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(search) ||
                                        (i.Customer != null && i.Customer.Name.Contains(search)));
            }

            if (status == "paid")
            {
                query = query.Where(i => i.IsPaid && !i.IsVoided);
            }
            else if (status == "unpaid")
            {
                query = query.Where(i => !i.IsPaid && !i.IsVoided);
            }
            else if (status == "voided")
            {
                query = query.Where(i => i.IsVoided);
            }

            int totalCount = await query.CountAsync();
            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(invoices);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _context.Customers.ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(new InvoiceWithItemsViewModel { Items = new List<InvoiceItemViewModel> { new() } });
        }

        [HttpPost]
        public async Task<IActionResult> Create(InvoiceWithItemsViewModel vm)
        {
            if (vm.Items == null || !vm.Items.Any())
            {
                ModelState.AddModelError("", "لازم تضيف بند واحد على الأقل");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _context.Customers.ToListAsync();
                ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
                return View(vm);
            }

            var invoiceNumber = await GenerateNextInvoiceNumber();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = vm.CustomerId,
                VatRate = vm.VatRate,
                DiscountAmount = vm.DiscountAmount,
                DueDate = vm.DueDate,
                IssueDate = DateTime.Now,
                Items = (vm.Items ?? new List<InvoiceItemViewModel>()).Select(i => new InvoiceItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} created for customer {CustomerId} with {ItemCount} items", invoice.InvoiceNumber, invoice.CustomerId, invoice.Items.Count);

            return RedirectToAction("Details", new { id = invoice.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsVoided || invoice.PaidAmount > 0)
            {
                TempData["Error"] = "لا يمكن تعديل فاتورة ملغاة أو تم تسجيل دفعة عليها";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.Customers = await _context.Customers.ToListAsync();

            var vm = new InvoiceEditViewModel
            {
                Id = invoice.Id,
                CustomerId = invoice.CustomerId,
                VatRate = invoice.VatRate,
                DiscountAmount = invoice.DiscountAmount,
                DueDate = invoice.DueDate,
                RowVersion = invoice.RowVersion
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _context.Customers.ToListAsync();
                return View(vm);
            }

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsVoided || invoice.PaidAmount > 0)
            {
                TempData["Error"] = "لا يمكن تعديل فاتورة ملغاة أو تم تسجيل دفعة عليها";
                return RedirectToAction("Details", new { id });
            }

            invoice.CustomerId = vm.CustomerId;
            invoice.VatRate = vm.VatRate;
            invoice.DiscountAmount = vm.DiscountAmount;
            invoice.DueDate = vm.DueDate;
            _context.Entry(invoice).Property("RowVersion").OriginalValue = vm.RowVersion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "تم تعديل هذه الفاتورة من مكان آخر في نفس الوقت. حدّث الصفحة وحاول تاني.");
                ViewBag.Customers = await _context.Customers.ToListAsync();
                return View(vm);
            }

            _logger.LogInformation("Invoice {InvoiceId} header edited", id);

            return RedirectToAction("Details", new { id });
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

            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(InvoiceItem item)
        {
            var parentInvoice = await _context.Invoices.FindAsync(item.InvoiceId);
            if (parentInvoice == null)
            {
                return NotFound();
            }

            if (parentInvoice.IsVoided)
            {
                TempData["Error"] = "لا يمكن التعديل على فاتورة ملغاة";
                return RedirectToAction("Details", new { id = item.InvoiceId });
            }

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
                ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
                return View("Details", invoice);
            }

            _context.InvoiceItems.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = item.InvoiceId });
        }

        public async Task<IActionResult> EditItem(int id)
        {
            var item = await _context.InvoiceItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> EditItem(int id, InvoiceItem item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            var parentInvoice = await _context.Invoices.FindAsync(item.InvoiceId);
            if (parentInvoice == null)
            {
                return NotFound();
            }

            if (parentInvoice.IsVoided)
            {
                TempData["Error"] = "لا يمكن التعديل على فاتورة ملغاة";
                return RedirectToAction("Details", new { id = item.InvoiceId });
            }

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _context.InvoiceItems.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = item.InvoiceId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteItem(int id, int invoiceId)
        {
            var parentInvoice = await _context.Invoices.FindAsync(invoiceId);
            if (parentInvoice == null)
            {
                return NotFound();
            }

            if (parentInvoice.IsVoided)
            {
                TempData["Error"] = "لا يمكن التعديل على فاتورة ملغاة";
                return RedirectToAction("Details", new { id = invoiceId });
            }

            var item = await _context.InvoiceItems.FindAsync(id);
            if (item != null)
            {
                _context.InvoiceItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = invoiceId });
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment(int id, decimal amount)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsVoided)
            {
                TempData["Error"] = "لا يمكن التعديل على فاتورة ملغاة";
                return RedirectToAction("Details", new { id });
            }

            if (amount <= 0)
            {
                TempData["Error"] = "المبلغ المدفوع لازم يكون أكبر من صفر";
                return RedirectToAction("Details", new { id });
            }

            decimal remaining = invoice.GetTotal() - invoice.PaidAmount;
            if (amount > remaining)
            {
                TempData["Error"] = $"المبلغ أكبر من المتبقي على الفاتورة ({remaining:F2})";
                return RedirectToAction("Details", new { id });
            }

            invoice.PaidAmount += amount;

            if (invoice.PaidAmount >= invoice.GetTotal())
            {
                invoice.IsPaid = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment of {Amount} recorded for invoice {InvoiceId}", amount, id);

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Void(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            invoice.IsVoided = true;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Invoice {InvoiceId} was voided", id);

            return RedirectToAction("Details", new { id });
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

            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

            decimal subtotal = invoice.GetSubtotal();
            decimal vatAmount = invoice.GetVatAmount();
            decimal total = invoice.GetTotal();

            byte[] qrBytes = await GenerateQrBytes(invoice);

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.ContentFromRightToLeft();
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(headerColumn =>
                    {
                        headerColumn.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(settings?.BusinessName ?? "قيدك").FontSize(20).Bold();
                                if (!string.IsNullOrEmpty(settings?.VatNumber))
                                {
                                    col.Item().Text($"الرقم الضريبي: {settings.VatNumber}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                                }
                            });

                            row.ConstantItem(90).Image(qrBytes);
                        });

                        headerColumn.Item().PaddingTop(10).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        column.Item().Text("فاتورة ضريبية")
                            .FontSize(16).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"رقم الفاتورة: {invoice.InvoiceNumber}").Bold();
                                col.Item().Text($"التاريخ: {invoice.IssueDate:yyyy-MM-dd}");
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"العميل: {invoice.Customer?.Name}").Bold();
                            });
                        });

                        if (invoice.IsVoided)
                        {
                            column.Item().PaddingTop(10).Background(QuestPDF.Helpers.Colors.Red.Lighten4)
                                .Padding(8).Text("هذه الفاتورة ملغاة").FontColor(QuestPDF.Helpers.Colors.Red.Darken2).Bold();
                        }

                        column.Item().PaddingTop(15).Table(table =>
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
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("الوصف").Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("الكمية").Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("السعر").Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("الإجمالي").Bold();
                            });

                            foreach (var item in invoice.Items)
                            {
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(item.Description);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(item.Quantity.ToString("0.##"));
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(item.UnitPrice.ToString("0.##"));
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text((item.Quantity * item.UnitPrice).ToString("0.##"));
                            }
                        });

                        column.Item().PaddingTop(15).AlignLeft().Width(220).Column(totalsColumn =>
                        {
                            totalsColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text("المجموع الفرعي:");
                                row.ConstantItem(80).AlignLeft().Text(subtotal.ToString("F2"));
                            });

                            if (invoice.DiscountAmount > 0)
                            {
                                totalsColumn.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("الخصم:");
                                    row.ConstantItem(80).AlignLeft().Text($"-{invoice.DiscountAmount:F2}");
                                });
                            }

                            totalsColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"الضريبة ({invoice.VatRate}%):");
                                row.ConstantItem(80).AlignLeft().Text(vatAmount.ToString("F2"));
                            });

                            totalsColumn.Item().PaddingTop(5).BorderTop(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Darken1)
                                .PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text("الإجمالي:").Bold().FontSize(13);
                                row.ConstantItem(80).AlignLeft().Text(total.ToString("F2")).Bold().FontSize(13);
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text("تم إنشاء هذه الفاتورة عبر نظام قيدك")
                        .FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                });
            });

            var pdfBytes = document.GeneratePdf();
            Response.Headers.Append("Content-Disposition", $"inline; filename=Invoice-{invoice.InvoiceNumber}.pdf");
            return File(pdfBytes, "application/pdf");
        }

        private async Task<byte[]> GenerateQrBytes(Invoice invoice)
        {
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

            decimal vatAmount = invoice.GetVatAmount();
            decimal total = invoice.GetTotal();

            string base64Tlv = ZatcaQrHelper.GenerateBase64Tlv(
                sellerName: settings?.BusinessName ?? "غير محدد",
                vatNumber: settings?.VatNumber ?? "000000000000000",
                timestamp: invoice.IssueDate,
                invoiceTotal: total,
                vatAmount: vatAmount
            );

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(base64Tlv, QRCodeGenerator.ECCLevel.Q);
            using var qrCodeImage = new PngByteQRCode(qrData);
            return qrCodeImage.GetGraphic(10);
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

            byte[] qrBytes = await GenerateQrBytes(invoice);
            return File(qrBytes, "image/png");
        }

        private async Task<string> GenerateNextInvoiceNumber()
        {
            var results = await _context.Database
                .SqlQuery<int>($"SELECT NEXT VALUE FOR InvoiceNumberSequence AS Value")
                .ToListAsync();

            return $"INV-{results[0]:D5}";
        }

        public async Task<IActionResult> SearchPartial(string? search, string? status, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Invoices.Include(i => i.Customer).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(search) ||
                                        (i.Customer != null && i.Customer.Name.Contains(search)));
            }

            if (status == "paid")
            {
                query = query.Where(i => i.IsPaid && !i.IsVoided);
            }
            else if (status == "unpaid")
            {
                query = query.Where(i => !i.IsPaid && !i.IsVoided);
            }
            else if (status == "voided")
            {
                query = query.Where(i => i.IsVoided);
            }

            int totalCount = await query.CountAsync();
            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Search = search;

            return PartialView("_InvoiceTablePartial", invoices);
        }

        private string FormatPhoneForWhatsApp(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            return new string(phone.Where(char.IsDigit).ToArray());
        }

        public async Task<IActionResult> WhatsAppLink(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null || invoice.Customer == null)
            {
                return Content("لم يتم العثور على الفاتورة أو العميل");
            }

            string phone = FormatPhoneForWhatsApp(invoice.Customer.PhoneNumber);
            if (string.IsNullOrEmpty(phone))
            {
                return Content("لا يوجد رقم هاتف مسجل لهذا العميل");
            }

            string pdfUrl = Url.Action("DownloadPdf", "Invoice", new { id }, Request.Scheme)!;
            string message = $"مرحبًا {invoice.Customer.Name}، فاتورتك رقم {invoice.InvoiceNumber} بإجمالي {invoice.GetTotal():F2} ريال. يمكنك عرضها من الرابط: {pdfUrl}";

            string whatsappUrl = $"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}";

            return Content(whatsappUrl);
        }

        public async Task<IActionResult> EmailLink(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null || invoice.Customer == null)
            {
                return Content("لم يتم العثور على الفاتورة أو العميل");
            }

            if (string.IsNullOrWhiteSpace(invoice.Customer.Email))
            {
                return Content("لا يوجد بريد إلكتروني مسجل لهذا العميل");
            }

            string pdfUrl = Url.Action("DownloadPdf", "Invoice", new { id }, Request.Scheme)!;
            string subject = $"فاتورة رقم {invoice.InvoiceNumber}";
            string body = $"مرحبًا {invoice.Customer.Name}،\n\nفاتورتك رقم {invoice.InvoiceNumber} بإجمالي {invoice.GetTotal():F2} ريال.\nيمكنك عرضها من الرابط التالي:\n{pdfUrl}\n\nشكرًا لتعاملكم معنا.";

            string mailtoUrl = $"mailto:{invoice.Customer.Email}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

            return Content(mailtoUrl);
        }

        public async Task<IActionResult> Duplicate(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var vm = new InvoiceWithItemsViewModel
            {
                CustomerId = invoice.CustomerId,
                VatRate = invoice.VatRate,
                DiscountAmount = invoice.DiscountAmount,
                Items = invoice.Items.Select(i => new InvoiceItemViewModel
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            ViewBag.Customers = await _context.Customers.ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

            return View("Create", vm);
        }

        public async Task<IActionResult> ExportExcel(string? search, string? status)
        {
            var query = _context.Invoices.Include(i => i.Customer).Include(i => i.Items).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(search) ||
                                        (i.Customer != null && i.Customer.Name.Contains(search)));
            }

            if (status == "paid")
            {
                query = query.Where(i => i.IsPaid && !i.IsVoided);
            }
            else if (status == "unpaid")
            {
                query = query.Where(i => !i.IsPaid && !i.IsVoided);
            }
            else if (status == "voided")
            {
                query = query.Where(i => i.IsVoided);
            }

            var invoices = await query.OrderByDescending(i => i.IssueDate).ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الفواتير");

            worksheet.Cell(1, 1).Value = "رقم الفاتورة";
            worksheet.Cell(1, 2).Value = "العميل";
            worksheet.Cell(1, 3).Value = "التاريخ";
            worksheet.Cell(1, 4).Value = "الإجمالي";
            worksheet.Cell(1, 5).Value = "المدفوع";
            worksheet.Cell(1, 6).Value = "المتبقي";
            worksheet.Cell(1, 7).Value = "الحالة";

            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var invoice in invoices)
            {
                worksheet.Cell(row, 1).Value = invoice.InvoiceNumber;
                worksheet.Cell(row, 2).Value = invoice.Customer?.Name ?? "";
                worksheet.Cell(row, 3).Value = invoice.IssueDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 4).Value = invoice.GetTotal();
                worksheet.Cell(row, 5).Value = invoice.PaidAmount;
                worksheet.Cell(row, 6).Value = invoice.GetTotal() - invoice.PaidAmount;
                worksheet.Cell(row, 7).Value = invoice.IsVoided ? "ملغاة" : (invoice.IsPaid ? "مدفوعة" : "غير مدفوعة");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Invoices.xlsx");
        }
    }
}