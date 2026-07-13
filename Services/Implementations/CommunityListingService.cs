using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SportHub.Common;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class CommunityListingService : ICommunityListingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiChatService _aiChat;
        private readonly IGeocodingService _geocoding;
        private readonly ILogger<CommunityListingService> _logger;

        public CommunityListingService(ApplicationDbContext context, IAiChatService aiChat,
            IGeocodingService geocoding, ILogger<CommunityListingService> logger)
        {
            _context = context;
            _aiChat = aiChat;
            _geocoding = geocoding;
            _logger = logger;
        }

        public async Task<CommunityListing> IngestAsync(string rawText, string sourceUrl, string? sourceAuthorName,
            int submittedByUserId, CancellationToken ct = default)
        {
            var existing = await _context.CommunityListings.FirstOrDefaultAsync(c => c.SourceUrl == sourceUrl, ct);
            if (existing != null) return existing;

            // Extension chỉ dùng để cào tin cầu lông — không cần AI suy luận môn thể thao
            var badmintonSport = await _context.Sports
                .FirstOrDefaultAsync(s => s.SportName == "Cầu lông", ct);

            var listing = new CommunityListing
            {
                SourceUrl = sourceUrl,
                SourceAuthorName = sourceAuthorName,
                RawText = rawText,
                SubmittedByUserID = submittedByUserId,
                SportID = badmintonSport?.SportID
            };

            var nowVn = VietnamTime.Now;
            var aiResponse = await _aiChat.ChatAsync(BuildSystemPrompt(nowVn), new List<AiChatHistoryItem>(), rawText);
            var parsed = TryParseJson(aiResponse);

            if (parsed != null)
            {
                listing.ParseStatus = "Parsed";
                listing.ParsedJsonRaw = aiResponse;
                listing.Title = string.IsNullOrWhiteSpace(parsed.Title) ? Truncate(rawText, 80) : parsed.Title!.Trim();
                listing.VenueName = parsed.VenueName;
                listing.Address = parsed.Address;
                listing.CostMaleVnd = parsed.CostMaleVnd;
                listing.CostFemaleVnd = parsed.CostFemaleVnd;
                listing.SlotsNeeded = parsed.SlotsNeeded;

                // Ưu tiên danh sách trình độ theo giới tính (đã map đúng thang đo SportHub) — chỉ dùng
                // skillRequired dạng chữ tự do khi AI không map được vào thang đo (fallback hiển thị).
                var composite = BuildSkillComposite(parsed.SkillMale, parsed.SkillFemale);
                listing.SkillRequired = composite ?? (string.IsNullOrWhiteSpace(parsed.SkillRequired) ? null : parsed.SkillRequired!.Trim());

                if (DateOnly.TryParse(parsed.MatchDate, out var matchDate)) listing.MatchDate = matchDate;
                if (TimeSpan.TryParse(parsed.StartTime, out var startTime)) listing.StartTime = startTime;
                if (TimeSpan.TryParse(parsed.EndTime, out var endTime))
                {
                    listing.EndTime = endTime;
                }
                else if (listing.StartTime.HasValue)
                {
                    // Bài chỉ ghi 1 mốc giờ (không nói giờ kết thúc) — mặc định chơi 2 tiếng
                    var defaultEnd = listing.StartTime.Value.Add(TimeSpan.FromHours(2));
                    listing.EndTime = defaultEnd.TotalHours >= 24 ? new TimeSpan(23, 59, 0) : defaultEnd;
                }

                // Geocode theo địa chỉ; nếu bài không tách được địa chỉ riêng thì thử luôn tên sân
                // (nhiều bài viết gộp chung "sân X 194 Bế Văn Đàn" vào 1 cụm).
                var geoSource = !string.IsNullOrWhiteSpace(listing.Address) ? listing.Address : listing.VenueName;
                if (!string.IsNullOrWhiteSpace(geoSource))
                {
                    try
                    {
                        // Tin từ Facebook thường thiếu tên thành phố → geocoder dễ đoán nhầm ra tỉnh khác.
                        // Extension chỉ dùng cho cầu lông Đà Nẵng nên thêm ", Đà Nẵng" làm ngữ cảnh nếu chưa có.
                        var geoQuery = geoSource!;
                        if (!geoQuery.Contains("đà nẵng", StringComparison.OrdinalIgnoreCase)
                            && !geoQuery.Contains("da nang", StringComparison.OrdinalIgnoreCase))
                            geoQuery += ", Đà Nẵng";

                        var geo = await _geocoding.ResolveAsync(geoQuery, ct);
                        if (geo != null)
                        {
                            listing.Latitude = (decimal)geo.Lat;
                            listing.Longitude = (decimal)geo.Lon;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Geocode thất bại cho địa chỉ '{Address}' — bỏ qua, vẫn hiện dạng text.", geoSource);
                    }
                }
            }
            else
            {
                listing.ParseStatus = "Failed";
                listing.ParsedJsonRaw = aiResponse;
                listing.Title = Truncate(rawText, 80);
            }

            listing.ExpiresAt = ComputeExpiresAtUtc(listing, nowVn);

            _context.CommunityListings.Add(listing);
            await _context.SaveChangesAsync(ct);
            return listing;
        }

        // MatchDate/StartTime/EndTime lưu theo giờ Việt Nam (khớp quy ước Match.MatchDate/StartTime/EndTime),
        // nhưng ExpiresAt lưu UTC để so sánh trực tiếp với DateTime.UtcNow (khớp quy ước MatchPayment.ExpiresAt) —
        // nên phải tự trừ 7h khi quy đổi.
        private static DateTime ComputeExpiresAtUtc(CommunityListing listing, DateTime nowVn)
        {
            DateTime vnExpiry;
            if (listing.MatchDate.HasValue && listing.EndTime.HasValue)
                vnExpiry = listing.MatchDate.Value.ToDateTime(TimeOnly.FromTimeSpan(listing.EndTime.Value)).AddHours(2);
            else if (listing.MatchDate.HasValue)
                vnExpiry = listing.MatchDate.Value.ToDateTime(new TimeOnly(23, 59));
            else
                vnExpiry = nowVn.AddHours(48);

            return vnExpiry.AddHours(-7);
        }

        private static ParsedListingDto? TryParseJson(string aiResponse)
        {
            try
            {
                var match = Regex.Match(aiResponse, @"\{[\s\S]*\}");
                if (!match.Success) return null;
                return JsonSerializer.Deserialize<ParsedListingDto>(match.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private static string Truncate(string text, int maxLen) =>
            text.Length <= maxLen ? text : text[..maxLen] + "…";

        // Thang đo trình độ cầu lông của SportHub (thấp → cao) — khớp đúng danh sách checkbox
        // "Trình Nam"/"Trình Nữ" trong Matchmaking/Create.cshtml.
        private static readonly string[] BadmintonSkillVocab =
            { "Newbie", "Yếu", "Yếu+", "TBY/TB-", "Trung Bình", "TB+/Khá" };

        private static string? NormalizeSkillTokens(List<string>? tokens)
        {
            if (tokens == null) return null;
            var valid = tokens
                .Select(t => BadmintonSkillVocab.FirstOrDefault(v => string.Equals(v, t?.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(v => v != null)
                .Distinct()
                .ToList();
            return valid.Count == 0 ? null : string.Join(",", valid);
        }

        // Lưu theo đúng format Match.SkillRequired ("Nam:X,Y|Nữ:Z") để tái dùng được MatchesSkillFilter
        // và hiển thị nhất quán với trận thật.
        private static string? BuildSkillComposite(List<string>? male, List<string>? female)
        {
            var maleStr = NormalizeSkillTokens(male);
            var femaleStr = NormalizeSkillTokens(female);
            var parts = new List<string>();
            if (maleStr != null) parts.Add($"Nam:{maleStr}");
            if (femaleStr != null) parts.Add($"Nữ:{femaleStr}");
            return parts.Count == 0 ? null : string.Join("|", parts);
        }

        private static string BuildSystemPrompt(DateTime nowVn) => $$"""
            Bạn là trợ lý bóc tách dữ liệu từ bài đăng tuyển người chơi cầu lông vãng lai trên Facebook (khu vực Đà Nẵng),
            điền vào đúng các trường của form tạo trận cầu lông trên SportHub.
            Hôm nay là {{nowVn:dddd, dd/MM/yyyy}} (giờ Việt Nam). Dùng ngày này để suy ra ngày cụ thể khi bài viết ghi tương đối ("tối nay", "thứ 4 tới"...).

            THANG ĐO TRÌNH ĐỘ CẦU LÔNG CỦA SPORTHUB (chỉ dùng đúng các giá trị sau, xếp từ thấp đến cao):
            "Newbie" < "Yếu" < "Yếu+" < "TBY/TB-" < "Trung Bình" < "TB+/Khá"
            Quy ước viết tắt thường gặp trên Facebook:
            - "mới", "newbie" → Newbie
            - "y", "yếu" (không kèm +/-) → Yếu
            - "y+", "yếu+" → Yếu+
            - "tby", "tb-" → TBY/TB-
            - "tb", "trung bình" (không kèm y hay +) → Trung Bình
            - "tb+", "khá" → TB+/Khá
            Nếu bài ghi khoảng/dải trình độ (VD "TBY trở lên", "dưới trung bình") thì LIỆT KÊ ĐẦY ĐỦ mọi mức trong khoảng đó theo thang đo trên, không chỉ ghi 1 mức.
            Nếu bài chỉ ghi trình độ chung không phân biệt Nam/Nữ, hãy áp dụng danh sách đó cho CẢ HAI giới.
            Nếu bài chỉ tuyển 1 giới (VD chỉ tuyển nữ) thì để mảng của giới còn lại là null — đừng bịa ra.

            Trả lời DUY NHẤT một object JSON (không kèm giải thích, không dùng markdown), đúng các khóa sau (giá trị không tìm thấy thì để null):
            {"title":string,"venueName":string,"address":string,"matchDate":"yyyy-MM-dd","startTime":"HH:mm","endTime":"HH:mm","skillMale":string[],"skillFemale":string[],"skillRequired":string,"costMaleVnd":number,"costFemaleVnd":number,"slotsNeeded":number}

            Lưu ý khi tách "venueName" và "address": venueName là TÊN RIÊNG của sân (VD "Sân Trọng Nghĩa"), address là địa chỉ cụ thể để tìm trên bản đồ (số nhà + tên đường). Nếu bài viết gộp chung thành 1 cụm kiểu "sân trọng nghĩa 194 Bế Văn Đàn", hãy TÁCH RIÊNG hai phần này chứ không gộp cả cụm vào 1 trường. "skillRequired" chỉ dùng khi không thể map trình độ vào thang đo ở trên (câu chữ mơ hồ) — để làm dự phòng hiển thị dạng chữ.

            Ví dụ 1 — đầu vào: "Tuyển vãng lai cầu lông tối nay T4 ngày 8/7, 19h-21h. Sân Trung tâm TDTT Quốc phòng 3 - số 07 Duy Tân. Phí nam 45k, nữ 35k. Cần thêm 2 nam."
            Đầu ra: {"title":"Tuyển vãng lai cầu lông tối nay","venueName":"Sân Trung tâm TDTT Quốc phòng 3","address":"Số 07 Duy Tân","matchDate":"2026-07-08","startTime":"19:00","endTime":"21:00","skillMale":null,"skillFemale":null,"skillRequired":null,"costMaleVnd":45000,"costFemaleVnd":35000,"slotsNeeded":2}

            Ví dụ 2 — đầu vào: "Cần tuyển vãng lai đánh cầu lông các buổi T3,5,7 lúc 18h30, trình độ TBY trở lên, sân Nguyễn Tri Phương."
            Đầu ra: {"title":"Tuyển vãng lai cầu lông T3-5-7","venueName":"Sân Nguyễn Tri Phương","address":null,"matchDate":null,"startTime":"18:30","endTime":null,"skillMale":["TBY/TB-","Trung Bình","TB+/Khá"],"skillFemale":["TBY/TB-","Trung Bình","TB+/Khá"],"skillRequired":null,"costMaleVnd":null,"costFemaleVnd":null,"slotsNeeded":null}

            Ví dụ 3 — đầu vào: "Team vẫn còn thiếu người nên tuyển thêm 2 vãng lai ngày mai 11-7 sân trọng nghĩa 194 Bế Văn Đàn 15:00-17:00. 2 nữ trình y-/y+/tby. cầu pro x. phí chia đều cuối buổi."
            Đầu ra: {"title":"Tuyển thêm 2 vãng lai ngày mai","venueName":"Sân Trọng Nghĩa","address":"194 Bế Văn Đàn","matchDate":"2026-07-11","startTime":"15:00","endTime":"17:00","skillMale":null,"skillFemale":["Yếu","Yếu+","TBY/TB-"],"skillRequired":null,"costMaleVnd":null,"costFemaleVnd":null,"slotsNeeded":2}
            """;

        private class ParsedListingDto
        {
            [JsonPropertyName("title")] public string? Title { get; set; }
            [JsonPropertyName("venueName")] public string? VenueName { get; set; }
            [JsonPropertyName("address")] public string? Address { get; set; }
            [JsonPropertyName("matchDate")] public string? MatchDate { get; set; }
            [JsonPropertyName("startTime")] public string? StartTime { get; set; }
            [JsonPropertyName("endTime")] public string? EndTime { get; set; }
            [JsonPropertyName("skillMale")] public List<string>? SkillMale { get; set; }
            [JsonPropertyName("skillFemale")] public List<string>? SkillFemale { get; set; }
            [JsonPropertyName("skillRequired")] public string? SkillRequired { get; set; }
            [JsonPropertyName("costMaleVnd")] public decimal? CostMaleVnd { get; set; }
            [JsonPropertyName("costFemaleVnd")] public decimal? CostFemaleVnd { get; set; }
            [JsonPropertyName("slotsNeeded")] public int? SlotsNeeded { get; set; }
        }
    }
}
