# PLAN — Fix lỗi hệ thống & nâng trải nghiệm người dùng SportHub

> Ngày lập: 2026-07-03 · Trạng thái: **CHỜ DUYỆT**
> Phạm vi: 6 nhóm vấn đề người dùng báo cáo + audit lỗi logic phát hiện khi khảo sát code.

---

## Tổng quan ưu tiên

| Phase | Nội dung | Ưu tiên | Effort | DB migration |
|-------|----------|---------|--------|--------------|
| **A** | Popup hoàn thiện hồ sơ + fix lỗi join trận báo sai | 🔴 P0 | ~4h | Không |
| **B** | Fix filter cầu lông (trình độ composite, chia đều tiền) + thêm "Sân số" | 🔴 P0 | ~3h | Có (1 cột) |
| **C** | Audit & fix các lỗi logic phát hiện thêm | 🟡 P1 | ~2h | Không |
| **D** | Chẩn đoán + fix Resend email & Facebook login | 🟡 P1 | ~2h | Không |
| **E** | Onboarding tour cho người dùng lần đầu | 🟢 P2 | ~3h | Không |
| **F** | Hoàn thiện luồng khiếu nại (ảnh + nhân chứng + AI) | 🟢 P2 | ~8h | Có (1 bảng + cột) |

Tổng: ~22h làm việc. DB upgrade gộp vào **`Scripts/database_upgrade_v6.sql`**.

---

## PHASE A — Popup hoàn thiện hồ sơ + fix lỗi join báo sai

### Vấn đề hiện tại
1. **User mới join trận bị chặn nhưng báo sai lý do.** `MatchService.JoinMatchAsync()` ([MatchService.cs:151-195](../../Services/Implementations/MatchService.cs)) trả về `bool`. Khi user chưa set `SkillLevel` (null) mà trận có `SkillRequired != "Any"`, hàm `ValidateUserSkillLevel()` (dòng 62) trả `false` → UI hiện *"Trận có thể đã đầy hoặc đã đóng"* ([Details.cshtml.cs:221](../../Pages/Matchmaking/Details.cshtml.cs)) — hoàn toàn sai bản chất.
2. **Nhắc hoàn thiện hồ sơ quá muộn.** `_Layout.cshtml:147` chỉ hiện banner khi `LoginCount >= 5`, trong khi môn + trình độ là điều kiện thiết yếu để join trận ngay từ lần đầu.

### Việc cần làm

#### A1 — Refactor JoinMatchAsync trả về lý do cụ thể
- `Services/Interfaces/IMatchService.cs`: đổi chữ ký
  ```csharp
  Task<JoinMatchResult> JoinMatchAsync(int matchId, int userId);
  // enum JoinMatchReason { Success, NotFound, Closed, Locked, AlreadyRequested, MatchFull, ProfileIncomplete, SkillTooLow }
  // record JoinMatchResult(bool Success, JoinMatchReason Reason)
  ```
- `MatchService.JoinMatchAsync`: trả reason tương ứng từng nhánh chặn. Nhánh `string.IsNullOrWhiteSpace(userSkill)` → `ProfileIncomplete` (tách riêng khỏi `SkillTooLow`).
- Cập nhật 2 call site: `Details.cshtml.cs` (OnPostJoin, dòng ~221) và `Index.cshtml.cs` (dòng ~403) — map reason → message tiếng Việt đúng:
  - `ProfileIncomplete` → *"Bạn chưa cập nhật môn thể thao & trình độ. Hãy hoàn thiện hồ sơ để tham gia trận."* + tự mở popup hồ sơ (A2)
  - `SkillTooLow` → *"Trình độ của bạn chưa phù hợp với yêu cầu của trận này."*
  - `MatchFull` → *"Trận đã đủ người."*
  - `Locked`/`Closed` → *"Trận đã khóa/đóng đăng ký."*
- Cập nhật `UiLocalizationMiddleware.cs:216` thêm bản dịch EN cho các message mới.

