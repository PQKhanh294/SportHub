using System.Text.RegularExpressions;

namespace SportHub.Common
{
    public static class PhoneValidator
    {
        // Đầu số di động VN hợp lệ (Viettel/Vina/Mobi/Vietnamobile/Gmobile/iTel) — 10 số, bắt đầu bằng 0.
        // Chặn được các chuỗi rác kiểu "0000000000" hay số không đúng đầu số nhà mạng nào.
        private static readonly Regex VietnamMobileRegex = new(
            @"^0(3[2-9]|5[25689]|7[06789]|8[1-9]|9[0-9])\d{7}$",
            RegexOptions.Compiled);

        public static bool IsValidVietnamesePhone(string? phone) =>
            !string.IsNullOrWhiteSpace(phone) && VietnamMobileRegex.IsMatch(phone.Trim());
    }
}
