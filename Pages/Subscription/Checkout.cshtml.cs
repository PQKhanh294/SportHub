using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Subscription
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IWalletService _walletService;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public CheckoutModel(ISubscriptionService subscriptionService, IWalletService walletService, IConfiguration config, ApplicationDbContext context)
        {
            _subscriptionService = subscriptionService;
            _walletService = walletService;
            _config = config;
            _context = context;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)] public string PlanKey { get; set; } = "Pro";
        [BindProperty(SupportsGet = true)] public string Billing { get; set; } = "Monthly";

        public string PlanName { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal WalletBalance { get; set; }
        public string? TransactionRef { get; set; }
        public string? QrUrl { get; set; }
        public bool IsCreditBundle => PlanKey == "Credit";
        public bool IsWalletPayment { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Subscription";
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            if (IsCreditBundle)
            {
                PlanName = "5 Credit trận đấu";
                Amount = 20_000;
            }
            else
            {
                var plans = await _subscriptionService.GetAllPlansAsync();
                var plan = plans.FirstOrDefault(p => p.PlanKey == PlanKey);
                if (plan == null) return RedirectToPage("/Subscription/Index");
                PlanName = $"{plan.Name} ({BillingLabel(Billing)})";
                Amount = _subscriptionService.GetPrice(PlanKey, Billing);
                if (Amount <= 0) return RedirectToPage("/Subscription/Index");
            }

            WalletBalance = await _walletService.GetBalanceAsync(userId);

            // Show existing pending order if any
            if (!IsCreditBundle)
            {
                var pending = await _subscriptionService.GetPendingOrderAsync(userId);
                if (pending != null && pending.PlanKey == PlanKey)
                {
                    TransactionRef = pending.TransactionRef;
                    Amount = pending.Amount;
                    BuildQr(pending.TransactionRef, pending.Amount);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostPayWalletAsync()
        {
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var balance = await _walletService.GetBalanceAsync(userId);
            decimal price = IsCreditBundle ? 20_000m : _subscriptionService.GetPrice(PlanKey, Billing);

            if (balance < price)
            {
                TempData["ErrorMessage"] = $"Ví không đủ. Cần {price:N0} xu, hiện có {balance:N0} xu.";
                return RedirectToPage(new { planKey = PlanKey, billing = Billing });
            }

            // Deduct from wallet
            await _walletService.DeductAsync(userId, price, IsCreditBundle ? "Mua credit trận đấu" : $"Đăng ký gói {PlanKey}");

            if (IsCreditBundle)
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _context.UserMatchCredits.Add(new UserMatchCredit
                {
                    UserID = userId,
                    RemainingCredits = 5,
                    TransactionRef = $"SUBCREDITW-{userId}-{ts}",
                    AmountPaid = price,
                    Status = "Confirmed",
                    PurchasedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Mua 5 credit thành công!";
            }
            else
            {
                await _subscriptionService.ActivateSubscriptionAsync(userId, PlanKey, Billing);
                TempData["SuccessMessage"] = $"Đăng ký gói {PlanKey} thành công!";
            }

            return RedirectToPage("/Subscription/Index");
        }

        public async Task<IActionResult> OnPostCreateOrderAsync()
        {
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            SubscriptionOrder? order = null;
            if (!IsCreditBundle)
            {
                order = await _subscriptionService.CreateOrderAsync(userId, PlanKey, Billing);
                TransactionRef = order.TransactionRef;
                Amount = order.Amount;
                BuildQr(order.TransactionRef, order.Amount);
            }
            else
            {
                var credit = await _subscriptionService.CreateCreditOrderAsync(userId);
                TransactionRef = credit.TransactionRef;
                Amount = credit.AmountPaid;
                BuildQr(credit.TransactionRef, credit.AmountPaid);
            }

            PlanName = IsCreditBundle ? "5 Credit trận đấu"
                : $"{PlanKey} ({BillingLabel(Billing)})";
            WalletBalance = await _walletService.GetBalanceAsync(userId);
            return Page();
        }

        private void BuildQr(string transRef, decimal amount)
        {
            var bankId = _config["SportHubPayment:BankId"] ?? "TPB";
            var accountNo = _config["SportHubPayment:AccountNumber"] ?? "0";
            var accountName = _config["SportHubPayment:AccountName"] ?? "SPORT HUB";
            var info = Uri.EscapeDataString(transRef);
            QrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={(int)amount}&addInfo={info}&accountName={Uri.EscapeDataString(accountName)}";
        }

        private static string BillingLabel(string b) => b switch {
            "Quarterly" => "3 tháng",
            "Annual"    => "1 năm",
            _           => "1 tháng"
        };

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