#### A2 — Popup hoàn thiện hồ sơ (modal giữa trang)
- **Trigger:** partial mới `Pages/Shared/_ProfileCompletionModal.cshtml`, render từ `_Layout.cshtml` khi: user đăng nhập && `!IsProfileCompleteAsync()` && chưa skip trong session (sessionStorage) && không ở path exempt (`/Profile`, `/Auth`, `/Onboarding`, `/api`). **Bỏ điều kiện LoginCount >= 5** cho popup (banner cũ giữ nguyên làm nhắc nhở phụ).
- **Nội dung form (4 trường):**
  1. Số điện thoại (input tel)
  2. Địa chỉ mặc định (reuse `SportHubLocationPicker` — autocomplete + ghim bản đồ)
  3. Môn thể thao (multi-select card giống trang Onboarding đã có)
  4. Trình độ theo từng môn đã chọn — **thang đo riêng từng môn, lấy từ `skillGuideData`** đang có sẵn ở [Matchmaking/Index.cshtml:1229](../../Pages/Matchmaking/Index.cshtml). Cạnh mỗi môn có nút **(i)** mở modal mô tả chi tiết các mức trình độ (reuse `skillGuideModal` markup — tách ra partial `_SkillGuideModal.cshtml` để dùng chung 2 nơi).
- **Nút:** "Lưu thông tin" (POST `/api/profile/complete` — endpoint mới trong controller mới `ProfileApiController`) + "Bỏ qua" (đóng modal, ghi `sessionStorage.profileSkipped=1` — hiện lại ở session sau).
- **Backend:** `ProfileApiController.Complete()` lưu `PhoneNumber`, `DefaultAddress` (+lat/lon), upsert `UserSportProfiles`, sync `FavoriteSport`/`SkillLevel` legacy (logic giống `Profile/Edit.cshtml.cs:163-181`).
- **(Tùy chọn — AI gợi ý trình độ):** nút "Chưa rõ trình độ? Để AI gợi ý" → 3 câu hỏi trắc nghiệm ngắn (thâm niên, tần suất, thành tích) → gọi `IAiChatService` (Groq, đã có circuit breaker + cache) trả mức gợi ý theo thang của môn. Nếu Groq lỗi → fallback chọn tay bình thường. *Làm sau cùng nếu còn thời gian trong phase.*

#### A3 — Fix validate trình độ theo từng môn (lỗi logic gốc)
`ValidateUserSkillLevel` hiện so `user.SkillLevel` (1 trường legacy, có thể là trình độ **môn khác**) với yêu cầu trận → so sánh chéo môn vô nghĩa. Sửa:
- `JoinMatchAsync` load thêm `UserSportProfiles` của user theo `match.SportID`; ưu tiên skill đúng môn, fallback legacy `SkillLevel`.
- Trận cầu lông có `SkillRequired` composite (`"Nam:...|Nữ:..."`): parse thành list mức theo giới tính user (`user.Gender`); user nữ so với vế `Nữ:`, nam so với vế `Nam:`; thiếu giới tính → so với hợp cả hai vế (dễ tính hơn, tránh chặn oan).
- Giữ nguyên buffer `>= required - 1` hiện có (không đổi độ khó, chỉ sửa đúng môn/đúng vế).

### Acceptance criteria Phase A
- [ ] User mới (chưa set gì) mở web → popup hiện giữa trang, điền xong join được trận ngay
- [ ] Nhấn "Bỏ qua" → popup không hiện lại trong cùng session, session sau hiện lại
- [ ] Join trận thiếu hồ sơ → message đúng + popup tự mở; trận đầy → message "đã đủ người"
- [ ] User có trình độ cầu lông "Yếu" join trận bóng đá "Khá" không còn bị so nhầm thang cầu lông

---

## PHASE B — Fix filter matchmaking (cầu lông) + thêm "Sân số"

### Vấn đề đã xác nhận trong code
1. **Filter trình độ không khớp trận cầu lông** — [Index.cshtml.cs:214-220](../../Pages/Matchmaking/Index.cshtml.cs): so sánh `string.Equals(m.SkillRequired, Skill)` **chính xác tuyệt đối**, trong khi trận cầu lông lưu `"Nam:Yếu,Trung Bình|Nữ:Yếu"` → chọn filter "Yếu" không bao giờ match. Đây chính là lỗi user báo.
2. **Filter giá sai với trận "chia đều tiền"** — dòng 282-294: `BuildMatchPriceAmount()` trả **tổng chi phí sân** (`CustomPriceVnd`), nhưng card hiển thị *"Chia đều cuối buổi"* (≈ tổng/số người). Trận 300k chia 6 người (50k/người) bị loại khi filter max 100k.

