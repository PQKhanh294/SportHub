using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Common;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Courts
{
    public class DetailsModel : PageModel
    {
        private readonly ICourtService _courtService;
        private readonly IBookingService _bookingService;

        public DetailsModel(ICourtService courtService, IBookingService bookingService)
        {
            _courtService   = courtService;
            _bookingService = bookingService;
        }

        public CourtDetailItem? Item { get; set; }

        // ── Form chọn slot đặt sân ────────────────────────────────────────────
        [BindProperty]
        public BookingInputModel BookingInput { get; set; } = new();

        public List<SlotItem> AvailableSlots { get; set; } = new();

        // ── GET ───────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync(int? id, DateTime? date)
        {
            ViewData["ActivePage"] = "SearchCourts";
            if (!id.HasValue) return RedirectToPage("/Courts/Search");

            var court = await _courtService.GetCourtDetailsAsync(id.Value);
            if (court == null) return RedirectToPage("/Courts/Search");

            var images = court.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList();
            if (images.Count == 0)
                images.Add("https://lh3.googleusercontent.com/aida-public/AB6AXuAHBxb5mOtwtPLRL5iLOL3N2ia6cQPxzsMvFrnaU-mvIzkN-OFZf-WmxktE2brADSGM8XqvCXWyZX5RI0rLcjNbLPQqAA7MBNg9nWV_EgTr0tZseI1GjDxtA7uyryYCoFe30E3hS6phBiX8SyZU2lxwHdp5ukU5bdCqUM51Yh61cBE-FhONkfUQtsWW1zNn3NYcNngF09ABcjqd6YjVd6XMNMOfGvNJQSt7hTQRySVKZaCC2soLSkTSTAnkmHKoiLMdL3yB3jeOYkU");

            while (images.Count < 5) images.Add(images[0]);

            var minPrice = court.PricingRules.Any() ? court.PricingRules.Min(p => p.UnitPrice) : 150_000m;

            Item = new CourtDetailItem
            {
                CourtId          = court.CourtID,
                CourtName        = court.CourtName,
                VenueName        = court.Venue.VenueName,
                Address          = court.Venue.Address,
                Description      = court.Description ?? "Premium indoor courts with modern lighting and stable playing conditions.",
                MinPriceDisplay  = $"{minPrice:N0} xu/giờ",
                Images           = images,
                OpenTime         = court.Venue.OpenTime,
                CloseTime        = court.Venue.CloseTime,
                HasParking       = court.Venue.AmenityParking,
                HasShower        = court.Venue.AmenityShower,
                HasLocker        = court.Venue.AmenityLocker,
                HasFood          = court.Venue.AmenityFood,
            };

            // Pre-fill ngày đặt mặc định là hôm nay
            var targetDate = date?.Date ?? DateTime.Today;
            BookingInput.CourtID     = court.CourtID;
            BookingInput.BookingDate = targetDate;

            // Load các slot trống trong ngày đó
            await LoadAvailableSlotsAsync(court.CourtID, targetDate, court.PricingRules, targetDate.DayOfWeek);

            return Page();
        }

        // ── POST: tạo booking → redirect sang Payment ─────────────────────────
        public async Task<IActionResult> OnPostBookAsync()
        {
            var dbg = $"[DEBUG {DateTime.Now:HH:mm:ss}]";
            Console.WriteLine($"{dbg} ===== OnPostBookAsync BẮT ĐẦU =====");

            // BƯỚC 1: Kiểm tra đăng nhập
            Console.WriteLine($"{dbg} BƯỚC 1: Kiểm tra auth...");
            if (!User.Identity!.IsAuthenticated)
            {
                Console.WriteLine($"{dbg} ❌ Chưa đăng nhập → redirect Login");
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }
            Console.WriteLine($"{dbg} ✅ Đã đăng nhập: {User.Identity.Name}");

            // BƯỚC 2: Parse form data
            Console.WriteLine($"{dbg} BƯỚC 2: Parse form...");
            Console.WriteLine($"{dbg}   Form keys: {string.Join(", ", Request.Form.Keys)}");

            if (Request.Form.TryGetValue("BookingInput.BookingDate", out var rawDate)
                && DateTime.TryParseExact(rawDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                BookingInput.BookingDate = parsedDate;
                Console.WriteLine($"{dbg}   BookingDate parsed: {parsedDate:yyyy-MM-dd}");
            }
            else
            {
                Console.WriteLine($"{dbg}   ⚠️ BookingDate parse FAIL — rawDate='{rawDate}'");
            }

            if (Request.Form.TryGetValue("BookingInput.CourtID", out var rawCourt)
                && int.TryParse(rawCourt, out var parsedCourtId) && parsedCourtId > 0)
            {
                BookingInput.CourtID = parsedCourtId;
                Console.WriteLine($"{dbg}   CourtID parsed: {parsedCourtId}");
            }
            else
            {
                Console.WriteLine($"{dbg}   ⚠️ CourtID parse FAIL — rawCourt='{rawCourt}'");
            }

            var slotValues = Request.Form["BookingInput.SelectedSlotIDs"];
            Console.WriteLine($"{dbg}   SlotIDs raw: [{string.Join(",", slotValues)}]");
            BookingInput.SelectedSlotIDs = slotValues
                .Where(v => !string.IsNullOrEmpty(v) && int.TryParse(v, out _))
                .Select(v => int.Parse(v!))
                .ToList();
            Console.WriteLine($"{dbg}   SlotIDs parsed: [{string.Join(",", BookingInput.SelectedSlotIDs)}]");

            var paymentMethod = Request.Form["BookingInput.PaymentMethod"].FirstOrDefault() ?? "BankTransfer";
            Console.WriteLine($"{dbg}   PaymentMethod: {paymentMethod}");

            // BƯỚC 3: Validate
            Console.WriteLine($"{dbg} BƯỚC 3: Validate...");
            if (BookingInput.CourtID <= 0)
            {
                Console.WriteLine($"{dbg} ❌ CourtID <= 0 → redirect Search");
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sân. Vui lòng thử lại.";
                return RedirectToPage("/Courts/Search");
            }

            if (BookingInput.SelectedSlotIDs.Count == 0)
            {
                Console.WriteLine($"{dbg} ❌ Không có slot nào được chọn → redirect Details");
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một khung giờ.";
                return RedirectToPage("/Courts/Details/" + BookingInput.CourtID);
            }

            if (BookingInput.BookingDate.Date < DateTime.Today)
            {
                Console.WriteLine($"{dbg} ❌ Ngày quá khứ: {BookingInput.BookingDate:yyyy-MM-dd}");
                TempData["ErrorMessage"] = "Ngày đặt sân không được là ngày trong quá khứ.";
                return RedirectToPage("/Courts/Details/" + BookingInput.CourtID);
            }
            Console.WriteLine($"{dbg} ✅ Validate OK");

            // BƯỚC 4: Lấy UserID
            Console.WriteLine($"{dbg} BƯỚC 4: Lấy UserID...");
            var userId = GetCurrentUserId();
            Console.WriteLine($"{dbg}   UserID = {userId}");
            if (userId <= 0)
            {
                Console.WriteLine($"{dbg} ❌ UserID <= 0 → redirect Login");
                return RedirectToPage("/Auth/Login");
            }

            // BƯỚC 5: Tạo booking
            Console.WriteLine($"{dbg} BƯỚC 5: Gọi CreateBookingAsync...");
            Console.WriteLine($"{dbg}   Params: userId={userId}, courtId={BookingInput.CourtID}, date={BookingInput.BookingDate:yyyy-MM-dd}, slots=[{string.Join(",", BookingInput.SelectedSlotIDs)}], method={paymentMethod}");

            try
            {
                var booking = await _bookingService.CreateBookingAsync(
                    userId,
                    BookingInput.CourtID,
                    BookingInput.BookingDate,
                    BookingInput.SelectedSlotIDs,
                    paymentMethod);

                Console.WriteLine($"{dbg} ✅ Booking tạo thành công! BookingID={booking.BookingID}, Amount={booking.FinalAmount}");
                Console.WriteLine($"{dbg} BƯỚC 6: Redirect → /Bookings/Payment?bookingId={booking.BookingID}");

                return RedirectToPage("/Bookings/Payment", new { bookingId = booking.BookingID });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"{dbg} ❌ InvalidOperationException: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Courts/Details/" + BookingInput.CourtID);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{dbg} ❌ EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"{dbg}   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"{dbg}   InnerException: {ex.InnerException.Message}");

                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToPage("/Courts/Details/" + BookingInput.CourtID);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task<IActionResult> RebuildPageOnErrorAsync()
        {
            ViewData["ActivePage"] = "SearchCourts";
            var court = await _courtService.GetCourtDetailsAsync(BookingInput.CourtID);
            if (court == null) return RedirectToPage("/Courts/Search");

            var images = court.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList();
            if (images.Count == 0)
                images.Add("https://lh3.googleusercontent.com/aida-public/AB6AXuAHBxb5mOtwtPLRL5iLOL3N2ia6cQPxzsMvFrnaU-mvIzkN-OFZf-WmxktE2brADSGM8XqvCXWyZX5RI0rLcjNbLPQqAA7MBNg9nWV_EgTr0tZseI1GjDxtA7uyryYCoFe30E3hS6phBiX8SyZU2lxwHdp5ukU5bdCqUM51Yh61cBE-FhONkfUQtsWW1zNn3NYcNngF09ABcjqd6YjVd6XMNMOfGvNJQSt7hTQRySVKZaCC2soLSkTSTAnkmHKoiLMdL3yB3jeOYkU");
            while (images.Count < 5) images.Add(images[0]);

            var minPrice = court.PricingRules.Any() ? court.PricingRules.Min(p => p.UnitPrice) : 150_000m;
            Item = new CourtDetailItem
            {
                CourtId         = court.CourtID,
                CourtName       = court.CourtName,
                VenueName       = court.Venue.VenueName,
                Address         = court.Venue.Address,
                Description     = court.Description ?? string.Empty,
                MinPriceDisplay = $"{minPrice:N0} xu/giờ",
                Images          = images,
                OpenTime        = court.Venue.OpenTime,
                CloseTime       = court.Venue.CloseTime,
                HasParking      = court.Venue.AmenityParking,
                HasShower       = court.Venue.AmenityShower,
                HasLocker       = court.Venue.AmenityLocker,
                HasFood         = court.Venue.AmenityFood,
            };

            await LoadAvailableSlotsAsync(court.CourtID, BookingInput.BookingDate,
                court.PricingRules, BookingInput.BookingDate.DayOfWeek);
            return Page();
        }

        private async Task LoadAvailableSlotsAsync(
            int courtId,
            DateTime targetDate,
            IEnumerable<Models.Entities.PricingRule> pricingRules,
            DayOfWeek dayOfWeek)
        {
            var slots = await _courtService.GetAvailableSlotsAsync(courtId, targetDate);
            var dayType = (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                ? "Weekend" : "Weekday";

            var vietnamTime = VietnamTime.Now;
            var today = vietnamTime.Date;

            AvailableSlots = slots.Select(s =>
            {
                var price = pricingRules
                    .FirstOrDefault(p => p.SlotID == s.SlotID && p.DayType == dayType)?.UnitPrice
                    ?? pricingRules
                    .FirstOrDefault(p => p.SlotID == s.SlotID)?.UnitPrice
                    ?? 150_000m;

                bool isPast = false;
                if (targetDate.Date == today)
                {
                    if (vietnamTime.TimeOfDay >= s.StartTime)
                    {
                        isPast = true;
                    }
                }
                else if (targetDate.Date < today)
                {
                    isPast = true;
                }

                return new SlotItem
                {
                    SlotID     = s.SlotID,
                    StartTime  = s.StartTime,
                    EndTime    = s.EndTime,
                    SlotLabel  = s.SlotLabel ?? $"{s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm}",
                    Price      = price,
                    IsAvailable = true,
                    IsPast     = isPast
                };
            }).OrderBy(s => s.StartTime).ToList();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // ── Inner classes ─────────────────────────────────────────────────────
        public class CourtDetailItem
        {
            public int CourtId { get; set; }
            public string CourtName { get; set; } = string.Empty;
            public string VenueName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string MinPriceDisplay { get; set; } = string.Empty;
            public List<string> Images { get; set; } = new();
            public TimeSpan OpenTime { get; set; }
            public TimeSpan CloseTime { get; set; }
            public bool HasParking { get; set; }
            public bool HasShower { get; set; }
            public bool HasLocker { get; set; }
            public bool HasFood { get; set; }
        }

        public class BookingInputModel
        {
            public int CourtID { get; set; }
            public DateTime BookingDate { get; set; } = DateTime.Today;
            public List<int> SelectedSlotIDs { get; set; } = new();
            public string PaymentMethod { get; set; } = "BankTransfer"; // khớp DB CHECK constraint
        }

        public class SlotItem
        {
            public int SlotID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string SlotLabel { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public bool IsAvailable { get; set; }
            public bool IsPast { get; set; }
            public string PriceDisplay => $"{Price:N0}đ";
        }
    }
}
