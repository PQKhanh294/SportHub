using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Localization;

namespace SportHub.Middleware
{
    public class UiLocalizationMiddleware
    {
        private readonly RequestDelegate _next;

        public UiLocalizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await _next(context);
            }
            catch
            {
                context.Response.Body = originalBody;
                throw;
            }

            context.Response.Body = originalBody;

            if (!IsHtmlResponse(context.Response))
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                return;
            }

            buffer.Position = 0;
            using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var html = await reader.ReadToEndAsync();

            var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture
                ?? CultureInfo.CurrentUICulture;

            var localized = LocalizeHtml(html, culture);
            var output = Encoding.UTF8.GetBytes(localized);

            context.Response.ContentLength = output.Length;
            await originalBody.WriteAsync(output);
        }

        private static bool IsHtmlResponse(HttpResponse response)
        {
            var contentType = response.ContentType;
            return response.StatusCode == StatusCodes.Status200OK
                && !string.IsNullOrWhiteSpace(contentType)
                && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        }

        private static string LocalizeHtml(string html, CultureInfo culture)
        {
            var isEnglish = culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            var parts = System.Text.RegularExpressions.Regex.Split(html, "(<[^>]+>)");
            var result = new StringBuilder(html.Length);
            var skipText = false;

            foreach (var part in parts)
            {
                if (part.StartsWith("<", StringComparison.Ordinal))
                {
                    var tag = part.TrimStart('<', '/', ' ', '\t', '\r', '\n')
                        .Split(new[] { ' ', '>', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();

                    if (tag != null && (tag.Equals("script", StringComparison.OrdinalIgnoreCase)
                        || tag.Equals("style", StringComparison.OrdinalIgnoreCase)))
                    {
                        skipText = !part.StartsWith("</", StringComparison.Ordinal);
                    }

                    result.Append(part);
                    continue;
                }

                result.Append(skipText ? part : TranslateText(part, isEnglish));
            }

            return result.ToString();
        }

        private static string TranslateText(string text, bool isEnglish)
        {
            var result = text;

            foreach (var entry in Entries.OrderByDescending(e => Math.Max(e.Vi.Length, e.En.Length)))
            {
                if (isEnglish)
                {
                    result = result.Replace(entry.Vi, entry.En, StringComparison.Ordinal);
                    foreach (var legacy in entry.LegacyVi)
                    {
                        result = result.Replace(legacy, entry.En, StringComparison.Ordinal);
                    }
                }
                else
                {
                    result = result.Replace(entry.En, entry.Vi, StringComparison.Ordinal);
                    foreach (var legacy in entry.LegacyVi)
                    {
                        result = result.Replace(legacy, entry.Vi, StringComparison.Ordinal);
                    }
                }
            }

            result = result.Replace("Môn thể thao Hub", "Sport Hub", StringComparison.Ordinal);
            result = result.Replace("Môn thể thaoHub", "SportHub", StringComparison.Ordinal);
            return result;
        }

        private sealed record TextEntry(string Vi, string En, params string[] LegacyVi);

        private static readonly IReadOnlyList<TextEntry> Entries = new List<TextEntry>
        {
            new("Trang chủ", "Home"),
            new("Ghép trận", "Matchmaking"),
            new("Ghép cặp", "Matchmaking", "Gh&#xE9;p c&#x1EB7;p"),
            new("Cộng đồng", "Community"),
            new("Cá nhân", "Profile"),
            new("Cài đặt", "Settings"),
            new("Bạn bè", "Friends"),
            new("Tin nhắn", "Messages"),
            new("Thông báo", "Notifications"),
            new("Tạo kèo", "Create match"),
            new("Tìm kèo ngay", "Find match now"),
            new("Đăng nhập", "Log in"),
            new("Đăng ký", "Register"),
            new("Đăng xuất", "Log out"),
            new("Mở menu", "Open menu"),
            new("Về chúng tôi", "About us"),
            new("Quy tắc cộng đồng", "Community rules"),
            new("Chính sách bảo mật", "Privacy policy"),
            new("Điều khoản sử dụng", "Terms of use"),
            new("Hỗ trợ", "Support"),
            new("Xem chi tiết", "View details"),
            new("Đóng", "Close"),
            new("Lưu", "Save"),
            new("Hủy", "Cancel"),
            new("Xóa", "Clear"),
            new("Tìm kiếm", "Search"),
            new("Lọc ngay", "Apply filters"),
            new("Bộ lọc", "Filters", "Bá»™ lá»c"),
            new("Cơ bản", "Basic", "CÆ¡ báº£n"),
            new("Thời gian", "Time", "Thá»i gian"),
            new("Khu vực", "Area", "Khu vá»±c"),
            new("Chi phí", "Cost", "Chi phÃ­"),
            new("Trạng thái kèo", "Match status", "Tráº¡ng thÃ¡i kÃ¨o"),
            new("Tất cả trạng thái", "All statuses", "Táº¥t cáº£ tráº¡ng thÃ¡i"),
            new("Còn slot", "Open slots", "CÃ²n slot"),
            new("Sắp đầy", "Almost full", "Sáº¯p Ä‘áº§y"),
            new("Sắp diễn ra", "Upcoming soon", "Sáº¯p diá»…n ra"),
            new("Đang chờ duyệt", "Pending approval", "Äang chá» duyá»‡t"),
            new("Đã tham gia", "Joined", "ÄÃ£ tham gia"),
            new("Kèo của tôi", "My matches", "KÃ¨o cá»§a tÃ´i"),
            new("Môn thể thao", "Sport", "MÃ´n thá»ƒ thao"),
            new("Tất cả", "All", "Táº¥t cáº£"),
            new("Cầu Lông", "Badminton", "Cáº§u LÃ´ng"),
            new("Bóng đá", "Football", "BÃ³ng Ä‘Ã¡"),
            new("Bóng bàn", "Table tennis", "BÃ³ng bÃ n"),
            new("Trình độ", "Skill level", "TrÃ¬nh Ä‘á»™"),
            new("Tất cả trình độ", "All skill levels", "Táº¥t cáº£ trÃ¬nh Ä‘á»™"),
            new("Mọi trình độ", "Any skill level", "Má»i trÃ¬nh Ä‘á»™"),
            new("Yếu+", "Weak+"),
            new("Yếu", "Weak"),
            new("Khá", "Good"),
            new("Người mới", "Beginner", "NgÆ°á»i má»›i"),
            new("Trung bình", "Intermediate", "Trung bÃ¬nh"),
            new("Nâng cao", "Advanced", "NÃ¢ng cao"),
            new("Chuyên nghiệp", "Professional", "ChuyÃªn nghiá»‡p"),
            new("Khung giờ", "Time range", "Khung giá»"),
            new("Ngày đánh", "Match date", "NgÃ y Ä‘Ã¡nh"),
            new("Hôm nay", "Today", "HÃ´m nay"),
            new("Ngày mai", "Tomorrow", "NgÃ y mai"),
            new("Cuối tuần", "Weekend", "Cuá»‘i tuáº§n"),
            new("Đang lọc", "Filtering", "Äang lá»c"),
            new("Khu vực đánh", "Playing area", "Khu vá»±c Ä‘Ã¡nh"),
            new("Nhập địa chỉ chi tiết...", "Enter a detailed address...", "Nháº­p Ä‘á»‹a chá»‰ chi tiáº¿t..."),
            new("Tỉnh/TP", "Province/City", "Tá»‰nh/TP"),
            new("Quận/Huyện", "District", "Quáº­n/Huyá»‡n"),
            new("Đà Nẵng", "Da Nang", "ÄÃ  Náºµng"),
            new("Hồ Chí Minh", "Ho Chi Minh City", "Há»“ ChÃ­ Minh"),
            new("Hà Nội", "Ha Noi", "HÃ  Ná»™i"),
            new("Khoảng giá", "Price range", "Khoáº£ng giÃ¡"),
            new("Từ", "From", "Tá»«"),
            new("Đến", "To", "Äáº¿n"),
            new("Tự động ưu tiên gần đến xa", "Automatically sorted from nearest to farthest"),
            new("SportHub dùng vị trí trong hồ sơ hoặc GPS để xếp kèo gần bạn lên trước. Không cần chọn bán kính thủ công.", "SportHub uses your profile location or GPS to show nearby matches first. No manual radius needed."),
            new("Đang phát hiện vị trí...", "Detecting location...", "Äang phÃ¡t hiá»‡n vá»‹ trÃ­..."),
            new("Bật định vị hoặc cập nhật địa chỉ trong Hồ sơ", "Enable location or update your profile address"),
            new("Vị trí hiện tại (GPS)", "Current location (GPS)"),
            new("Vị trí đã ghim (hồ sơ)", "Pinned profile location"),
            new("Địa chỉ hồ sơ", "Profile address"),
            new("Đang tính...", "Calculating...", "Äang tÃ­nh..."),
            new("Chưa có giá", "No price yet", "ChÆ°a cÃ³ giÃ¡"),
            new("Tổng", "Total", "Tá»•ng"),
            new("giờ", "hour", "giá»"),
            new("Gửi yêu cầu", "Send request"),
            new("Bỏ qua", "Skip", "Bá» qua"),
            new("Đã gửi yêu cầu", "Request sent"),
            new("Đã bỏ qua trận này. Feed của bạn sẽ gọn hơn.", "Skipped this match. Your feed will be cleaner."),
            new("Không thể bỏ qua trận đấu này.", "Unable to skip this match."),
            new("Yêu cầu đã gửi. Bạn đang trong hàng chờ - host sẽ duyệt trong vòng 2 giờ.", "Request sent. You are in the queue - the host will review it within 2 hours."),
            new("Có người muốn tham gia trận", "Someone wants to join the match"),
            new("Không thể tham gia trận đấu. Kiểm tra lại trình độ hoặc trận đấu đã đầy/đóng.", "Unable to join this match. Check your skill level or the match may be full/closed."),
            new("Trận đấu sắp tới", "Upcoming matches", "Tráº­n Ä‘áº¥u sáº¯p tá»›i"),
            new("Lịch sử trận đấu", "Match history", "Lá»‹ch sá»­ tráº­n Ä‘áº¥u"),
            new("Thống kê", "Stats", "Thá»‘ng kÃª"),
            new("Trận sắp tới", "Upcoming matches", "Tráº­n sáº¯p tá»›i"),
            new("Trận đã chơi", "Played matches", "Tráº­n Ä‘Ã£ chÆ¡i"),
            new("Chưa có trận đấu sắp tới.", "No upcoming matches yet.", "ChÆ°a cÃ³ tráº­n Ä‘áº¥u sáº¯p tá»›i."),
            new("Chưa có lịch sử trận đấu.", "No match history yet.", "ChÆ°a cÃ³ lá»‹ch sá»­ tráº­n Ä‘áº¥u."),
            new("Bản đồ", "Map", "Báº£n Ä‘á»“"),
            new("Tạo trận mới", "Create new match", "Táº¡o tráº­n má»›i"),
            new("Chi tiết", "Details", "Chi tiáº¿t"),
            new("Không rõ địa điểm", "Unknown location", "KhÃ´ng rÃµ Ä‘á»‹a Ä‘iá»ƒm"),
            new("Trận đấu", "Match", "Tráº­n Ä‘áº¥u"),
            new("Người chơi", "Player", "NgÆ°á»i chÆ¡i"),
            new("Người tham gia", "Participants"),
            new("Chủ kèo", "Host"),
            new("Địa điểm", "Venue"),
            new("Giá", "Price", "GiÃ¡"),
            new("Không có trận nào phù hợp", "No matching matches found"),
            new("Không tìm thấy dữ liệu", "No data found"),
            new("Cập nhật", "Update"),
            new("Chỉnh sửa", "Edit"),
            new("Xác nhận", "Confirm"),
            new("Thanh toán", "Payment"),
            new("Lịch đặt", "Bookings"),
            new("Đặt sân", "Book court"),
            new("Sân", "Court"),
            new("Sân gần đây", "Nearby courts"),
            new("Đã hủy", "Cancelled"),
            new("Hoàn thành", "Completed"),
            new("Sắp tới", "Upcoming"),
            new("Đang chờ", "Pending"),
            new("Bạn chưa có lịch đặt nào.", "You do not have any bookings yet."),
            new("Hồ sơ", "Profile"),
            new("Chỉnh sửa hồ sơ", "Edit profile"),
            new("Chưa cập nhật", "Not updated", "ChÆ°a cáº­p nháº­t"),
            new("Ngày tham gia", "Joined date"),
            new("Số trận", "Matches"),
            new("Chiến thắng", "Wins"),
            new("Huy hiệu", "Badges"),
            new("Giao diện", "Theme", "Giao diá»‡n"),
            new("Tùy chỉnh trải nghiệm của bạn trên Sport Hub.", "Customize your Sport Hub experience."),
            new("Chọn chế độ sáng hoặc tối cho ứng dụng.", "Choose light or dark mode for the app."),
            new("Chế độ sáng", "Light mode"),
            new("Chế độ tối", "Dark mode"),
            new("Đang sử dụng", "Active"),
            new("Ngôn ngữ", "Language"),
            new("Chọn ngôn ngữ hiển thị chính.", "Choose the main display language."),
            new("Tiếng Việt", "Vietnamese"),
            new("Tiếng Việt (Việt Nam)", "Vietnamese (Vietnam)"),
            new("Tạo tài khoản", "Create account"),
            new("Chào mừng trở lại", "Welcome back"),
            new("Email", "Email"),
            new("Mật khẩu", "Password"),
            new("Họ và tên", "Full name"),
            new("Xác nhận mật khẩu", "Confirm password"),
            new("Bạn đã có tài khoản?", "Already have an account?"),
            new("Chưa có tài khoản?", "Don't have an account?"),
            new("Đăng nhập ngay", "Log in now"),
            new("Đăng ký ngay", "Register now"),
            new("Tạo cộng đồng chơi thể thao của bạn", "Create your sports community"),
            new("Kết nối người chơi, tìm trận phù hợp và ra sân nhanh hơn.", "Connect with players, find suitable matches, and get on court faster."),
            new("Đăng nhập thành công!", "Logged in successfully!"),
            new("Đăng xuất thành công!", "Logged out successfully!"),
            new("Đăng ký thành công! Vui lòng đăng nhập.", "Registration successful! Please log in."),
            new("Email hoặc mật khẩu không đúng.", "Invalid email or password."),
            new("Email đã được sử dụng.", "Email is already in use."),
            new("Vui lòng nhập email.", "Please enter your email."),
            new("Email không hợp lệ.", "Invalid email address."),
            new("Vui lòng nhập mật khẩu.", "Please enter your password."),
            new("Mật khẩu phải có ít nhất 6 ký tự.", "Password must be at least 6 characters."),
            new("Mật khẩu xác nhận không khớp.", "Passwords do not match."),
            new("Vui lòng nhập họ tên.", "Please enter your full name."),
            new("Tất cả", "All"),
            new("Tất cả bạn bè", "All friends"),
            new("Lời mời kết bạn", "Friend requests"),
            new("Gửi lời mời", "Send request"),
            new("Chấp nhận", "Accept"),
            new("Từ chối", "Decline"),
            new("Kết bạn", "Add friend"),
            new("Đã là bạn bè", "Already friends"),
            new("Tìm người chơi", "Find players"),
            new("Không có thông báo nào.", "No notifications."),
            new("Đánh dấu đã đọc", "Mark as read"),
            new("Đọc tất cả", "Read all"),
            new("Gửi", "Send"),
            new("Nhập tin nhắn...", "Type a message..."),
            new("Chọn cuộc trò chuyện", "Select a conversation"),
            new("Đề xuất đặt sân", "Court booking proposal"),
            new("Chia tiền", "Split payment"),
            new("Tôi thanh toán", "I will pay"),
            new("Đồng ý", "Agree"),
            new("Không đồng ý", "Disagree"),
        };
    }
}
