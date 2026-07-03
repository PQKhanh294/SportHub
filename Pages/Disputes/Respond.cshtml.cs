using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Disputes
{
    [Authorize]
    public class RespondModel : PageModel
    {
        private readonly IDisputeService _disputeService;
        private readonly IWebHostEnvironment _env;

        public RespondModel(IDisputeService disputeService, IWebHostEnvironment env)
        {
            _disputeService = disputeService;
            _env = env;
        }

        public DisputeWitnessResponse? WitnessRequest { get; set; }
        public string DisputeTypeDisplay { get; set; } = string.Empty;
        public string MatchTitle { get; set; } = string.Empty;
        public string MatchDateText { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            WitnessRequest = await _disputeService.GetWitnessRequestAsync(id, userId);
            if (WitnessRequest == null)
            {
                TempData["ErrorMessage"] = "Bạn không có yêu cầu xác minh cho khiếu nại này.";
                return RedirectToPage("/Index");
            }

            var match = WitnessRequest.Dispute.Match;
            MatchTitle = match.Title ?? match.MatchType;
            MatchDateText = $"{match.MatchDate:dd/MM/yyyy} {match.StartTime:hh\\:mm}";
            DisputeTypeDisplay = WitnessRequest.Dispute.DisputeType switch
            {
                "HostNoShow" => "Chủ trận vắng mặt",
                "PlayerNoShow" => "Người chơi vắng mặt",
                "PlayerMisconduct" => "Người chơi vi phạm quy tắc",
                "WrongVenue" => "Sai địa điểm",
                "QualityIssue" => "Vấn đề chất lượng",
                "PaymentDispute" => "Tranh chấp thanh toán",
                var t => t
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string stance, string? comment, IFormFile? evidenceFile)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            string? evidenceUrl = null;
            if (evidenceFile is { Length: > 0 })
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
                if (allowed.Contains(ext) && evidenceFile.Length <= 5 * 1024 * 1024)
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "disputes");
                    Directory.CreateDirectory(folder);
                    var fileName = $"witness_{id}_{userId}_{Guid.NewGuid():N}{ext}";
                    var savePath = Path.Combine(folder, fileName);
                    await using var stream = System.IO.File.Create(savePath);
                    await evidenceFile.CopyToAsync(stream);
                    evidenceUrl = $"/uploads/disputes/{fileName}";
                }
            }

            var ok = await _disputeService.SubmitWitnessResponseAsync(id, userId, stance, comment, evidenceUrl);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Cảm ơn bạn đã xác minh! Thông tin của bạn giúp admin xử lý công bằng hơn."
                : "Không thể gửi phản hồi. Có thể bạn đã phản hồi rồi.";
            return RedirectToPage("/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