### Việc cần làm

#### B1 — Sửa filter trình độ hỗ trợ composite
Thay khối filter Skill bằng hàm `MatchesSkillFilter(string? skillRequired, string filter)`:
- `skillRequired` null/`"Any"` → luôn match (giữ nguyên).
- Chứa `"Nam:"`/`"Nữ:"` → tách theo `|`, tách vế theo `:`, split mức theo `,` → match nếu **bất kỳ** mức nào (Nam hoặc Nữ) equals filter (OrdinalIgnoreCase, trim).
- Ngược lại → exact match như cũ.

#### B2 — Sửa filter giá theo giá thực tế mỗi người
Trong khối MinPrice/MaxPrice: nếu `m.IsSplitFee && m.MaxParticipants > 0` → so sánh bằng `Math.Ceiling(amount / m.MaxParticipants)` (khớp đúng công thức hiển thị `splitFeePreview` ở trang Create). Trận thường giữ nguyên tổng.

#### B3 — Thêm trường "Sân số" (chỉ form Đầy đủ)
- Entity `Match` (Models/Entities): thêm `public string? CourtNumber { get; set; }` (NVARCHAR(30) NULL — cho phép "Sân 3", "3A"...).
- Migration EF `AddMatchCourtNumber` + thêm mục vào `Scripts/database_upgrade_v6.sql`.
- `Create.cshtml` (chỉ trong `fullModeSection`, cạnh "Tên sân"): input "Sân số (tùy chọn)" placeholder *"VD: Sân 3"*.
- `Create.cshtml.cs`: InputModel + map vào Match.
- Hiển thị: `Details.cshtml` (phần thông tin sân — "Sân số: 3") + card ở `Index.cshtml` (badge nhỏ cạnh tên sân nếu có).

### Acceptance criteria Phase B
- [ ] Tạo trận cầu lông trình Nam "Yếu" → filter môn Cầu lông + trình "Yếu" ra đúng trận đó
- [ ] Trận 300k chia đều 6 người → filter max 100k vẫn hiện (50k/người)
- [ ] Form đầy đủ có ô Sân số; điền → hiện ở Details + card; bỏ trống → không hiện gì

---

## PHASE C — Audit lỗi logic khác (phát hiện khi khảo sát)

| # | Lỗi | Vị trí | Fix |
|---|-----|--------|-----|
| C1 | `ValidateUserSkillLevel`: `SkillRequired` composite không có trong skillMap → return `true` (bỏ qua hoàn toàn yêu cầu trình độ với trận cầu lông) — **mâu thuẫn**: filter thì chặn hết, join thì thả hết | MatchService.cs:85-86 | Gộp vào A3 (parse composite) |
| C2 | `ApproveParticipantAsync` cũng gọi validate skill với cùng lỗi trên | MatchService.cs:299 | Dùng chung hàm mới của A3 |
| C3 | Skill map thiếu các thang Pickleball (2.0-4.5+), Tennis (1.0-5.0+), Bóng đá, Bóng bàn → các môn này validate luôn "unknown → true" | MatchService.cs:66-82 | Thêm map thang điểm từng môn (đồng bộ với `skillsBySport` ở Create.cshtml) |
| C4 | `CalculateSkillScore`/`SkillToRank` (gợi ý trận) cùng vấn đề composite → điểm gợi ý sai cho cầu lông | Index.cshtml.cs:807-828 | Parse composite lấy mức trung bình để chấm điểm |
| C5 | Quick-create không có ô "Số sân/địa chỉ" bắt buộc → trận không tọa độ sẽ bị loại khỏi filter khoảng cách (dòng 261 `return false`) | Index.cshtml.cs:254-264 | Đổi hành vi: trận thiếu tọa độ **vẫn hiện** khi filter khoảng cách (xếp cuối) thay vì biến mất |
| C6 | `Register` auto-login không gán claim Role → user mới đăng ký thiếu role "User" đến khi re-login | Register.cshtml.cs (vừa thêm ở commit 0cc1af6) | Load UserRoles sau CreateUserAsync và add claims Role |

