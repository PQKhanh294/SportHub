using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class PromotionService : IPromotionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public PromotionService(ApplicationDbContext context, IWalletService walletService, INotificationService notificationService, IEmailService emailService)
        {
            _context = context;
            _walletService = walletService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // ─── Campaign Management ──────────────────────────────────────────────

        public async Task<PromotionCampaign> CreateCampaignAsync(string name, string? description, string triggerType,
            decimal amount, string applicableScope, DateTime? startDate, DateTime? endDate,
            int? maxRedemptions, int adminId)
        {
            var campaign = new PromotionCampaign
            {
                Name = name,
                Description = description,
                TriggerType = triggerType,
                Amount = amount,
                ApplicableScope = applicableScope,
                StartDate = startDate,
                EndDate = endDate,
                MaxRedemptions = maxRedemptions,
                CreatedByAdminID = adminId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.PromotionCampaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<List<PromotionCampaign>> GetAllCampaignsAsync()
        {
            return await _context.PromotionCampaigns
                .Include(c => c.CreatedByAdmin)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<(int Distributed, int Skipped)> DistributeManualCampaignAsync(int campaignId, int adminId)
        {
            var campaign = await _context.PromotionCampaigns.FindAsync(campaignId);
            if (campaign == null || !campaign.IsActive || campaign.TriggerType != "Manual")
                return (0, 0);

            var adminIds = await _context.UserRoles
                .Where(ur => ur.Role.RoleName == "Admin")
                .Select(ur => ur.UserID)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsBanned && !adminIds.Contains(u.UserID))
                .ToListAsync();

            int distributed = 0, skipped = 0;
            foreach (var user in users)
            {
                if (campaign.MaxRedemptions.HasValue && campaign.RedemptionCount >= campaign.MaxRedemptions)
                    break;

                if (!await CheckScopeBasicAsync(user, campaign.ApplicableScope)) { skipped++; continue; }

                var alreadyReceived = await _context.PromotionRedemptions
                    .AnyAsync(r => r.UserID == user.UserID && r.CampaignID == campaignId
                              && r.PromoCodeID == null && r.VoucherID == null);
                if (alreadyReceived) { skipped++; continue; }

                await CreditCampaignAsync(user.UserID, campaign, "Thưởng từ admin");
                distributed++;
            }

            return (distributed, skipped);
        }

        public async Task<int> GetEligibleUserCountAsync(int campaignId)
        {
            var campaign = await _context.PromotionCampaigns.FindAsync(campaignId);
            if (campaign == null || !campaign.IsActive) return 0;

            var adminIds = await _context.UserRoles
                .Where(ur => ur.Role.RoleName == "Admin")
                .Select(ur => ur.UserID)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsBanned && !adminIds.Contains(u.UserID))
                .ToListAsync();

            int count = 0;
            foreach (var user in users)
            {
                if (campaign.MaxRedemptions.HasValue && count >= campaign.MaxRedemptions) break;
                if (!await CheckScopeBasicAsync(user, campaign.ApplicableScope)) continue;
                var alreadyReceived = await _context.PromotionRedemptions
                    .AnyAsync(r => r.UserID == user.UserID && r.CampaignID == campaignId
                              && r.PromoCodeID == null && r.VoucherID == null);
                if (!alreadyReceived) count++;
            }
            return count;
        }

        public async Task SetCampaignActiveAsync(int campaignId, bool active)
        {
            var campaign = await _context.PromotionCampaigns.FindAsync(campaignId);
            if (campaign == null) return;
            campaign.IsActive = active;
            await _context.SaveChangesAsync();
        }

        public async Task<PromotionCampaign?> GetCampaignAsync(int campaignId)
        {
            return await _context.PromotionCampaigns
                .Include(c => c.PromoCodes)
                .FirstOrDefaultAsync(c => c.CampaignID == campaignId);
        }

        // ─── Promo Codes ─────────────────────────────────────────────────────

        public async Task<PromoCode> CreatePromoCodeAsync(int campaignId, string code, int? maxUses, DateTime? expiresAt)
        {
            var promo = new PromoCode
            {
                CampaignID = campaignId,
                Code = code.Trim().ToUpper(),
                MaxUses = maxUses,
                ExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();
            return promo;
        }

        public async Task<List<PromoCode>> GeneratePromoCodesAsync(int campaignId, int count, string prefix, int? maxUses, DateTime? expiresAt)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            var pfx = prefix.ToUpper().Replace("-", "").Replace(" ", "");
            if (pfx.Length > 6) pfx = pfx[..6];
            var suffixLen = Math.Max(4, 10 - pfx.Length);

            var codes = new List<PromoCode>();
            var existing = await _context.PromoCodes
                .Where(p => p.CampaignID == campaignId)
                .Select(p => p.Code)
                .ToListAsync();
            var generated = new HashSet<string>(existing);

            for (int i = 0; i < count; i++)
            {
                string code;
                int attempts = 0;
                do
                {
                    var suffix = new string(Enumerable.Range(0, suffixLen).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
                    code = pfx + suffix;
                    attempts++;
                } while (generated.Contains(code) && attempts < 100);

                generated.Add(code);
                codes.Add(new PromoCode
                {
                    CampaignID = campaignId,
                    Code = code,
                    MaxUses = maxUses,
                    ExpiresAt = expiresAt,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            _context.PromoCodes.AddRange(codes);
            await _context.SaveChangesAsync();
            return codes;
        }

        public async Task<List<PromoCode>> GetPromoCodesAsync(int campaignId)
        {
            return await _context.PromoCodes
                .Where(p => p.CampaignID == campaignId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task SetPromoCodeActiveAsync(int promoCodeId, bool active)
        {
            var code = await _context.PromoCodes.FindAsync(promoCodeId);
            if (code == null) return;
            code.IsActive = active;
            await _context.SaveChangesAsync();
        }

        // ─── Admin Credit & Voucher ───────────────────────────────────────────

        public async Task AdminCreditAsync(int adminId, int userId, decimal amount, string note)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            await _walletService.CreditAsync(userId, amount, $"Admin cộng ví: {note}", type: "AdminCredit");

            await _notificationService.CreateAsync(
                userId,
                "AdminCredit",
                "Ví ảo được cộng tiền",
                $"+{amount:N0} xu — {note}",
                "/Wallet"
            );
        }

        public async Task<UserVoucher> IssueVoucherAsync(int adminId, int userId, int? campaignId, decimal amount, DateTime? expiresAt, string? note)
        {
            var code = GenerateVoucherCode();
            var voucher = new UserVoucher
            {
                UserID = userId,
                CampaignID = campaignId,
                Code = code,
                Amount = amount,
                ExpiresAt = expiresAt,
                IssuedByAdminID = adminId,
                Note = note,
                IssuedAt = DateTime.UtcNow
            };
            _context.UserVouchers.Add(voucher);
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                userId,
                "Voucher",
                "Bạn nhận được voucher mới!",
                $"Voucher {code} — {amount:N0} xu{(expiresAt.HasValue ? $", hết hạn {expiresAt.Value.ToLocalTime():dd/MM/yyyy}" : "")}",
                "/Wallet"
            );

            var user = await _context.Users.FindAsync(userId);
            if (user?.NotifyByEmail == true && user.NotifyPromoCode && !string.IsNullOrWhiteSpace(user.Email))
            {
                var campaignName = campaignId.HasValue
                    ? (await _context.PromotionCampaigns.FindAsync(campaignId.Value))?.Name ?? "Voucher"
                    : "Voucher cá nhân";
                await _emailService.SendPromoCodeAsync(user.Email, user.FullName, code, amount, campaignName, expiresAt);
            }

            return voucher;
        }

        // ─── Redemption History ───────────────────────────────────────────────

        public async Task<List<PromotionRedemption>> GetAllRedemptionsAsync(int page = 1, int pageSize = 30)
        {
            return await _context.PromotionRedemptions
                .Include(r => r.User)
                .Include(r => r.Campaign)
                .Include(r => r.PromoCode)
                .Include(r => r.Voucher)
                .OrderByDescending(r => r.RedeemedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<PromotionRedemption>> GetUserRedemptionsAsync(int userId)
        {
            return await _context.PromotionRedemptions
                .Include(r => r.Campaign)
                .Include(r => r.PromoCode)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();
        }

        // ─── User: Promo Codes ────────────────────────────────────────────────

        public async Task<PromoCodeInfoDto> LookupCodeAsync(string code, int userId)
        {
            var promo = await _context.PromoCodes
                .Include(p => p.Campaign)
                .FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpper());

            if (promo == null || !promo.IsActive || promo.Campaign == null || !promo.Campaign.IsActive)
                return new PromoCodeInfoDto(false, "", 0, null, null, false, false, "Mã không hợp lệ hoặc đã bị vô hiệu hóa.");

            if (promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow)
                return new PromoCodeInfoDto(true, promo.Campaign.Name, promo.Campaign.Amount, null, promo.ExpiresAt, false, false, "Mã đã hết hạn.");

            if (promo.MaxUses.HasValue && promo.UseCount >= promo.MaxUses)
                return new PromoCodeInfoDto(true, promo.Campaign.Name, promo.Campaign.Amount, 0, promo.ExpiresAt, false, false, "Mã đã được dùng hết lượt.");

            var campaign = promo.Campaign;
            if (campaign.StartDate.HasValue && campaign.StartDate > DateTime.UtcNow)
                return new PromoCodeInfoDto(true, campaign.Name, campaign.Amount, null, promo.ExpiresAt, false, false, "Chương trình chưa bắt đầu.");

            if (campaign.EndDate.HasValue && campaign.EndDate < DateTime.UtcNow)
                return new PromoCodeInfoDto(true, campaign.Name, campaign.Amount, null, promo.ExpiresAt, false, false, "Chương trình đã kết thúc.");

            // Check if already used
            var alreadyUsed = await _context.PromotionRedemptions
                .AnyAsync(r => r.UserID == userId && r.PromoCodeID == promo.PromoCodeID);
            if (alreadyUsed)
                return new PromoCodeInfoDto(true, campaign.Name, campaign.Amount,
                    promo.MaxUses.HasValue ? Math.Max(0, promo.MaxUses.Value - promo.UseCount) : null,
                    promo.ExpiresAt, true, false, "Bạn đã sử dụng mã này rồi.");

            // Scope check
            var (eligible, reason) = await CheckScopeAsync(userId, campaign.ApplicableScope);
            if (!eligible)
                return new PromoCodeInfoDto(true, campaign.Name, campaign.Amount,
                    promo.MaxUses.HasValue ? Math.Max(0, promo.MaxUses.Value - promo.UseCount) : null,
                    promo.ExpiresAt, false, false, reason);

            int? remaining = promo.MaxUses.HasValue ? Math.Max(0, promo.MaxUses.Value - promo.UseCount) : null;
            return new PromoCodeInfoDto(true, campaign.Name, campaign.Amount, remaining, promo.ExpiresAt, false, true, null);
        }

        public async Task<RedeemResult> RedeemPromoCodeAsync(int userId, string code)
        {
            var info = await LookupCodeAsync(code, userId);
            if (!info.Eligible)
                return new RedeemResult(false, info.BlockReason ?? "Không thể áp dụng mã.", 0);

            var promo = await _context.PromoCodes
                .Include(p => p.Campaign)
                .FirstAsync(p => p.Code == code.Trim().ToUpper());

            // Atomic UseCount increment — 0 rows = code ran out under concurrent load
            var incremented = await _context.PromoCodes
                .Where(p => p.PromoCodeID == promo.PromoCodeID
                         && (p.MaxUses == null || p.UseCount < p.MaxUses))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UseCount, p => p.UseCount + 1));

            if (incremented == 0)
                return new RedeemResult(false, "Mã đã hết lượt dùng.", 0);

            if (promo.Campaign.MaxRedemptions.HasValue)
                await _context.PromotionCampaigns
                    .Where(c => c.CampaignID == promo.CampaignID)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.RedemptionCount, c => c.RedemptionCount + 1));

            // Insert redemption BEFORE crediting — DB unique index UX_Redemptions_User_PromoCode
            // catches the double-claim race condition at DB level
            _context.PromotionRedemptions.Add(new PromotionRedemption
            {
                UserID = userId,
                CampaignID = promo.CampaignID,
                PromoCodeID = promo.PromoCodeID,
                AmountCredited = promo.Campaign.Amount,
                Note = promo.Code,
                RedeemedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException?.Message?.Contains("UX_Redemptions_User_PromoCode") == true
                   || ex.InnerException?.Message?.Contains("Cannot insert duplicate") == true)
            {
                // Concurrent double-claim: roll back UseCount we just incremented
                await _context.PromoCodes
                    .Where(p => p.PromoCodeID == promo.PromoCodeID)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.UseCount, p => p.UseCount - 1));
                return new RedeemResult(false, "Mã đã được sử dụng rồi.", 0);
            }

            // Credit wallet after redemption record committed
            await _walletService.CreditAsync(userId, promo.Campaign.Amount,
                $"Mã khuyến mãi {promo.Code} — {promo.Campaign.Name}", type: "Promotion");

            await _notificationService.CreateAsync(
                userId,
                "Promotion",
                "Mã khuyến mãi được áp dụng!",
                $"+{promo.Campaign.Amount:N0} xu — {promo.Campaign.Name} (Mã: {promo.Code})",
                "/Wallet"
            );

            return new RedeemResult(true, $"Áp dụng thành công! Ví đã nhận +{promo.Campaign.Amount:N0} xu.", promo.Campaign.Amount);
        }

        // ─── User: Personal Vouchers ──────────────────────────────────────────

        public async Task<List<UserVoucher>> GetMyVouchersAsync(int userId)
        {
            return await _context.UserVouchers
                .Include(v => v.Campaign)
                .Where(v => v.UserID == userId)
                .OrderByDescending(v => v.IssuedAt)
                .ToListAsync();
        }

        public async Task<RedeemResult> UseVoucherAsync(int userId, string voucherCode)
        {
            var voucher = await _context.UserVouchers
                .Include(v => v.Campaign)
                .FirstOrDefaultAsync(v => v.Code == voucherCode.Trim().ToUpper() && v.UserID == userId);

            if (voucher == null)
                return new RedeemResult(false, "Voucher không tồn tại hoặc không thuộc về bạn.", 0);

            if (voucher.IsUsed)
                return new RedeemResult(false, "Voucher này đã được sử dụng.", 0);

            if (voucher.ExpiresAt.HasValue && voucher.ExpiresAt < DateTime.UtcNow)
                return new RedeemResult(false, "Voucher đã hết hạn.", 0);

            voucher.IsUsed = true;
            voucher.UsedAt = DateTime.UtcNow;

            await _walletService.CreditAsync(userId, voucher.Amount,
                $"Voucher {voucher.Code}{(voucher.Note != null ? $" — {voucher.Note}" : "")}", type: "Promotion");

            var tx = await _context.WalletTransactions
                .Where(wt => wt.UserID == userId)
                .OrderByDescending(wt => wt.CreatedAt)
                .FirstOrDefaultAsync();

            _context.PromotionRedemptions.Add(new PromotionRedemption
            {
                UserID = userId,
                CampaignID = voucher.CampaignID,
                VoucherID = voucher.VoucherID,
                AmountCredited = voucher.Amount,
                WalletTransactionID = tx?.WalletTransactionID,
                Note = voucher.Code,
                RedeemedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                userId,
                "Voucher",
                "Voucher đã được kích hoạt!",
                $"+{voucher.Amount:N0} xu — {(voucher.Note ?? voucher.Code)}",
                "/Wallet"
            );

            return new RedeemResult(true, $"Kích hoạt thành công! Ví đã nhận +{voucher.Amount:N0} xu.", voucher.Amount);
        }

        // ─── Auto Triggers ────────────────────────────────────────────────────

        public async Task TriggerFirstLoginAsync(int userId)
        {
            var campaigns = await _context.PromotionCampaigns
                .Where(c => c.IsActive && c.TriggerType == "FirstLogin"
                    && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                    && (c.EndDate == null || c.EndDate >= DateTime.UtcNow)
                    && (c.MaxRedemptions == null || c.RedemptionCount < c.MaxRedemptions))
                .ToListAsync();

            foreach (var campaign in campaigns)
            {
                var alreadyReceived = await _context.PromotionRedemptions
                    .AnyAsync(r => r.UserID == userId && r.CampaignID == campaign.CampaignID
                        && r.PromoCodeID == null && r.VoucherID == null);
                if (alreadyReceived) continue;

                var user = await _context.Users.FindAsync(userId);
                if (user == null) continue;

                if (!await CheckScopeBasicAsync(user, campaign.ApplicableScope)) continue;

                await CreditCampaignAsync(userId, campaign, "Thưởng đăng nhập lần đầu");
            }
        }

        public async Task TriggerBirthdayAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.DateOfBirth == null) return;

            var today = DateTime.Today;
            var dob = user.DateOfBirth.Value;
            if (dob.Month != today.Month || dob.Day != today.Day) return;

            var campaigns = await _context.PromotionCampaigns
                .Where(c => c.IsActive && c.TriggerType == "Birthday"
                    && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                    && (c.EndDate == null || c.EndDate >= DateTime.UtcNow)
                    && (c.MaxRedemptions == null || c.RedemptionCount < c.MaxRedemptions))
                .ToListAsync();

            foreach (var campaign in campaigns)
            {
                // Birthday: allow once per year — check if received this year
                var thisYearReceived = await _context.PromotionRedemptions
                    .AnyAsync(r => r.UserID == userId && r.CampaignID == campaign.CampaignID
                        && r.PromoCodeID == null && r.VoucherID == null
                        && r.RedeemedAt.Year == today.Year);
                if (thisYearReceived) continue;

                await CreditCampaignAsync(userId, campaign, $"Chúc mừng sinh nhật!");
            }
        }

        public async Task TriggerHolidayAsync(string triggerType)
        {
            var campaigns = await _context.PromotionCampaigns
                .Where(c => c.IsActive && c.TriggerType == triggerType
                    && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                    && (c.EndDate == null || c.EndDate >= DateTime.UtcNow)
                    && (c.MaxRedemptions == null || c.RedemptionCount < c.MaxRedemptions))
                .ToListAsync();

            if (!campaigns.Any()) return;

            var requiredGender = triggerType == "WomensDay" ? "F" : triggerType == "MensDay" ? "M" : null;

            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsBanned
                    && (requiredGender == null || u.Gender == requiredGender))
                .Select(u => u.UserID)
                .ToListAsync();

            var today = DateTime.Today;
            foreach (var campaign in campaigns)
            {
                foreach (var userId in users)
                {
                    var thisYearReceived = await _context.PromotionRedemptions
                        .AnyAsync(r => r.UserID == userId && r.CampaignID == campaign.CampaignID
                            && r.PromoCodeID == null && r.VoucherID == null
                            && r.RedeemedAt.Year == today.Year);
                    if (thisYearReceived) continue;

                    await CreditCampaignAsync(userId, campaign,
                        triggerType == "WomensDay" ? "Chúc mừng ngày Quốc tế Phụ nữ 8/3!"
                        : "Chúc mừng ngày Quốc tế Đàn ông 19/11!");
                }
            }
        }

        // ─── User: Saved Promo Codes ──────────────────────────────────────────

        public async Task<(bool Success, string Message)> SaveCodeAsync(int userId, string code)
        {
            var normalized = code.Trim().ToUpper();
            if (await _context.SavedPromoCodes.AnyAsync(s => s.UserID == userId && s.Code == normalized))
                return (false, "Mã đã được lưu trước đó.");

            _context.SavedPromoCodes.Add(new SavedPromoCode { UserID = userId, Code = normalized, SavedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return (true, "Đã lưu mã thành công.");
        }

        public async Task<List<SavedCodeWithDetailsDto>> GetSavedCodesAsync(int userId)
        {
            var saved = await _context.SavedPromoCodes
                .Where(s => s.UserID == userId)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            var result = new List<SavedCodeWithDetailsDto>();
            foreach (var s in saved)
            {
                var promo = await _context.PromoCodes
                    .Include(p => p.Campaign)
                    .FirstOrDefaultAsync(p => p.Code == s.Code);

                if (promo == null || !promo.IsActive || promo.Campaign == null || !promo.Campaign.IsActive)
                {
                    result.Add(new SavedCodeWithDetailsDto(s.Code, null, null, null, false, "Mã không còn hoạt động", s.SavedAt));
                    continue;
                }

                var expired = promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow;
                var exhausted = promo.MaxUses.HasValue && promo.UseCount >= promo.MaxUses;
                var alreadyUsed = await _context.PromotionRedemptions
                    .AnyAsync(r => r.UserID == userId && r.PromoCodeID == promo.PromoCodeID);

                var msg = expired ? "Đã hết hạn" : exhausted ? "Hết lượt dùng" : alreadyUsed ? "Bạn đã dùng rồi" : (string?)null;
                result.Add(new SavedCodeWithDetailsDto(s.Code, promo.Campaign.Amount, promo.Campaign.Name, promo.ExpiresAt, msg == null, msg, s.SavedAt));
            }
            return result;
        }

        public async Task RemoveSavedCodeAsync(int userId, string code)
        {
            var saved = await _context.SavedPromoCodes
                .FirstOrDefaultAsync(s => s.UserID == userId && s.Code == code.Trim().ToUpper());
            if (saved != null)
            {
                _context.SavedPromoCodes.Remove(saved);
                await _context.SaveChangesAsync();
            }
        }

        // ─── Admin User List ──────────────────────────────────────────────────

        public async Task<List<WalletUserDto>> GetUsersForWalletManagementAsync(string? search, int page, int pageSize)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            return await query
                .OrderByDescending(u => u.WalletBalance)
                .ThenBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new WalletUserDto(u.UserID, u.FullName, u.Email, u.AvatarUrl, u.WalletBalance, u.CreatedAt))
                .ToListAsync();
        }

        public async Task<int> GetUsersCountAsync(string? search)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            return await query.CountAsync();
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        private async Task CreditCampaignAsync(int userId, PromotionCampaign campaign, string description)
        {
            await _walletService.CreditAsync(userId, campaign.Amount,
                $"{description} — {campaign.Name}", type: "Promotion");

            var tx = await _context.WalletTransactions
                .Where(wt => wt.UserID == userId)
                .OrderByDescending(wt => wt.CreatedAt)
                .FirstOrDefaultAsync();

            campaign.RedemptionCount++;
            _context.PromotionRedemptions.Add(new PromotionRedemption
            {
                UserID = userId,
                CampaignID = campaign.CampaignID,
                AmountCredited = campaign.Amount,
                WalletTransactionID = tx?.WalletTransactionID,
                Note = description,
                RedeemedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                userId,
                "Promotion",
                description,
                $"+{campaign.Amount:N0} xu — {campaign.Name}",
                "/Wallet"
            );
        }

        private async Task<(bool Eligible, string Reason)> CheckScopeAsync(int userId, string scope)
        {
            if (scope == "All") return (true, "");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Không tìm thấy người dùng.");

            return await CheckScopeBasicAsync(user, scope) ? (true, "") : (false, GetScopeBlockReason(scope));
        }

        private async Task<bool> CheckScopeBasicAsync(User user, string scope)
        {
            return scope switch
            {
                "All" => true,
                "Gender:F" => user.Gender == "F",
                "Gender:M" => user.Gender == "M",
                "HostOnly" => await _context.UserRoles.Include(ur => ur.Role)
                    .AnyAsync(ur => ur.UserID == user.UserID && ur.Role.RoleName == "Host"),
                "NewUser" => user.LoginCount <= 1,
                _ => true
            };
        }

        private static string GetScopeBlockReason(string scope) => scope switch
        {
            "Gender:F" => "Ưu đãi này chỉ dành cho người dùng nữ.",
            "Gender:M" => "Ưu đãi này chỉ dành cho người dùng nam.",
            "HostOnly" => "Ưu đãi này chỉ dành cho Host.",
            "NewUser"  => "Ưu đãi này chỉ dành cho người dùng mới.",
            _ => "Bạn không đủ điều kiện áp dụng ưu đãi này."
        };

        private static string GenerateVoucherCode()
        {
            var rng = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new string(Enumerable.Range(0, 12).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            return $"V-{code[..4]}-{code[4..8]}-{code[8..12]}";
        }
    }
}
