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
            var dbg = $"[SVC {DateTime.Now:HH:mm:ss}]";
            Console.WriteLine($"{dbg} ===== CreateBookingAsync BẮT ĐẦU =====");
            Console.WriteLine($"{dbg}   userId={userId}, courtId={courtId}, date={date:yyyy-MM-dd}, slots=[{string.Join(",", slotIds)}], method={paymentMethod}");

            // Bước 1: Transaction
            Console.WriteLine($"{dbg} BƯỚC 1: Mở transaction...");
            using var transaction = await _context.Database.BeginTransactionAsync();
            Console.WriteLine($"{dbg} ✅ Transaction opened");

            try
            {
                // Bước 2: Kiểm tra slot trống
                Console.WriteLine($"{dbg} BƯỚC 2: Kiểm tra slot bị đặt rồi...");
                var existingBookings = await _context.BookingSlots
                    .Include(bs => bs.Booking)
                    .Where(bs => bs.Booking.CourtID == courtId 
                              && bs.Booking.BookingDate.Date == date.Date
                              && bs.Booking.Status != "Cancelled"
                              && slotIds.Contains(bs.SlotID))
                    .ToListAsync();

                Console.WriteLine($"{dbg}   Slot đã đặt tìm thấy: {existingBookings.Count}");
                if (existingBookings.Any())
                {
                    Console.WriteLine($"{dbg} ❌ Slot bị trùng: [{string.Join(",", existingBookings.Select(x => x.SlotID))}]");
                    throw new InvalidOperationException("One or more selected slots are already booked.");
                }

                // Bước 3: Tính giá
                Console.WriteLine($"{dbg} BƯỚC 3: Query PricingRules...");
                decimal totalAmount = 0;
                var pricingRules = await _context.PricingRules
                    .Where(p => p.CourtID == courtId && slotIds.Contains(p.SlotID))
                    .ToListAsync();
                    
                string currentDayType = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? "Weekend" : "Weekday";
                Console.WriteLine($"{dbg}   DayType={currentDayType}, PricingRules tìm thấy: {pricingRules.Count}");
                
                var createdBookingSlots = new List<BookingSlot>();
                foreach (var slotId in slotIds)
                {
                    var price = pricingRules.FirstOrDefault(p => p.SlotID == slotId && p.DayType == currentDayType)?.UnitPrice ?? 100000;
                    Console.WriteLine($"{dbg}   Slot {slotId} → giá {price:N0} xu ({(pricingRules.Any(p => p.SlotID == slotId) ? "có rule" : "dùng default")})");
                    totalAmount += price;
                    
                    createdBookingSlots.Add(new BookingSlot
                    {
                        SlotID = slotId,
                        UnitPrice = price
                    });
                }
                Console.WriteLine($"{dbg}   TotalAmount = {totalAmount:N0} xu");

                // Bước 4: Tạo Booking
                Console.WriteLine($"{dbg} BƯỚC 4: Tạo Booking entity...");
                var booking = new Booking
                {
                    UserID = userId,
                    CourtID = courtId,
                    BookingDate = date,
                    TotalAmount = totalAmount,
                    FinalAmount = totalAmount,
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

                Console.WriteLine($"{dbg}   Booking entity tạo xong. PaymentMethod='{paymentMethod}'");

                // Bước 5: Save
                Console.WriteLine($"{dbg} BƯỚC 5: SaveChangesAsync...");
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{dbg} ✅ SaveChanges OK — BookingID={booking.BookingID}");

                // Bước 6: Commit
                Console.WriteLine($"{dbg} BƯỚC 6: CommitAsync...");
                await transaction.CommitAsync();
                Console.WriteLine($"{dbg} ✅ Commit OK — Booking {booking.BookingID} hoàn tất!");

                return booking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{dbg} ❌ EXCEPTION trong CreateBookingAsync: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"{dbg}   InnerException: {ex.InnerException.Message}");
                Console.WriteLine($"{dbg} ⏪ RollbackAsync...");
                await transaction.RollbackAsync();
                Console.WriteLine($"{dbg} ✅ Rollback xong. Re-throw exception.");
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
