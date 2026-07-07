using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SportHub.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Logged out successfully.";
            // Xóa cờ "đã bỏ qua popup hoàn thiện hồ sơ" — tránh trường hợp đăng nhập tài khoản
            // khác cùng tab thừa hưởng nhầm trạng thái bỏ qua của tài khoản trước.
            TempData["ClearProfileSkipFlag"] = true;
            return RedirectToPage("/Index");
        }
    }
}
