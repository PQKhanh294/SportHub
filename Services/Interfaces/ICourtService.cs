using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface ICourtService
    {
        Task<List<CourtVenue>> GetNearbyVenuesAsync(double latitude, double longitude, int limit = 3);
        Task<Court?> GetCourtDetailsAsync(int courtId);
        Task<List<Court>> SearchCourtsAsync(string query, int? sportId);
        Task<List<TimeSlot>> GetAvailableSlotsAsync(int courtId, DateTime targetDate);
    }
}
