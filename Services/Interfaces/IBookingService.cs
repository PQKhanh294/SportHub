using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(int userId, int courtId, DateTime date, List<int> slotIds, string paymentMethod);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<bool> CancelBookingAsync(int bookingId, int userId, string reason);
    }
}
