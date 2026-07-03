# BÁO CÁO AUDIT UI/UX — SportHub

> Ngày: 2026-07-03 · Phạm vi: Song ngữ VI/EN · Dark mode · Mobile · Admin & User · Chất thể thao
> Mọi phát hiện đều có dẫn chứng file:dòng trong code.

---

## Tóm tắt điều hành

| Mảng | Điểm | Nhận định 1 dòng |
|------|------|------------------|
| Song ngữ VI/EN | 🔴 4/10 | Kiến trúc dịch bằng string-replace là gốc rễ mọi lỗi trộn ngôn ngữ — không sửa vặt được triệt để |
| Dark mode | 🟡 6.5/10 | Nền tảng tốt (4 theme), nhưng có bug chữ đen-nền-đen và các component JS bỏ quên dark |
| Mobile (user) | 🟢 7.5/10 | Đầu tư tốt: bottom nav, filter drawer, card responsive — còn vài điểm lệch |
| Mobile (admin) | 🟡 5/10 | Desktop-first, có bảng tràn ngang, khó thao tác trên phone |
| Chất thể thao | 🟡 5/10 | Giao diện "SaaS dashboard sạch" hơn là sân đấu — font thể thao đã load mà không dùng |

---

## A. SONG NGỮ VI/EN — vấn đề lớn nhất

### A1. Kiến trúc hiện tại (gốc rễ của mọi lỗi trộn ngôn ngữ)

Toàn bộ hệ thống dịch nằm ở [UiLocalizationMiddleware.cs](../../Middleware/UiLocalizationMiddleware.cs): **buffer toàn bộ HTML của mỗi response rồi string-replace 2 chiều** dựa trên từ điển ~190 cặp câu (dòng 125+). Hệ quả tất yếu:

**(1) Lỗ hổng coverage — nguyên nhân trực tiếp của hiện tượng bạn thấy.** Chuỗi nào không có trong từ điển thì giữ nguyên ngôn ngữ lúc viết code:
- Admin viết tay **tiếng Anh** ở nhiều chỗ → mode VI vẫn hiện tiếng Anh:
  - [Disputes.cshtml:17-23](../../Pages/Admin/Disputes.cshtml) — `StatusLabel` trả cứng "Pending / Under Review / Resolved / Auto-closed / Dismissed" bất kể ngôn ngữ
  - Disputes.cshtml:174-208 — cả modal xử lý hardcode EN: "Resolve dispute", "Note to user", "Outcome", "Full refund", "Refund amount (if any)", "Cancel"
  - "Search by..." placeholder ở 6 trang admin (Users:47, Wallet:34, Transactions:39, Subscriptions:75, Reviews:29, ReportedMessages:74)
  - [Promotions.cshtml:254](../../Pages/Admin/Promotions.cshtml) — "New users (LoginCount ≤ 1)" trong dropdown
  - Nút phân trang "Prev/Next" (Disputes:159-164)
- Ngược lại, chuỗi VI mới thêm (AI card khiếu nại, bảng nhân chứng, popup hồ sơ...) chưa vào từ điển → **mode EN vẫn hiện tiếng Việt**.

**(2) Text render bằng JavaScript không bao giờ được dịch.** Middleware chủ động bỏ qua nội dung trong `<script>` (dòng 78-82, đúng kỹ thuật để không phá JS) — nghĩa là mọi UI sinh từ JS giữ nguyên tiếng Việt ở mode EN: toast ví ("Lỗi kết nối. Vui lòng thử lại."), tour hướng dẫn, popup hoàn thiện hồ sơ, skill guide modal, kết quả AI chat, court autocomplete...

**(3) Dịch đè cả nội dung người dùng — nguy hiểm nhất.** Mode VI chạy Replace EN→VI trên **toàn bộ text** của trang, gồm cả dữ liệu user nhập: trận tên "Home game" sẽ hiển thị thành "Trang chủ game". Bằng chứng lỗi này đã từng xảy ra: dòng 118-119 có patch thủ công `"Môn thể thao Hub" → "Sport Hub"` — tức brand "Sport Hub" từng bị dịch nhầm thành "Môn thể thao Hub" và được vá kiểu whack-a-mole.

**(4) Hiệu năng tệ.** Mỗi text node của mỗi request chạy `Entries.OrderByDescending(...)` (sort lại 190 phần tử mỗi lần gọi — dòng 98) rồi ~190×2 lần `Replace` trên chuỗi. Trang Matchmaking HTML rất dài → CPU tốn vô ích trên mọi request.

**(5) Hai hệ thống song song không nhất quán.** 12 trang admin tự làm song ngữ bằng biến `isEn` (208 chỗ) nhưng chỉ làm nửa vời — nửa trang dùng `isEn`, nửa hardcode EN → chính là lỗi trộn bạn thấy. Trong khi đó chỉ duy nhất Settings dùng chuẩn `IStringLocalizer`.

### A2. Khuyến nghị (chọn 1 trong 2 chiến lược)

