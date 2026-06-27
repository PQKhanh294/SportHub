# SportHub — CLAUDE.md

## Tổng quan dự án

SportHub là nền tảng kết nối thể thao cho người dùng Việt Nam: ghép trận cầu lông + đặt sân thể thao. Sản phẩm MVP, kinh phí hạn chế — không dùng API có phí mới ngoài những gì đã tích hợp.

**Deploy:** https://sporthub-dn.id.vn  
**Branch chính:** `master` | **Branch dev:** `dev`

---

## Tech Stack

| Thành phần | Công nghệ |
|-----------|-----------|
| Backend + Frontend | ASP.NET Core 8 Razor Pages (coupled — KHÔNG tách FE/BE) |
| Database | SQL Server, instance `.`, database `SportHubDB` |
| ORM | Entity Framework Core 8, migrations trong `Migrations/` |
| Real-time | SignalR — hub `/hubs/notifications`, `/hubs/chat` |
| CSS | Tailwind CSS (CDN), dark mode support |
| JS | Vanilla JS + Fetch API, không dùng framework |
| AI | Groq API (key `gsk_...`) — model `llama-3.1-8b-instant`, config key là `"Gemini"` trong appsettings |
| Thanh toán | SePay (bank transfer QR + webhook) — PRIMARY. VNPay/MoMo chưa dùng. |
| Maps | Goong API + OpenStreetMap Nominatim |
| Auth | Cookie-based + Google OAuth + Facebook OAuth |

---

## Kiến trúc & Conventions

### Pattern cố định
- **Services:** Interface trong `Services/Interfaces/IXxxService.cs` → Implementation trong `Services/Implementations/XxxService.cs` → Đăng ký Scoped trong `Program.cs`
- **Pages:** Razor Pages trong `Pages/` — mỗi page có `.cshtml` + `.cshtml.cs`
- **API Controllers:** Trong `Controllers/` — dùng cho webhook và AJAX calls từ JS
- **Hubs:** SignalR trong `Hubs/`

### Vai trò người dùng (Roles)
`User` · `Admin` · `CourtOwner` · `Host`

### Quan trọng về AI
Config key `"Gemini"` trong `appsettings.json` thực ra đang dùng **Groq API** (không phải Google Gemini). Model: `llama-3.1-8b-instant`. Không nhầm lẫn khi debug AI features.

### Secrets
Credentials trong `appsettings.json` (Google OAuth, Facebook, SePay, Groq, Goong). Không log, không commit production values ra public repo.

---

## Lệnh thường dùng

```powershell
# Build project
cd "d:\LAP TRINH\SUMMER 2026\SportHub"
dotnet build 2>&1 | Where-Object { $_ -match "error|warning" }

# Chạy local
dotnet run --project "d:\LAP TRINH\SUMMER 2026\SportHub"

# Thêm EF Migration
dotnet ef migrations add <TenMigration> --project "d:\LAP TRINH\SUMMER 2026\SportHub"

# Cập nhật DB
dotnet ef database update --project "d:\LAP TRINH\SUMMER 2026\SportHub"

# Rollback migration
dotnet ef database update <TenMigrationTruoc> --project "d:\LAP TRINH\SUMMER 2026\SportHub"

# Thêm package
dotnet add "d:\LAP TRINH\SUMMER 2026\SportHub" package <PackageName>

# Query DB nhanh
sqlcmd -S "." -d "SportHubDB" -E -Q "<SQL>" -W -C
```

---

## Bản đồ file quan trọng

```
SportHub/
├── Program.cs                          ← DI registration, middleware pipeline
├── appsettings.json                    ← Config (secrets, connection string)
├── Data/
│   └── ApplicationDbContext.cs         ← DbContext, tất cả DbSet
├── Models/
│   └── Entities/                       ← Entity classes (DB tables)
├── Services/
│   ├── Interfaces/                     ← IXxxService contracts
│   └── Implementations/               ← XxxService logic
│       ├── WalletService.cs            ← Ví, credit/debit, SignalR balance update
│       ├── PromotionService.cs         ← Khuyến mãi, promo code, voucher
│       ├── MatchService.cs             ← Tạo/quản lý trận
│       ├── MatchPaymentService.cs      ← Thanh toán trận
│       └── AiChatService.cs           ← Groq API integration
├── Controllers/
│   ├── SepayWebhookController.cs       ← SePay webhook handler
│   └── WalletApiController.cs          ← API: balance, redeem-code, topup-status
├── Hubs/
│   ├── NotificationHub.cs              ← SignalR notifications (group: user:{userId})
│   └── ChatHub.cs                      ← SignalR chat
├── Pages/
│   ├── Admin/                          ← Admin panel (require Admin role)
│   │   ├── Index.cshtml                ← Dashboard (Chart.js, 7-day revenue)
│   │   └── Promotions.cshtml           ← Quản lý khuyến mãi
│   ├── Wallet/
│   │   ├── Index.cshtml                ← Trang ví (promo input, voucher list)
│   │   └── TopUp.cshtml                ← QR nạp tiền (polling 5s + cần thêm SignalR)
│   ├── Profile/
│   │   └── Index.cshtml                ← Hồ sơ người dùng
│   └── Auth/
│       └── ExternalCallback.cshtml.cs  ← Google/Facebook OAuth callback
└── Services/
    ├── PendingJoinExpiryHostedService.cs  ← Background: expire pending joins
    └── PromotionSchedulerService.cs       ← Background: auto-trigger promotions
```