### Acceptance criteria Phase C
- [ ] Trận cầu lông yêu cầu "Nam:TB+/Khá" chặn đúng user Newbie (trước đây cho qua)
- [ ] Trận không tọa độ vẫn hiện khi bật filter khoảng cách

---

## PHASE D — Resend email & Facebook login

### D1 — Resend chưa gửi được mail
**Chẩn đoán từ code:** `ResendEmailService.SendAsync()` ([ResendEmailService.cs:36-45](../../Services/Implementations/ResendEmailService.cs)) nuốt lỗi — chỉ `LogWarning` status code, không log body. Nguyên nhân khả dĩ nhất (theo thứ tự):
1. **Domain `sporthub-dn.id.vn` chưa verify trên Resend** (screenshot onboarding trước đó cho thấy đang ở bước setup) → Resend trả 403, chỉ cho gửi tới chính email chủ tài khoản.
2. From address không khớp domain đã verify.

**Việc cần làm:**
- Log thêm response body khi fail: `_logger.LogWarning("Resend {Status}: {Body}", resp.StatusCode, await resp.Content.ReadAsStringAsync())`.
- Endpoint test cho admin: `POST /api/admin/test-email` (role Admin) gửi mail thử → trả nguyên văn response Resend để thấy lỗi ngay trên UI/Swagger thay vì mò log server.
- **Việc của bạn (ngoài code):** vào dashboard Resend → Domains → add `sporthub-dn.id.vn` → thêm 3 record DNS (SPF, DKIM, MX) tại trang quản lý domain id.vn → chờ verify xanh. Chỉ khi domain Verified thì mail mới tới người dùng thật.

### D2 — Facebook login không hoạt động
**Chẩn đoán từ code:** config `Program.cs:74-79` đúng chuẩn. Vấn đề gần như chắc chắn nằm ở **Facebook App settings** (không phải code):
1. App đang ở **Development Mode** → chỉ admin/tester của app đăng nhập được; user thường bị lỗi. Cần chuyển **Live Mode** (yêu cầu Privacy Policy URL).
2. **Valid OAuth Redirect URIs** phải có chính xác `https://sporthub-dn.id.vn/signin-facebook`.

**Việc cần làm:**
- Code: thêm `options.Events.OnRemoteFailure` → redirect về `/Auth/Login?error=facebook` + hiện message thân thiện thay vì trang lỗi 500.
- **Việc của bạn:** vào developers.facebook.com → App → (a) Settings > Basic: điền Privacy Policy URL (dùng `https://sporthub-dn.id.vn/Privacy`), (b) chuyển App Mode → Live, (c) Facebook Login > Settings: thêm redirect URI ở trên.
- **Phương án MVP nếu không muốn xử lý FB app:** ẩn nút Facebook ở Login/Register (comment lại, giữ code) — quyết định khi duyệt plan.

### Acceptance criteria Phase D
- [ ] `POST /api/admin/test-email` trả 200 + mail thật về hộp thư (sau khi verify domain)
- [ ] Facebook login hoặc hoạt động với tài khoản thường, hoặc nút bị ẩn (theo quyết định)

---

## PHASE E — Onboarding tour lần đầu đăng nhập

### Thiết kế
- **Tự viết vanilla JS ~150 dòng** (`wwwroot/js/sporthub-tour.js`) — không thêm dependency: overlay tối + spotlight (box-shadow cutout quanh element target) + tooltip card có nút "Tiếp theo / Bỏ qua".
- **Trigger:** `LoginCount == 1` (server render flag vào `_Layout`) && `!localStorage.tourDone`. Nút "Bỏ qua" → `localStorage.tourDone=1`.
- **Các bước tour (trang chủ / matchmaking):**
  1. Nút **"Tạo trận đấu"** — "Đăng kèo tìm người chơi cùng"
  2. Tab **"Ghép trận"** — "Tìm và tham gia trận quanh bạn"
  3. **Bộ lọc** — "Lọc theo môn, trình độ, khu vực, giờ"
  4. **Ví xu** trên navbar — "Nạp xu để đặt cọc & thanh toán phí"
  5. **Chuông thông báo** — "Theo dõi duyệt trận, nhắc lịch tại đây"
  6. **Avatar → Hồ sơ** — "Cập nhật trình độ để ghép trận chuẩn hơn"
