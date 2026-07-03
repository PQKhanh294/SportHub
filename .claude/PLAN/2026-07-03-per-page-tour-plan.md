# PLAN — Hướng dẫn riêng theo từng trang (Per-page Tour)

> Ngày lập: 2026-07-03 · Trạng thái: **CHỜ DUYỆT**
> Bối cảnh: Tour hiện tại (Phase E) chỉ highlight 5 điểm trên navbar, chạy 1 lần lúc đăng nhập đầu.
> Yêu cầu mới: mỗi trang chức năng cốt lõi có bộ hướng dẫn riêng, tự chạy lần đầu user vào trang đó.

---

## Kiến trúc

### Nâng cấp `wwwroot/js/sporthub-tour.js` → registry theo trang
- Từ 1 mảng `steps` cứng → object `pageTours`, mỗi entry gồm: pattern match URL + danh sách bước.
- Tự nhận diện trang qua `location.pathname` (không phân biệt hoa thường, bỏ query string).
- **Trigger tự động:** lần ĐẦU user vào mỗi trang → tour trang đó tự chạy sau 800ms. Đánh dấu đã xem bằng `localStorage["spTour:<key>"]` — mỗi trang 1 flag riêng.
- **Không chồng chéo:** nếu popup hoàn thiện hồ sơ (`pcmOverlay`) đang mở → hoãn tour (thử lại sau khi popup đóng). Tour navbar (first login) chạy trước; tour trang chạy ở lượt truy cập kế tiếp.
- **Chạy lại:** nút nổi **"?"** góc phải-dưới (chỉ hiện trên trang có tour) → chạy lại tour trang hiện tại bất kỳ lúc nào. Mục "Xem lại hướng dẫn" trong menu avatar giữ nguyên (chạy tour navbar).
- Bước nào element ẩn (mobile/desktop khác nhau, chưa đăng nhập...) → tự bỏ qua, không lỗi.
- Toàn bộ vanilla JS, không dependency mới — mở rộng engine spotlight sẵn có.

---

## Nội dung tour từng trang (5 trang cốt lõi)

### 1. `/Matchmaking` — Ghép trận (6 bước)
| # | Anchor | Nội dung hướng dẫn |
|---|--------|--------------------|
| 1 | Tabs trạng thái (thêm `id="mmStatusTabs"`) | "Chuyển giữa trận Đang mở / Đã tham gia / Kèo của tôi" |
| 2 | `#filterSidebar` (có sẵn) | "Lọc theo môn, trình độ (nhấn (i) xem thang điểm), khu vực, giờ, chi phí" |
| 3 | Card trận đầu tiên (thêm `class="mm-card"` anchor) | "Mỗi card hiển thị điểm phù hợp, khoảng cách, giá/người và số chỗ" |
| 4 | Nút "Gửi yêu cầu" trên card | "Gửi yêu cầu tham gia — host duyệt trong 2h, sau đó thanh toán phí giữ chỗ 5.000 xu" |
| 5 | Nút bản đồ/toggle view (nếu có) | "Xem các trận trên bản đồ để chọn sân gần bạn" |
| 6 | Nút Tạo trận trên navbar (`#nav-create-match`) | "Không thấy kèo phù hợp? Tự tạo trận của bạn" |

### 2. `/Matchmaking/Create` — Tạo trận (5 bước)
| # | Anchor | Nội dung |
|---|--------|----------|
| 1 | `#tabQuick` + `#tabFull` (có sẵn) | "Chế độ Nhanh: 5 trường cơ bản. Đầy đủ: tùy chỉnh chi tiết" |
| 2 | `#quickSportSelect` | "Chọn môn — loại trận & thang trình độ tự đổi theo môn" |
| 3 | `#courtSearchInput` (form đầy đủ) | "Gõ tên sân để tìm — tự điền địa chỉ, không cần nhớ ID" |
| 4 | `#splitFeeSection` (cầu lông) | "Bật Chia đều tiền — hệ thống tự tính xu/người" |
| 5 | `#aiDescBtn` | "Để AI viết mô tả trận hấp dẫn giúp bạn" |