**Chiến lược MVP (khuyến nghị — ~4h):**
1. **Tuyên bố Admin chỉ dùng tiếng Việt** — admin là công cụ nội bộ của chính bạn, song ngữ ở đây là chi phí không có người hưởng. Xóa toàn bộ `isEn` trong Pages/Admin, viết thuần Việt. Hết vĩnh viễn lỗi trộn ở admin.
2. **Tắt chiều dịch EN→VI** trong middleware (xóa nhánh `else` dòng 108-115) — nguồn code đã là tiếng Việt, chiều này chỉ tồn tại để vá các chỗ hardcode EN (sẽ hết sau bước 1) và đang là thủ phạm dịch đè nội dung user.
3. Bổ sung từ điển cho các chuỗi user-facing mới (popup hồ sơ, khiếu nại, message join trận mới).
4. Chấp nhận: text từ JS không dịch — ghi nhận là hạn chế MVP.

**Chiến lược chuẩn dài hạn (~2-3 ngày, làm sau MVP):** chuyển dần sang `IStringLocalizer` + file `.resx` (mẫu đã có ở Settings), bỏ hẳn middleware. Đúng chuẩn ASP.NET, dịch được cả JS (bơm dict JSON), không đè nội dung user, không tốn CPU.

---

## B. DARK MODE

### B1. Cơ chế hiện tại — thiết kế khá tốt
- 4 mức theme: `light / dark / dark-2 (đậm hơn) / dark-3 (đen thuần)` — lưu cookie, apply sớm trong `<head>` ([_Layout:64-71](../../Pages/Shared/_Layout.cshtml)) nên **không bị chớp trắng (FOUC)** ✓
- dark-2/dark-3 hoạt động bằng CSS override `!important` đè lên một số class (`_Layout:76-89`) — chỉ đè `bg-slate-900/800` và border → component nào dùng class khác sẽ không nhận được mức tối tương ứng (rủi ro lệch tông giữa các card).

### B2. Bug cụ thể tìm thấy
| Mức độ | Lỗi | Vị trí | Chi tiết |
|--------|-----|--------|----------|
| 🔴 Cao | **Chữ đen trên nền đen** ở gợi ý địa chỉ | [location-picker.js:104-106](../../wwwroot/js/location-picker.js) | Item gợi ý set cứng `style="color:#1e293b"` (đen) nhưng container là `dark:bg-slate-800` → dark mode gần như không đọc được. Ảnh hưởng: Tạo trận, Sửa hồ sơ, popup hồ sơ |
| 🟡 Vừa | Tooltip tour trắng cứng | [sporthub-tour.js:104](../../wwwroot/js/sporthub-tour.js) | `background:#fff` + chữ slate cứng — đọc được nhưng chói/lệch tông trong dark mode |
| 🟡 Vừa | dark-2/dark-3 chỉ đè 2 class nền | _Layout:76-89 | Card dùng `dark:bg-slate-950`, gradient, `bg-white/10`... không được đè → 3 mức dark trông gần giống nhau ở nhiều trang |
| 🟢 Nhỏ | Email HTML không có dark scheme | ResendEmailService | Chấp nhận được với MVP |

### B3. Khuyến nghị
- Fix ngay bug chữ đen (5 phút: đổi `style="color:#1e293b"` thành class `text-slate-800 dark:text-slate-100`).
- Tooltip tour: thêm nhận diện `document.documentElement.classList.contains('dark')` để đổi nền/chữ (15 phút).
- Cân nhắc: 4 mức theme có thật sự cần? 2 mức (light/dark) ít bug hơn — dark-2/dark-3 hiện tạo khác biệt rất nhỏ so với chi phí duy trì.

---

## C. MOBILE

### C1. Phía user — điểm mạnh thật sự
- Bottom navigation thay hamburger ✓ (đúng pattern app thể thao)
- Filter là drawer riêng trên mobile với overlay ✓
- Match card có layout mobile riêng biệt (ảnh nhỏ, text rút gọn "Gửi" thay "Gửi yêu cầu") ✓
- Popup hồ sơ, modal khiếu nại có `max-h + overflow-y` ✓

