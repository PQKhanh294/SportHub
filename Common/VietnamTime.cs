namespace SportHub.Common
{
    // SQL Server datetime columns không lưu thông tin timezone — EF Core đọc lại luôn có Kind=Unspecified,
    // nên .ToLocalTime()/DateTime.Now phụ thuộc timezone của hệ điều hành server (mặc định UTC trên container
    // khi deploy), gây lệch giờ hiển thị so với giờ Việt Nam thực tế. Dùng VietnamTime.Now và .ToVietnamTime()
    // thay thế để luôn ra đúng giờ Việt Nam (UTC+7) bất kể server chạy timezone gì.
    public static class VietnamTime
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

        public static DateTime Now => DateTime.UtcNow.Add(Offset);

        public static DateTime ToVietnamTime(this DateTime utcValue) => utcValue.Add(Offset);
    }
}