- Re-chạy được: thêm mục "Xem lại hướng dẫn" trong dropdown avatar (xóa localStorage flag + reload).
- Mobile: các bước trỏ vào bottom-nav item tương ứng (selector riêng theo breakpoint).

### Acceptance criteria Phase E
- [ ] Tài khoản mới đăng nhập lần đầu → tour tự chạy, highlight đúng 6 điểm
- [ ] "Bỏ qua" → không bao giờ tự hiện lại; "Xem lại hướng dẫn" chạy lại được

---

## PHASE F — Hoàn thiện luồng khiếu nại (evidence + nhân chứng + AI)

### Hiện trạng
`DisputeService` (129 dòng): submit (type, description, evidenceUrl đơn) → admin xem list → resolve (refund) / dismiss. Thiếu: upload ảnh thật, xác minh từ người chơi khác, hỗ trợ AI, deadline.

### Kiến trúc mới

```
Người khiếu nại ──> Submit (mô tả + tối đa 3 ảnh)
                        │
                        ▼
        Hệ thống gửi Notification + form xác minh
        cho MỌI participant Accepted khác + host của trận
                        │            (deadline 48h)
                        ▼
        DisputeWitnessResponses (đồng ý / phản đối / ý kiến + ảnh)
                        │
                        ▼
        AI (Groq) tổng hợp: mô tả + phản hồi nhân chứng
        → JSON: {score 0-100, verdict, summary, reasons[]}
                        │
                        ▼
        Admin UI: xem AI summary + evidence + witness responses
        → Resolve / Dismiss (quyết định cuối vẫn là admin)
```

### F1 — Upload ảnh bằng chứng
- Controller mới `DisputeUploadController` (pattern copy từ `ChatUploadController`): `POST /api/disputes/upload` → lưu `wwwroot/uploads/disputes/`, validate jpg/png/webp ≤ 2MB, tối đa 3 ảnh/dispute.
- `MatchDisputes.EvidenceUrl` (NVARCHAR(MAX), đã có từ v5) lưu JSON array `["url1","url2"]`.
- Form khiếu nại ở `Details.cshtml` (modal report hiện có): thêm input file multiple + preview thumbnail.

### F2 — Xác minh nhân chứng
- **Bảng mới `DisputeWitnessResponses`** (vào `database_upgrade_v6.sql` + migration EF):
  ```sql
  Id INT IDENTITY PK,
  DisputeId INT NOT NULL FK->MatchDisputes(Id) ON DELETE CASCADE,
  WitnessUserId INT NOT NULL FK->Users(UserID),
  Stance NVARCHAR(20) NOT NULL,        -- Support | Oppose | Neutral
  Comment NVARCHAR(1000) NULL,
  EvidenceUrl NVARCHAR(MAX) NULL,      -- JSON array ảnh
  RespondedAt DATETIME2 NULL,          -- NULL = chưa phản hồi
  CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  UNIQUE (DisputeId, WitnessUserId)
  ```
- Khi `SubmitDisputeAsync`: tạo sẵn 1 row/nhân chứng (participants Accepted + host, trừ người khiếu nại) + gửi Notification type `DisputeWitness` (link `/Disputes/Respond?id=`) + email (nếu NotifyByEmail).
- Page mới `Pages/Disputes/Respond.cshtml(.cs)`: hiện tóm tắt khiếu nại (ẩn danh người báo) + 3 lựa chọn Stance + comment + ảnh. Chỉ user có row witness mới vào được.
- `PendingJoinExpiryHostedService` thêm bước 7: quá 48h từ CreatedAt → đánh dấu các response chưa trả lời là hết hạn (giữ NULL RespondedAt), trigger F3 chạy AI với dữ liệu đang có.

### F3 — AI chấm điểm khiếu nại
- `IAiChatService` thêm method `AnalyzeDisputeAsync(DisputeAnalysisInput input)`:
  - Input: loại khiếu nại, mô tả, số ảnh, danh sách phản hồi nhân chứng (stance + comment), tỉ lệ phản hồi.
  - Prompt Groq (llama-3.1-8b-instant, JSON mode) → output: `{ "credibilityScore": 0-100, "suggestedResolution": "FullRefund|PartialRefund|NoRefund|Dismiss", "summary": "...", "keyPoints": ["..."] }`.
  - Reuse circuit breaker + cache hiện có của `AiChatService`. Groq sập → dispute vẫn xử lý tay bình thường (AI là phụ trợ).
