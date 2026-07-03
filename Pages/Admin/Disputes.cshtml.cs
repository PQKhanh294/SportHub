using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class DisputesModel : PageModel
    {
        private readonly IDisputeService _disputeService;

        public DisputesModel(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        public List<MatchDispute> Disputes { get; set; } = new();
        public int PendingCount { get; set; }
        public string ActiveTab { get; set; } = "pending";

        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        private async Task<bool> IsAdminAsync()
        {
            var rolesClaim = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);
            return rolesClaim.Contains("Admin");
        }

        private int GetAdminId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public async Task<IActionResult> OnGetAsync(string tab = "pending")
        {
            ViewData["ActivePage"] = "Disputes";
            if (!await IsAdminAsync()) return Forbid();

            ActiveTab = tab;
            PendingCount = await _disputeService.GetPendingCountAsync();
            Disputes = tab == "all"
                ? await _disputeService.GetAllDisputesAsync(P, 20)
                : await _disputeService.GetPendingDisputesAsync();
            if (!string.IsNullOrWhiteSpace(Search))
                Disputes = Disputes.Where(d =>
                    (d.Reporter?.FullName ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (d.Match?.Title ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    d.Description.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(FilterType))
                Disputes = Disputes.Where(d => d.DisputeType == FilterType).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostResolveAsync(int disputeId, string adminNote, string resolution, decimal? refundAmount)
        {
            if (!await IsAdminAsync()) return Forbid();
            await _disputeService.ResolveDisputeAsync(disputeId, GetAdminId(), adminNote, resolution, refundAmount);
            TempData["Msg"] = "Đã giải quyết khiếu nại.";
            return RedirectToPage(new { tab = "pending" });
        }

        public async Task<IActionResult> OnPostDismissAsync(int disputeId, string adminNote)
        {
            if (!await IsAdminAsync()) return Forbid();
            await _disputeService.DismissDisputeAsync(disputeId, GetAdminId(), adminNote);
            TempData["Msg"] = "Đã bác khiếu nại.";
            return RedirectToPage(new { tab = "pending" });
        }

        public async Task<IActionResult> OnPostRunAiAsync(int disputeId)
        {
            if (!await IsAdminAsync()) return Forbid();
            await _disputeService.RunAiAnalysisAsync(disputeId);
            TempData["Msg"] = "Đã chạy phân tích AI cho khiếu nại #" + disputeId + ".";
            return RedirectToPage(new { tab = ActiveTab });
        }
    }
}