### C2. Vấn đề tìm thấy
| Mức độ | Vấn đề | Vị trí |
|--------|--------|--------|
| 🟡 | **Tour navbar trên mobile gần như trống** — 3/5 anchor bị ẩn trên mobile (`#nav-create-match` = hidden xl, `#nav-matchmaking-desktop` = hidden lg, `#nav-wallet-btn` = hidden md) → user mobile đăng nhập lần đầu chỉ thấy tour 2 bước (chuông + avatar) | sporthub-tour.js navSteps |
| 🟡 | Admin [Reviews.cshtml](../../Pages/Admin/Reviews.cshtml) có `<table>` **không bọc** `overflow-x-auto` → tràn ngang trên phone (các trang admin khác đã bọc) | Reviews.cshtml |
| 🟡 | Admin tổng thể desktop-first: bảng nhiều cột, touch target nhỏ, modal rộng — admin thao tác bằng phone sẽ khổ (chấp nhận được nếu admin luôn dùng laptop) | Pages/Admin/* |
| 🟢 | Navbar mobile giờ có 3 icon (?, chuông, avatar) — hơi đông nhưng ổn | _Layout |

### C3. Khuyến nghị
- Thêm bộ `navStepsMobile` trỏ vào các item bottom-nav (30 phút) để tour first-login mobile có ý nghĩa.
- Bọc `overflow-x-auto` cho bảng Reviews (5 phút).
- Admin mobile: không đầu tư thêm cho MVP — ghi nhận "admin dùng desktop".

---

## D. CHẤT THỂ THAO — vì sao web "chưa giống website thể thao"

### D1. Chẩn đoán
Giao diện hiện tại là **SaaS dashboard sạch sẽ**: Inter + card trắng bo góc + slate xám. Sạch nhưng **không có năng lượng**. Cụ thể:

1. **Font thể thao đã load nhưng không dùng** — `Barlow Condensed` (font condensed đậm chất athletic) được khai báo là `font-sport` trong tailwind config (_Layout:45) nhưng cả codebase chỉ dùng **3 chỗ**. Đang trả phí băng thông tải font mà không lấy giá trị.
2. **Số liệu không có "chất scoreboard"** — các con số (16 người chơi, 2 trận/tuần, tỉ số 4.6★) hiển thị bằng font thường, không tạo cảm giác bảng điểm sân đấu.
3. **Màu sắc không phân biệt môn thể thao** — mọi card trận đều teal/trắng như nhau; cầu lông, bóng đá, tennis không có nhận diện riêng.
4. **Thiếu texture/imagery thể thao** — ngoài hero banner và background mờ trên card cầu lông, các section còn lại là nền phẳng; empty state chỉ có icon xám.
5. **Thiếu motion** — mọi thứ tĩnh; thể thao = chuyển động.

### D2. Hướng "thể thao hóa" cụ thể (không đổi stack, thuần Tailwind + font đã có)

| # | Việc | Effort | Tác động |
|---|------|--------|----------|
| 1 | Áp `font-sport` (Barlow Condensed, uppercase, tracking-wide) cho: mọi heading trang, tên trận trên card, số liệu stats, số dư ví | 1h | ⭐⭐⭐ Đổi cảm giác toàn site ngay lập tức |
| 2 | **Sport color coding**: mỗi môn 1 màu nhận diện (cầu lông xanh lá sân, bóng đá cỏ đậm, tennis vàng đất nện, pickleball tím, bóng bàn cam) — dải màu 4px bên trái card trận + badge môn cùng màu | 1.5h | ⭐⭐⭐ Quét mắt là biết môn gì |
| 3 | **Stats kiểu scoreboard**: khối số liệu trang chủ + admin KPI dùng font condensed cỡ lớn, nền navy, số màu accent — như bảng tỉ số | 1h | ⭐⭐ |
| 4 | **Court-line pattern**: SVG nét kẻ sân (đường biên, vòng tròn giữa sân) mờ 4-6% làm background các section lớn thay nền phẳng | 1h | ⭐⭐ Nhìn là thấy "sân đấu" |
| 5 | Empty states có minh họa môn thể thao (emoji lớn + câu CTA giọng thể thao: "Chưa có kèo nào — làm chủ sân trước đi!") | 45' | ⭐⭐ |
| 6 | Micro-motion: nút "Tạo trận đấu" pulse nhẹ, card hover nghiêng 0.5deg, badge "sắp diễn ra" nhấp nháy chấm đỏ live | 45' | ⭐ |
| 7 | Giọng văn UI: đổi các label trung tính sang giọng thể thao ("Tham gia" → "Vào kèo", "Người tham gia" → "Đội hình"...) — cần cân nhắc vì ảnh hưởng từ điển dịch | 1h | ⭐ tùy khẩu vị |

Tổng gói "thể thao hóa": **~6-7h** cho mục 1-6.

---

## E. LỘ TRÌNH ĐỀ XUẤT

### P0 — Bug thấy ngay (~1h)
1. Fix chữ đen nền đen ở location-picker (dark mode)
2. Bọc overflow bảng admin Reviews
3. Tooltip tour nhận diện dark mode

### P1 — Dứt điểm song ngữ theo chiến lược MVP (~4h)
4. Admin chuyển thuần tiếng Việt (xóa isEn + sửa hardcode EN)
5. Tắt chiều dịch EN→VI trong middleware (hết dịch đè nội dung user)
6. Bổ sung từ điển cho chuỗi user-facing mới
7. Tour mobile: bộ bước riêng cho bottom-nav

### P2 — Thể thao hóa giao diện (~6-7h)
8. Gói D2 mục 1-6 (font sport, sport colors, scoreboard, court pattern, empty states, motion)

### Sau MVP
9. Di cư dần sang IStringLocalizer/.resx, gỡ middleware dịch
10. Cân nhắc gộp 4 theme còn 2
