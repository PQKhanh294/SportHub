using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Bookings
{
    [Authorize]
    public class MyBookingsModel : PageModel
    {
        private readonly IBookingService _bookingService;

        public MyBookingsModel(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public List<BookingItemViewModel> Items { get; set; } = new();
        public string ActiveTab { get; set; } = "all";

        public async Task OnGetAsync(string? tab)
        {
            ViewData["ActivePage"] = "Bookings";
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLowerInvariant();

            var userId = GetCurrentUserId();
            var bookings = await _bookingService.GetUserBookingsAsync(userId);

            Items = bookings
                .Select(b => new BookingItemViewModel
                {
                    BookingId = b.BookingID,
                    CourtId = b.CourtID,
                    VenueName = b.Court.Venue.VenueName,
                    BookingDate = b.BookingDate,
                    TimeDisplay = BuildTimeDisplay(b.BookingSlots),
                    TotalPriceDisplay = $"${b.FinalAmount:N2}",
                    Status = b.Status,
                    ImageUrl = b.Court.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                               ?? b.Court.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                               ?? "https://images.unsplash.com/photo-1542144582-1ba00456b5e3?q=80&w=1200&auto=format&fit=crop",
                    RefundDisplay = b.Payments.Any(p => p.Status == "Refunded")
                        ? $"Refund Processed: ${b.Payments.Where(p => p.Status == "Refunded").Sum(p => p.Amount):N2}"
                        : null
                })
                .ToList();

            Items = ActiveTab switch
            {
                "upcoming" => Items.Where(i => i.Status is "Pending" or "Confirmed").ToList(),
                "completed" => Items.Where(i => i.Status == "Completed").ToList(),
                "cancelled" => Items.Where(i => i.Status == "Cancelled").ToList(),
                _ => Items
            };
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookingId, string? tab)
        {
            var userId = GetCurrentUserId();
            var cancelled = await _bookingService.CancelBookingAsync(bookingId, userId, "Cancelled by user");

            TempData[cancelled ? "SuccessMessage" : "ErrorMessage"] = cancelled
                ? "Booking cancelled successfully."
                : "Unable to cancel this booking.";

            return RedirectToPage(new { tab = string.IsNullOrWhiteSpace(tab) ? "all" : tab });
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }

        private static string BuildTimeDisplay(IEnumerable<Models.Entities.BookingSlot> slots)
        {
            var ordered = slots
                .Where(s => s.TimeSlot != null)
                .OrderBy(s => s.TimeSlot.StartTime)
                .ToList();

            if (ordered.Count == 0)
            {
                return "No time slots";
            }

            var start = ordered.First().TimeSlot.StartTime;
            var end = ordered.Last().TimeSlot.EndTime;
            return $"{start:hh\\:mm} - {end:hh\\:mm}";
        }

        public class BookingItemViewModel
        {
            public int BookingId { get; set; }
            public int CourtId { get; set; }
            public string VenueName { get; set; } = string.Empty;
            public DateTime BookingDate { get; set; }
            public string TimeDisplay { get; set; } = string.Empty;
            public string TotalPriceDisplay { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public string? RefundDisplay { get; set; }

            public string DisplayStatus => Status switch
            {
                "Pending" => "Pending",
                "Confirmed" => "Confirmed",
                "Cancelled" => "Cancelled",
                "Completed" => "Completed",
                _ => Status
            };

            public string StatusBadgeClass => Status switch
            {
                "Pending" => "bg-amber-100 dark:bg-amber-900/30 text-amber-800 dark:text-amber-300",
                "Confirmed" => "bg-emerald-100 dark:bg-emerald-900/30 text-emerald-800 dark:text-emerald-300",
                "Cancelled" => "bg-rose-100 dark:bg-rose-900/30 text-rose-800 dark:text-rose-300",
                "Completed" => "bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300",
                _ => "bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300"
            };

            public bool CanCancel => Status is "Pending" or "Confirmed";
            public bool IsCancelled => Status == "Cancelled";
        }
    }
}
