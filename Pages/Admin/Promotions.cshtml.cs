using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    public class PromotionsModel : PageModel
    {
        private readonly IPromotionService _promotionService;
        private readonly IUserService _userService;

        public PromotionsModel(IPromotionService promotionService, IUserService userService)
        {
            _promotionService = promotionService;
            _userService = userService;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)] public string ActiveTab { get; set; } = "campaigns";
        [BindProperty(SupportsGet = true)] public int? SelectedCampaignId { get; set; }

        public List<PromotionCampaign> Campaigns { get; set; } = new();
        public List<PromoCode> PromoCodes { get; set; } = new();
        public List<PromotionRedemption> Redemptions { get; set; } = new();
        public PromotionCampaign? SelectedCampaign { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["AdminPage"] = "Promotions";
            if (!await IsAdminAsync()) return Forbid();

            Campaigns = await _promotionService.GetAllCampaignsAsync();

            if (ActiveTab == "codes" && SelectedCampaignId.HasValue)
            {
                SelectedCampaign = await _promotionService.GetCampaignAsync(SelectedCampaignId.Value);
                PromoCodes = await _promotionService.GetPromoCodesAsync(SelectedCampaignId.Value);
            }
            else if (ActiveTab == "history")
            {
                Redemptions = await _promotionService.GetAllRedemptionsAsync(1, 50);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateCampaignAsync(
            string name, string? description, string triggerType, decimal amount,
            string applicableScope, DateTime? startDate, DateTime? endDate, int? maxRedemptions)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Tên chiến dịch không được trống.";
                return RedirectToPage(new { ActiveTab = "campaigns" });
            }

            try
            {
                await _promotionService.CreateCampaignAsync(name.Trim(), description?.Trim(), triggerType,
                    amount, applicableScope, startDate?.ToUniversalTime(), endDate?.ToUniversalTime(),
                    maxRedemptions, GetAdminId());
                SuccessMessage = $"Đã tạo chiến dịch \"{name}\".";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { ActiveTab = "campaigns" });
        }

        public async Task<IActionResult> OnPostToggleCampaignAsync(int campaignId, bool active)
        {
            if (!await IsAdminAsync()) return Forbid();
            await _promotionService.SetCampaignActiveAsync(campaignId, active);
            SuccessMessage = active ? "Đã bật chiến dịch." : "Đã tắt chiến dịch.";
            return RedirectToPage(new { ActiveTab = "campaigns" });
        }

        public async Task<IActionResult> OnPostCreateCodeAsync(int campaignId, string code, int? maxUses, DateTime? expiresAt)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(code))
            {
                ErrorMessage = "Mã không được trống.";
                return RedirectToPage(new { ActiveTab = "codes", SelectedCampaignId = campaignId });
            }

            try
            {
                await _promotionService.CreatePromoCodeAsync(campaignId, code.Trim(), maxUses, expiresAt?.ToUniversalTime());
                SuccessMessage = $"Đã tạo mã {code.Trim().ToUpper()}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.Contains("unique") || ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    ? "Mã này đã tồn tại. Vui lòng dùng mã khác."
                    : $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { ActiveTab = "codes", SelectedCampaignId = campaignId });
        }

        public async Task<IActionResult> OnPostGenerateCodesAsync(int campaignId, string prefix, int count, int? maxUses, DateTime? expiresAt)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(prefix) || count <= 0 || count > 100)
            {
                ErrorMessage = "Prefix không được trống và số lượng phải từ 1–100.";
                return RedirectToPage(new { ActiveTab = "codes", SelectedCampaignId = campaignId });
            }

            try
            {
                var codes = await _promotionService.GeneratePromoCodesAsync(campaignId, count, prefix.Trim(), maxUses, expiresAt?.ToUniversalTime());
                SuccessMessage = $"Đã tạo {codes.Count} mã với prefix {prefix.ToUpper()}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { ActiveTab = "codes", SelectedCampaignId = campaignId });
        }

        public async Task<IActionResult> OnPostToggleCodeAsync(int promoCodeId, bool active, int campaignId)
        {
            if (!await IsAdminAsync()) return Forbid();
            await _promotionService.SetPromoCodeActiveAsync(promoCodeId, active);
            return RedirectToPage(new { ActiveTab = "codes", SelectedCampaignId = campaignId });
        }

        private int GetAdminId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private async Task<bool> IsAdminAsync()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var id)) return false;
            return await _userService.IsAdminAsync(id);
        }
    }
}