### 3. `/Matchmaking/Details` — Chi tiết trận (5 bước)
| # | Anchor | Nội dung |
|---|--------|----------|
| 1 | Khối thông tin trận (thêm `id="mdInfoCard"`) | "Thông tin sân, sân số, giờ đấu và chi phí" |
| 2 | Nút Tham gia (thêm `id="mdJoinBtn"`) | "Gửi yêu cầu → host duyệt → thanh toán 5.000 xu để giữ chỗ" |
| 3 | Danh sách người tham gia (thêm `id="mdParticipants"`) | "Sau khi được duyệt, bạn thấy Zalo/SĐT người cùng trận để liên hệ" |
| 4 | Nút Khiếu nại (thêm `id="mdReportBtn"`) | "Có vấn đề sau trận? Gửi khiếu nại kèm ảnh — người trong trận sẽ xác minh" |
| 5 | Bản đồ (thêm `id="mdMap"` nếu chưa có) | "Vị trí sân — nhấn để mở chỉ đường" |

### 4. `/Wallet` — Ví xu (5 bước)
| # | Anchor | Nội dung |
|---|--------|----------|
| 1 | Khối số dư (thêm `id="wlBalance"`) | "Số dư xu — dùng đặt cọc tạo trận & phí tham gia" |
| 2 | Nút/link Nạp xu (thêm `id="wlTopUp"`) | "Nạp qua chuyển khoản QR — tiền vào ví tự động trong ~5 giây" |
| 3 | `#promoCodeInput` (có sẵn) | "Nhập mã khuyến mãi để nhận xu miễn phí" |
| 4 | Khối voucher (thêm `id="wlVouchers"`) | "Voucher cá nhân được tặng từ sự kiện nằm ở đây" |
| 5 | Lịch sử giao dịch (thêm `id="wlHistory"`) | "Mọi biến động xu đều được ghi lại minh bạch" |

### 5. `/Profile` — Hồ sơ (4 bước)
| # | Anchor | Nội dung |
|---|--------|----------|
| 1 | Nút Chỉnh sửa (thêm `id="pfEditBtn"`) | "Cập nhật SĐT, Zalo, khu vực tại đây" |
| 2 | Khối môn & trình độ (thêm `id="pfSkills"`) | "Trình độ từng môn quyết định bạn được ghép trận nào — điền chính xác nhé" |
| 3 | Khối huy hiệu (thêm `id="pfBadges"`) | "Chơi càng nhiều, huy hiệu càng nhiều — tăng uy tín với host" |
| 4 | Khối đánh giá (thêm `id="pfReviews"`) | "Điểm đánh giá từ các trận đã chơi hiển thị ở đây" |

---

## Files thay đổi

| File | Thay đổi |
|------|----------|
| `wwwroot/js/sporthub-tour.js` | Refactor registry + nút "?" nổi + per-page localStorage |
| `Pages/Shared/_Layout.cshtml` | Không đổi logic (script đã load toàn site) |
| `Pages/Matchmaking/Index.cshtml` | Thêm 2-3 id anchor |
| `Pages/Matchmaking/Create.cshtml` | Anchors đã có sẵn gần đủ, thêm 0-1 id |
| `Pages/Matchmaking/Details.cshtml` | Thêm 4 id anchor |
| `Pages/Wallet/Index.cshtml` | Thêm 4 id anchor |
| `Pages/Profile/Index.cshtml` | Thêm 4 id anchor |

**Không cần:** DB migration, backend, package mới. Effort: **~3h**.

---

## Hành vi tổng hợp sau khi làm xong

1. User mới đăng nhập lần đầu → tour navbar 5 bước (như hiện tại)
2. Lần đầu vào `/Matchmaking` → tour 6 bước của trang đó tự chạy (1 lần duy nhất)
3. Tương tự cho 4 trang còn lại — mỗi trang tự giới thiệu lần đầu ghé
4. Góc phải-dưới các trang này có nút **"?"** nhỏ → xem lại hướng dẫn trang bất kỳ lúc nào
5. "Bỏ qua" ở bất kỳ bước nào → tắt tour trang đó vĩnh viễn (vẫn xem lại được qua nút "?")

## Acceptance criteria
- [ ] Vào lần đầu mỗi trang trong 5 trang → tour đúng nội dung trang đó, không lặp lại lần sau
- [ ] Nút "?" hiện đúng 5 trang có tour, không hiện trang khác
- [ ] Mobile: các bước trỏ element ẩn được bỏ qua êm, không vỡ layout
- [ ] Tour không đè lên popup hoàn thiện hồ sơ
- [ ] Không lỗi console ở trang không có tour
