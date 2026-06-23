using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wallet")]
    public class WalletApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly IPromotionService _promotionService;

        public WalletApiController(ApplicationDbContext context, IWalletService walletService, IPromotionService promotionService)
        {
            _context = context;
            _walletService = walletService;
            _promotionService = promotionService;
        }

        [HttpGet("topup-status")]
        public async Task<IActionResult> GetTopUpStatus([FromQuery] string @ref)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();

            var req = await _context.WalletTopUpRequests
                .FirstOrDefaultAsync(t => t.TransactionRef == @ref && t.UserID == userId);
            if (req == null) return NotFound(new { status = "NotFound" });

            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(new { status = req.Status, balance, actualAmount = req.ActualAmount });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();

            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(new { balance });
        }

        [HttpGet("lookup-code")]
        public async Task<IActionResult> LookupCode([FromQuery] string code)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();
            if (string.IsNullOrWhiteSpace(code)) return BadRequest(new { found = false, blockReason = "Mã không được trống." });

            var info = await _promotionService.LookupCodeAsync(code.Trim(), userId);
            return Ok(new
            {
                found = info.Found,
                campaignName = info.CampaignName,
                amount = info.Amount,
                remainingUses = info.RemainingUses,
                expiresAt = info.ExpiresAt,
                alreadyUsed = info.AlreadyUsed,
                eligible = info.Eligible,
                blockReason = info.BlockReason
            });
        }

        [HttpPost("redeem-code")]
        public async Task<IActionResult> RedeemCode([FromBody] RedeemCodeRequest req)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();
            if (string.IsNullOrWhiteSpace(req?.Code)) return BadRequest(new { success = false, message = "Mã không được trống." });

            var result = await _promotionService.RedeemPromoCodeAsync(userId, req.Code.Trim());
            return Ok(new { success = result.Success, message = result.Message, amount = result.Amount });
        }

        [HttpPost("save-code")]
        public async Task<IActionResult> SaveCode([FromBody] RedeemCodeRequest req)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();
            if (string.IsNullOrWhiteSpace(req?.Code)) return BadRequest(new { success = false, message = "Mã không được trống." });

            var (success, message) = await _promotionService.SaveCodeAsync(userId, req.Code.Trim());
            return Ok(new { success, message });
        }

        [HttpDelete("unsave-code/{code}")]
        public async Task<IActionResult> UnsaveCode(string code)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();

            await _promotionService.RemoveSavedCodeAsync(userId, code);
            return Ok(new { success = true });
        }

        public record RedeemCodeRequest(string Code);
    }
}