---

## Database Schema — Bảng quan trọng

| Bảng | Mô tả |
|------|-------|
| `Users` | Người dùng, có `WalletBalance`, `Role`, `IsBanned` |
| `Matches` | Trận đấu — cần thêm `IsLockedByHost`, `CancelReason`, `CancelledAt` |
| `MatchParticipants` | Người tham gia trận, `JoinStatus`, `PaymentStatus` |
| `MatchPayments` | Thanh toán trận (HostDeposit, PlayerFee, HostRemaining) |
| `WalletTopUpRequests` | Lệnh nạp tiền, `TransactionRef`, `Status` (Pending/Confirmed/Expired) |
| `WalletTransactions` | Lịch sử giao dịch ví |
| `PromotionCampaigns` | Campaign khuyến mãi |
| `PromoCodes` | Mã promo (có `UseCount`, `MaxUses`) |
| `PromotionRedemptions` | Log lần dùng mã |
| `Notifications` | In-app notifications |
| `ChatMessages` | Chat 1-1, có `IsRead` |

**DB Connection:** `Server=.;Database=SportHubDB;Trusted_Connection=True;TrustServerCertificate=True`

---

## Kế hoạch đã duyệt — MVP Fix & Features

### Nhóm A — Bug Critical
- [ ] **A1** — TopUp.cshtml: thêm SignalR `WalletCredited` listener, cập nhật navbar balance real-time
- [ ] **A2** — Wallet/Index.cshtml: merge "Tra cứu" + "Áp dụng" thành 1 bước trong JS
- [ ] **A3** — PromotionService.cs:268 — thay `UseCount++` bằng atomic `ExecuteUpdateAsync` + rate limiting + DB unique index
- [ ] **A4** — Admin Promotions: thêm nút "Phát thưởng ngay" cho TriggerType=Manual, thêm `DistributeManualCampaignAsync()`
- [ ] **A5** — Program.cs: bật `GetClaimsFromUserInfoEndpoint = true` cho Google OAuth để lấy avatar

### Nhóm B — Tính năng mới
- [ ] **B1** — MatchService: thêm `LockMatchByHostAsync()` + `CancelMatchByHostAsync()` + DB migration (3 fields)
- [ ] **B2** — Email notification qua Resend.com (free 3000/tháng): `IEmailService` + 5 trigger points
- [ ] **B3** — Dispute system: bảng `MatchDisputes` + `DisputeService` + auto-resolve rules + admin UI
- [ ] **B4** — Admin/Promotions.cshtml: tooltip (i) hướng dẫn từng loại campaign + preview count
- [ ] **B5** — Profile/Index.cshtml: collapsible sections (ví, mã KM, huy hiệu, đánh giá)

### Nhóm C — Kỹ thuật
- [ ] **C1** — SQL Full-Text Search + B-tree indexes (Matches, Venues, Users)
- [ ] **C2** — Chat: read receipt API + SignalR `MessagesRead` event + UI ✓✓
- [ ] **C3** — AI: circuit breaker wrapper + `IMemoryCache` cho Groq calls
- [ ] **C4** — Response compression + output cache + Cloudflare CDN
- [ ] **C5** — Admin dashboard: 4 biểu đồ mới + KPI row + time range selector

---

## KHÔNG làm (ngoài scope MVP)

- ❌ Tách FE/BE (SPA + API) — quá lớn cho MVP
- ❌ Zalo OA / ZNS — cần phí per-message
- ❌ Tích hợp đầy đủ VNPay/MoMo — SePay đủ rồi
- ❌ Group chat + upload file/video — cần storage có phí
- ❌ Unit test suite — để sau
- ❌ Elasticsearch — SQL Server FTS đủ cho MVP
- ❌ Native iOS/Android app — web responsive thay thế

---

## Quy ước code

- Không thêm comment giải thích code rõ ràng — chỉ comment khi WHY không hiển nhiên
- Không tạo abstraction chưa cần thiết
- Không thêm error handling cho case không thể xảy ra
- Đặt tên tiếng Anh cho code, tiếng Việt cho UI và nội dung người dùng thấy
- Mỗi service method nên có 1 trách nhiệm rõ ràng
- Razor Pages: dữ liệu từ DB chỉ load trong `OnGet*` / `OnPost*`, không load trong view
- JS: dùng `fetch` + `async/await`, không dùng jQuery
- CSS: Tailwind utility classes — không viết custom CSS trừ khi thật sự cần
