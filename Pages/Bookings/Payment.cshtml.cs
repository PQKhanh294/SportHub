using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Pages.Bookings
{
    [Authorize]
    public class PaymentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // ── Cấu hình tài khoản nhận tiền ──────────────────────────────────────
        // Lấy từ https://vietqr.io → Tích hợp → Template ID + Quick Link
        private const string BankBin     = "970436";         // BIN Vietcombank
        private const string AccountNo   = "1028177048";     // Số tài khoản
        private const string AccountName = "VO VAN HAI";     // Tên chủ TK (không dấu)
        private const string TemplateId  = "nXxLBhM";        // Template ID từ vietqr.io
        private const string BankId      = "VCB";            // Dùng để hiển thị tên NH

        public PaymentModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ── View data ──────────────────────────────────────────────────────────
        public PaymentViewModel? PaymentInfo { get; set; }

        // ── GET: /Bookings/Payment?bookingId=xxx ───────────────────────────────
        public async Task<IActionResult> OnGetAsync(int bookingId)
        {
            var dbg = $"[PAY {DateTime.Now:HH:mm:ss}]";
            Console.WriteLine($"{dbg} ===== Payment.OnGetAsync bookingId={bookingId} =====");

            ViewData["ActivePage"] = "Bookings";

            var userId = GetCurrentUserId();
            Console.WriteLine($"{dbg}   userId={userId}");

            Console.WriteLine($"{dbg}   Đang load booking từ DB...");
            var booking = await LoadBookingAsync(bookingId);

            if (booking == null)
            {
                Console.WriteLine($"{dbg} ❌ Booking {bookingId} KHÔNG TỒN TẠI trong DB");
                TempData["ErrorMessage"] = "Không tìm thấy booking hoặc bạn không có quyền truy cập.";
                return RedirectToPage("/Bookings/MyBookings");
            }

            Console.WriteLine($"{dbg} ✅ Booking tìm thấy: ID={booking.BookingID}, UserID={booking.UserID}, Status={booking.Status}, Amount={booking.FinalAmount}");

            if (booking.UserID != userId)
            {
                Console.WriteLine($"{dbg} ❌ UserID không khớp: booking.UserID={booking.UserID} vs current={userId}");
                TempData["ErrorMessage"] = "Không tìm thấy booking hoặc bạn không có quyền truy cập.";
                return RedirectToPage("/Bookings/MyBookings");
            }

            if (booking.Status == "Cancelled" || booking.Status == "Completed")
            {
                Console.WriteLine($"{dbg} ❌ Booking đã {booking.Status} → redirect MyBookings");
                TempData["ErrorMessage"] = "Booking này đã hoàn thành hoặc đã huỷ.";
                return RedirectToPage("/Bookings/MyBookings");
            }

            Console.WriteLine($"{dbg}   BuildViewModel...");
            PaymentInfo = BuildViewModel(booking);
            Console.WriteLine($"{dbg} ✅ ViewModel OK — QrUrl={PaymentInfo.QrCodeUrl}");
            Console.WriteLine($"{dbg} ✅ VietQrPageUrl={PaymentInfo.VietQrPageUrl}");

            return Page();
        }

        // ── POST: Người dùng xác nhận "Tôi đã chuyển khoản" ──────────────────
        public async Task<IActionResult> OnPostConfirmAsync(int bookingId, IFormFile? ReceiptImage)
        {
            var dbg = $"[PAY {DateTime.Now:HH:mm:ss}]";
            Console.WriteLine($"{dbg} ===== Payment.OnPostConfirmAsync bookingId={bookingId} =====");

            var userId = GetCurrentUserId();
            Console.WriteLine($"{dbg}   userId={userId}");

            var booking = await LoadBookingAsync(bookingId);

            if (booking == null || booking.UserID != userId)
            {
                Console.WriteLine($"{dbg} ❌ Booking không tìm thấy hoặc sai user");
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToPage("/Bookings/MyBookings");
            }

            Console.WriteLine($"{dbg}   Booking status hiện tại: {booking.Status}");
            if (booking.Status != "Pending")
            {
                Console.WriteLine($"{dbg} ❌ Booking không ở Pending → redirect");
                TempData["ErrorMessage"] = "Booking này không ở trạng thái chờ thanh toán.";
                return RedirectToPage("/Bookings/MyBookings");
            }

            // Upload ảnh biên lai
            string? receiptUrl = null;
            if (ReceiptImage != null && ReceiptImage.Length > 0)
            {
                // Validate file extension
                var ext = Path.GetExtension(ReceiptImage.FileName).ToLower();
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExts.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh (.jpg, .jpeg, .png, .gif)";
                    return RedirectToPage("/Bookings/Payment", new { bookingId });
                }

                // Check size (max 5MB)
                if (ReceiptImage.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Kích thước ảnh biên lai không được vượt quá 5MB";
                    return RedirectToPage("/Bookings/Payment", new { bookingId });
                }

                var folderPath = Path.Combine(_environment.WebRootPath, "uploads", "receipts");
                Directory.CreateDirectory(folderPath);

                var fileName = $"receipt_{bookingId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ReceiptImage.CopyToAsync(stream);
                }

                receiptUrl = $"/uploads/receipts/{fileName}";
                Console.WriteLine($"{dbg} ✅ Đã lưu ảnh biên lai: {receiptUrl}");
            }
            else
            {
                // Bắt buộc phải có ảnh biên lai
                TempData["ErrorMessage"] = "Vui lòng tải lên ảnh chụp biên lai chuyển khoản thành công.";
                return RedirectToPage("/Bookings/Payment", new { bookingId });
            }

            Console.WriteLine($"{dbg}   Cập nhật Booking → Confirmed...");
            booking.Status    = "Confirmed";
            booking.UpdatedAt = DateTime.UtcNow;

            var pendingPayment = booking.Payments
                .FirstOrDefault(p => p.Status == "Pending" && p.PaymentType == "Payment");

            if (pendingPayment != null)
            {
                pendingPayment.Status  = "Success";
                pendingPayment.PaidAt  = DateTime.UtcNow;
                pendingPayment.ReceiptUrl = receiptUrl; // Lưu URL ảnh biên lai
                pendingPayment.TransactionRef = $"QR-{bookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                Console.WriteLine($"{dbg}   Payment → Success, TransactionRef={pendingPayment.TransactionRef}");
            }
            else
            {
                Console.WriteLine($"{dbg}   ⚠️ Không tìm thấy Pending payment record");
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"{dbg} ✅ Booking {bookingId} → Confirmed thành công!");

            TempData["SuccessMessage"] = "Xác nhận thanh toán thành công! Booking của bạn đã được xác nhận.";
            return RedirectToPage("/Bookings/MyBookings");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task<Booking?> LoadBookingAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Court)
                    .ThenInclude(c => c.Venue)
                .Include(b => b.Court)
                    .ThenInclude(c => c.Sport)
                .Include(b => b.Court)
                    .ThenInclude(c => c.Images)
                .Include(b => b.BookingSlots)
                    .ThenInclude(bs => bs.TimeSlot)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);
        }

        private PaymentViewModel BuildViewModel(Booking booking)
        {
            var orderedSlots = booking.BookingSlots
                .Where(s => s.TimeSlot != null)
                .OrderBy(s => s.TimeSlot.StartTime)
                .ToList();

            var timeDisplay = orderedSlots.Count > 0
                ? $"{orderedSlots.First().TimeSlot.StartTime:hh\\:mm} – {orderedSlots.Last().TimeSlot.EndTime:hh\\:mm}"
                : "–";

            var description = $"SPORTHUB BK{booking.BookingID} {booking.BookingDate:ddMMyy}";
            var amountInt   = (long)booking.FinalAmount;

            // ── URL nhúng ảnh QR (img.vietqr.io) ────────────────────────────────
            var qrUrl = $"https://img.vietqr.io/image/{BankBin}-{AccountNo}-{TemplateId}.png"
                      + $"?accountName={Uri.EscapeDataString(AccountName)}"
                      + $"&amount={amountInt}"
                      + $"&addInfo={Uri.EscapeDataString(description)}";

            // ── URL trang QR VietQR (my.vietqr.io) — luôn hoạt động ──────────────
            // Người dùng click vào đây → mở trang VietQR với QR sẵn, số tiền đúng
            var vietQrPageUrl = $"https://my.vietqr.io/vietqr/templates/{TemplateId}"
                              + $"?amount={amountInt}"
                              + $"&addInfo={Uri.EscapeDataString(description)}";

            var mainImage = booking.Court.Images
                .OrderBy(i => i.SortOrder)
                .FirstOrDefault(i => i.IsMain)?.ImageUrl
                ?? booking.Court.Images
                    .OrderBy(i => i.SortOrder)
                    .FirstOrDefault()?.ImageUrl;

            return new PaymentViewModel
            {
                BookingID      = booking.BookingID,
                CourtName      = booking.Court.CourtName,
                VenueName      = booking.Court.Venue.VenueName,
                Address        = booking.Court.Venue.Address,
                SportName      = booking.Court.Sport?.SportName ?? "Sport",
                CourtImageUrl  = mainImage,
                BookingDate    = booking.BookingDate,
                TimeDisplay    = timeDisplay,
                TotalAmount    = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount    = booking.FinalAmount,
                Status         = booking.Status,
                PaymentMethod  = booking.Payments.FirstOrDefault()?.PaymentMethod switch
                {
                    "BankTransfer" => "Chuyển khoản QR",
                    "VNPay"        => "VNPay",
                    "MoMo"         => "Ví MoMo",
                    "ZaloPay"      => "ZaloPay",
                    "Cash"         => "Tiền mặt",
                    var m          => m ?? "Chuyển khoản QR"
                },
                QrCodeUrl       = qrUrl,
                VietQrPageUrl   = vietQrPageUrl,
                BankId          = BankId,
                AccountNo       = AccountNo,
                AccountName     = AccountName,
                TransferContent = description,
            };
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // ── ViewModel ─────────────────────────────────────────────────────────
        public class PaymentViewModel
        {
            public int      BookingID      { get; set; }
            public string   CourtName      { get; set; } = string.Empty;
            public string   VenueName      { get; set; } = string.Empty;
            public string   Address        { get; set; } = string.Empty;
            public string   SportName      { get; set; } = string.Empty;
            public string?  CourtImageUrl  { get; set; }
            public DateTime BookingDate    { get; set; }
            public string   TimeDisplay    { get; set; } = string.Empty;
            public decimal  TotalAmount    { get; set; }
            public decimal  DiscountAmount { get; set; }
            public decimal  FinalAmount    { get; set; }
            public string   Status         { get; set; } = string.Empty;
            public string   PaymentMethod  { get; set; } = string.Empty;

            // VietQR
            public string QrCodeUrl      { get; set; } = string.Empty;
            public string VietQrPageUrl  { get; set; } = string.Empty; // link trang QR my.vietqr.io
            public string BankId         { get; set; } = string.Empty;
            public string AccountNo      { get; set; } = string.Empty;
            public string AccountName    { get; set; } = string.Empty;
            public string TransferContent { get; set; } = string.Empty;
        }
    }
}