- Cột mới trên `MatchDisputes`: `AiScore INT NULL`, `AiSummary NVARCHAR(MAX) NULL` (JSON) — vào v6.
- Chạy AI khi: (a) đủ 100% nhân chứng phản hồi, hoặc (b) hết deadline 48h. Chạy trong hosted service (không block request).

### F4 — Admin UI nâng cấp (`Pages/Admin/Disputes.cshtml`)
- Detail view mỗi dispute: gallery ảnh evidence, bảng phản hồi nhân chứng (ai/stance/comment/ảnh), card AI (score màu theo mức + summary + gợi ý resolution + key points, badge "AI chưa chạy/lỗi" khi null).
- Nút "Chạy lại AI" (force re-analyze).
- Giữ nguyên flow Resolve/Dismiss — admin quyết định cuối.

### F5 — Notification types mới
Thêm `DisputeWitness`, `DisputeResolved` vào `CK_Notifications_Type` (v6 script — **rút kinh nghiệm sự cố constraint promo vừa rồi**) + snapshot EF.

### Acceptance criteria Phase F
- [ ] Khiếu nại kèm 3 ảnh submit được; nhân chứng nhận notification + form trong vòng 1 phút
- [ ] Đủ phản hồi hoặc hết 48h → AI score + summary xuất hiện trong admin
- [ ] Groq lỗi → khiếu nại vẫn xử lý tay được, không crash
- [ ] Admin resolve → người liên quan nhận notification kết quả

---

## Scripts/database_upgrade_v6.sql (gộp toàn bộ)

1. `Matches.CourtNumber NVARCHAR(30) NULL` (B3)
2. Bảng `DisputeWitnessResponses` + index `(DisputeId)`, unique `(DisputeId, WitnessUserId)` (F2)
3. `MatchDisputes.AiScore INT NULL`, `MatchDisputes.AiSummary NVARCHAR(MAX) NULL` (F3)
4. Rebuild `CK_Notifications_Type` thêm `DisputeWitness`, `DisputeResolved` (F5)
5. Idempotent (IF NOT EXISTS) như v5; INSERT tương ứng vào `__EFMigrationsHistory` cho các migration EF đi kèm (giữ sổ sách đồng bộ — bài học từ sự cố vừa rồi)

## Migration EF tương ứng (chạy local bằng /migrate)
- `AddMatchCourtNumber`
- `AddDisputeWitnessAndAi`

---

## Thứ tự triển khai đề xuất

1. **A** (popup + join reason) → tác động lớn nhất tới user mới
2. **B** (filter + sân số) → lỗi user thấy rõ hằng ngày
3. **C** (audit fixes) → gộp chung commit với A/B vì chạm cùng file
4. **D** (Resend/Facebook) → cần bạn thao tác dashboard song song
5. **E** (tour) → độc lập, làm khi A-D xong
6. **F** (dispute) → lớn nhất, làm cuối, 1 commit riêng + v6

## Việc cần BẠN / H làm (ngoài code)
- [ ] Resend dashboard: verify domain `sporthub-dn.id.vn` (3 DNS records)
- [ ] Facebook App: Live Mode + Privacy Policy URL + redirect URI — **hoặc quyết định ẩn nút FB**
- [ ] Sau khi code xong: chạy `database_upgrade_v6.sql` trên production (đã có quyền adminhai)
- [ ] H deploy code từ `dev` sau mỗi phase hoặc gộp cuối

## Rủi ro & lưu ý
- Đổi chữ ký `JoinMatchAsync` chạm nhiều call site → build + test kỹ 2 luồng join (Details + Index quick-join)
- Popup hồ sơ: KHÔNG chặn cứng (vẫn bỏ qua được) — tránh làm phiền user cũ đã có ý định riêng
- AI dispute chỉ là **phụ trợ** — mọi quyết định tiền bạc vẫn qua admin, không auto-refund theo AI
- Tổng migration mới: 2 (CourtNumber, DisputeWitnessAndAi) — đều additive, không phá dữ liệu cũ
