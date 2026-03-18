using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Services.Implementations
{
    public class BookingService : Interfaces.IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> CreateBookingAsync(int userId, int courtId, DateTime date, List<int> slotIds, string paymentMethod)
        {
            // Bước 1: Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bước 2: Kiểm tra slot trống
                var existingBookings = await _context.BookingSlots
                    .Include(bs => bs.Booking)
                    .Where(bs => bs.Booking.CourtID == courtId 
                              && bs.Booking.BookingDate.Date == date.Date
                              && bs.Booking.Status != "Cancelled"
                              && slotIds.Contains(bs.SlotID))
                    .ToListAsync();

                if (existingBookings.Any())
                {
                    throw new InvalidOperationException("One or more selected slots are already booked.");
                }

                // Bước 3: Tính giá
                decimal totalAmount = 0;
                var pricingRules = await _context.PricingRules
                    .Where(p => p.CourtID == courtId && slotIds.Contains(p.SlotID))
                    .ToListAsync();
                    
                // (Giả sử logic dayType đơn giản)
                string currentDayType = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? "Weekend" : "Weekday";
                
                var createdBookingSlots = new List<BookingSlot>();
                foreach (var slotId in slotIds)
                {
                    var price = pricingRules.FirstOrDefault(p => p.SlotID == slotId && p.DayType == currentDayType)?.UnitPrice ?? 100000;
                    totalAmount += price;
                    
                    createdBookingSlots.Add(new BookingSlot
                    {
                        SlotID = slotId,
                        UnitPrice = price
                    });
                }

                // Bước 4: Tạo Booking
                var booking = new Booking
                {
                    UserID = userId,
                    CourtID = courtId,
                    BookingDate = date,
                    TotalAmount = totalAmount,
                    FinalAmount = totalAmount, // Bỏ qua discount tạm thời
                    Status = "Pending",
                    BookedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    BookingSlots = createdBookingSlots,
                    Payments = new List<Payment>
                    {
                        new Payment
                        {
                            Amount = totalAmount,
                            PaymentMethod = paymentMethod,
                            Status = "Pending",
                            PaymentType = "Payment",
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return booking;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Venue)
                .Include(b => b.Court).ThenInclude(c => c.Images)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.TimeSlot)
                .Include(b => b.Payments)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<bool> CancelBookingAsync(int bookingId, int userId, string reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

            if (booking == null || booking.Status == "Cancelled" || booking.Status == "Completed")
                return false;

            booking.Status = "Cancelled";
            booking.CancelReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            // Xử lý hoàn tiền nểu payment status là success (logic demo đơn giản)
            var payment = booking.Payments.FirstOrDefault(p => p.Status == "Success");
            if (payment != null)
            {
                payment.Status = "Refunded"; 
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
