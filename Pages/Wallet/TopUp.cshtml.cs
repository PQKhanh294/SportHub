using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Wallet
{
    [Authorize]
    public class TopUpModel : PageModel
    {
        private readonly IWalletService _walletService;
        private readonly IConfiguration _config;

        public TopUpModel(IWalletService walletService, IConfiguration config)
        {
            _walletService = walletService;
            _config = config;
        }

        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty] public decimal Amount { get; set; }

        public decimal Balance { get; set; }
        public WalletTopUpRequest? PendingRequest { get; set; }
        public string? QrUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Wallet";
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            Balance = await _walletService.GetBalanceAsync(userId);
            PendingRequest = await _walletService.GetPendingTopUpAsync(userId);
            if (PendingRequest != null) BuildQr(PendingRequest);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            if (Amount < 10_000 || Amount > 50_000_000)
            {
                ErrorMessage = "Số tiền nạp phải từ 10.000 đến 50.000.000 VND.";
                return RedirectToPage();
            }

            // Round to nearest 1000
            Amount = Math.Round(Amount / 1000) * 1000;

            PendingRequest = await _walletService.CreateTopUpRequestAsync(userId, Amount);
            BuildQr(PendingRequest);
            Balance = await _walletService.GetBalanceAsync(userId);
            return Page();
        }

        private void BuildQr(WalletTopUpRequest req)
        {
            var bankId     = _config["SportHubPayment:BankId"] ?? "TPB";
            var accountNo  = _config["SportHubPayment:AccountNumber"] ?? "0";
            var accountName = _config["SportHubPayment:AccountName"] ?? "SPORT HUB";
            var info = Uri.EscapeDataString(req.TransactionRef);
            QrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={(int)req.Amount}&addInfo={info}&accountName={Uri.EscapeDataString(accountName)}";
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
