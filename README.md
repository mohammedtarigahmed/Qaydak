# قيدك (Qaydak)

نظام فوترة ومحاسبة مبسّط لأصحاب الأعمال الصغيرة في السعودية، مبني بـ ASP.NET Core MVC.

## المميزات

- إدارة العملاء (إضافة، تعديل، حذف، بحث)
- إنشاء فواتير مرتبطة بالعملاء مع بنود متعددة
- حساب ضريبة القيمة المضافة تلقائيًا
- توليد فاتورة PDF قابلة للتحميل
- توليد QR Code بصيغة TLV (مبدأ فاتورة إلكترونية)
- تسجيل الدفعات وتتبع حالة الفاتورة (مدفوعة / غير مدفوعة)
- نظام تسجيل دخول آمن (ASP.NET Core Identity)
- بحث و Pagination على القوائم

## التقنيات المستخدمة

- ASP.NET Core 10 MVC
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity
- QuestPDF (توليد PDF)
- QRCoder (توليد QR Code)
- Bootstrap 5

## كيفية التشغيل محليًا

1. Clone المشروع:
   \`\`\`
   git clone https://github.com/mohammedtarigahmed/Qaydak.git
   \`\`\`

2. عدّل الـ connection string في \`appsettings.json\` حسب سيرفر SQL Server عندك

3. طبّق الـ Migrations:
   \`\`\`
   dotnet ef database update
   \`\`\`

4. شغّل المشروع:
   \`\`\`
   dotnet run
   \`\`\`

## ملاحظات معمارية

- تم تطبيق حماية CSRF على مستوى المشروع بالكامل
- استخدام ViewModels لمنع ثغرات Overposting
- Concurrency Protection على العمليات الحساسة (تحديث بيانات العملاء)
- تسجيل الأحداث المهمة (Logging) ومعالجة الأخطاء المركزية

## خطط تطوير مستقبلية

- تكامل كامل مع منصة فاتورة (ZATCA) للمرحلة الثانية (التوقيع الرقمي والربط المباشر)
- تحويل المشروع لنظام Multi-Tenant (SaaS) يخدم عدة أنشطة تجارية
- إضافة اختبارات وحدة (Unit Tests)