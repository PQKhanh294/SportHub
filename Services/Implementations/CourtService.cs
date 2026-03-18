using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class CourtService : ICourtService
    {
        private readonly ApplicationDbContext _context;

        public CourtService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CourtVenue>> GetNearbyVenuesAsync(double latitude, double longitude, int limit = 3)
        {
            // Trong thực tế sẽ dùng công thức Haversine để tính khoảng cách
            // Demo logic: Trả về random sân đang active
            return await _context.CourtVenues
                .Include(v => v.Courts).ThenInclude(c => c.Sport)
                .Include(v => v.Courts).ThenInclude(c => c.Images)
                .Include(v => v.Courts).ThenInclude(c => c.PricingRules)
                .Where(v => v.IsActive)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Court?> GetCourtDetailsAsync(int courtId)
        {
            return await _context.Courts
                .Include(c => c.Venue)
                .Include(c => c.Images)
                .Include(c => c.PricingRules)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.CourtID == courtId);
        }

        public async Task<List<Court>> SearchCourtsAsync(string query, int? sportId)
        {
            var courts = _context.Courts
                .Include(c => c.Venue)
                .Include(c => c.Sport)
                .Include(c => c.Images)
                .Include(c => c.PricingRules)
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(query))
            {
                courts = courts.Where(c => c.CourtName.Contains(query) || c.Venue.VenueName.Contains(query));
            }

            if (sportId.HasValue)
            {
                courts = courts.Where(c => c.SportID == sportId.Value);
            }

            return await courts.ToListAsync();
        }

        public async Task<List<TimeSlot>> GetAvailableSlotsAsync(int courtId, DateTime targetDate)
        {
            // Logic tìm Slot trống cơ bản
            var allSlots = await _context.TimeSlots.ToListAsync();
            var bookedSlots = await _context.BookingSlots
                .Include(bs => bs.Booking)
                .Where(bs => bs.Booking.CourtID == courtId 
                          && bs.Booking.BookingDate.Date == targetDate.Date
                          && bs.Booking.Status != "Cancelled")
                .Select(bs => bs.SlotID)
                .ToListAsync();

            return allSlots.Where(s => !bookedSlots.Contains(s.SlotID)).ToList();
        }
    }
}
