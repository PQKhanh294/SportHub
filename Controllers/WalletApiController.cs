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

        public WalletApiController(ApplicationDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
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
    }
}
