# SportHub — Kế hoạch Test Toàn Diện

> Cập nhật: 2026-07-07. Test trên **cả desktop lẫn điện thoại thật** (DevTools giả lập
> KHÔNG mô phỏng đúng bàn phím ảo/visualViewport). Mỗi mục tick ✅ khi pass, ghi chú lỗi nếu fail.
>
> Chuẩn bị: 2 tài khoản email thường (1 mới tinh, 1 đã dùng lâu), 1 tài khoản Google,
> 1 tài khoản Admin, 1 tài khoản CourtOwner. Ví có sẵn xu để test thanh toán.

---

## 1. Xác thực & Tài khoản

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 1.1 | Đăng ký email mới → nhận OTP → nhập đúng mã | Vào Onboarding, tài khoản EmailConfirmed |
| 1.2 | Đăng ký với email đã tồn tại | Báo "Email is already in use", không tạo trùng |
| 1.3 | Nhập sai OTP / OTP hết hạn (>10 phút) | Báo lỗi rõ ràng, không đăng nhập được |
| 1.4 | Gửi lại mã trước 60s | Bị chặn cooldown; sau 60s gửi lại được |
| 1.5 | Đăng nhập tài khoản cũ (tạo trước tính năng OTP) | Bị chuyển sang trang xác thực email trước khi vào |
| 1.6 | Đăng nhập Google (tài khoản mới + tài khoản cũ cùng email) | Không cần OTP, tự EmailConfirmed, liên kết đúng tài khoản cũ |
| 1.7 | Quên mật khẩu: email có tài khoản / không có tài khoản | Cùng 1 thông báo thành công (chống dò email); mail chỉ gửi khi có tài khoản |
| 1.8 | Link đặt lại mật khẩu hết hạn (>1h) hoặc dùng lại lần 2 | Báo link không hợp lệ |
| 1.9 | Đăng nhập sai mật khẩu nhiều lần | (Nếu có rate limit) bị chặn tạm; tối thiểu không lộ thông tin gì thêm |
| 1.10 | Tài khoản bị khóa (IsActive=false) / bị ban | Không vào được, chuyển trang Banned đúng |

## 2. Onboarding & Hoàn thiện hồ sơ

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 2.1 | Sau xác thực OTP đăng ký → Onboarding 3 bước | Chọn môn → trình độ theo môn → địa chỉ (CÓ gợi ý autocomplete) + SĐT |
| 2.2 | Gõ địa chỉ ≥3 ký tự ở bước 3 | Dropdown gợi ý hiện, chọn xong lưu cả lat/lon |
| 2.3 | Hoàn tất Onboarding đầy đủ (môn+trình độ+địa chỉ+SĐT) | Vào Matchmaking, **popup "Hoàn thiện hồ sơ" KHÔNG hiện nữa** |
| 2.4 | Onboarding bấm "Bỏ qua" | Vào trang chính, popup hoàn thiện hồ sơ hiện, **prefill đúng những gì đã điền** (nếu có) |
| 2.5 | Popup hoàn thiện hồ sơ với user đã có môn/trình độ/địa chỉ | Chip môn được chọn sẵn, trình độ đúng, SĐT/địa chỉ điền sẵn |
| 2.6 | Đăng ký Google mới → popup hoàn thiện hồ sơ | Autocomplete địa chỉ hoạt động, lưu xong popup không hiện lại |
| 2.7 | Nút "AI đánh giá trình độ" trong popup | Trả kết quả, không treo nút khi lỗi mạng |

## 3. Matchmaking (luồng lõi)

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 3.1 | Tạo trận chế độ Nhanh + chế độ Đầy đủ | Đủ validation, đặt cọc trừ ví đúng, trận hiện trong danh sách |
| 3.2 | Join trận → host duyệt → thanh toán giữ chỗ | Trừ đúng 5.000 xu, trạng thái cập nhật real-time (SignalR) |
| 3.3 | Host từ chối yêu cầu | Người xin join nhận thông báo, không bị trừ xu |
| 3.4 | Yêu cầu join quá 2h không duyệt | Tự hết hạn (PendingJoinExpiryHostedService) |
| 3.5 | Host hủy trận có người đã thanh toán | Hoàn xu đúng người, email + notification gửi đủ |
| 3.6 | Trận đủ người | Chuyển trạng thái Full, không join thêm được |
| 3.7 | Bộ lọc: môn/trình độ/khu vực/giờ/giá/bán kính (gói Starter+) | Kết quả đúng, user Free thấy khóa bán kính |
| 3.8 | StatusFilter: Đã tham gia / Chờ duyệt / Trận của tôi | Danh sách đúng theo user |
| 3.9 | Khiếu nại sau trận (dispute) + admin xử lý | Tạo được kèm ảnh, auto-resolve rules chạy đúng |
| 3.10 | 2 người cùng join slot cuối cùng đồng thời | Chỉ 1 người vào được (không âm slot) |

## 4. Ví & Thanh toán

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 4.1 | Nạp xu QR SePay → chuyển khoản thật | Ví cộng tự động vài giây (webhook), navbar cập nhật không cần F5 |
| 4.2 | Lệnh nạp quá hạn | Chuyển Expired, không cộng xu |
| 4.3 | Webhook gửi trùng (replay) | Không cộng xu 2 lần (idempotent theo TransactionRef) |
| 4.4 | Mã khuyến mãi: hợp lệ / sai / hết lượt / hết hạn / dùng lại | Thông báo đúng từng trường hợp; UseCount không vượt MaxUses khi bấm đồng thời |
| 4.5 | Voucher: dùng, dùng lại lần 2 | Cộng xu 1 lần duy nhất |
| 4.6 | Lịch sử giao dịch | Đủ mọi biến động, số dư khớp tổng |

## 5. Chat & AI

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 5.1 | Chat 1-1: gửi text/ảnh/thẻ trận, reply, pin, forward, báo cáo, xóa | Hoạt động 2 chiều real-time; badge unread đúng |
| 5.2 | Mobile: mở cuộc trò chuyện | **Fullscreen toàn màn hình**, không thấy header site/bottom nav; back về danh sách |
| 5.3 | Mobile: mở bàn phím trong chat | Header tên người chat luôn hiển thị, input ngay trên bàn phím |
| 5.4 | Trong chat fullscreen mở modal Chuyển tiếp/Báo cáo | Modal nổi TRÊN khung chat (không bị che) |
| 5.5 | AI chatbot desktop: popup nhỏ, mở rộng, thu nhỏ, X | Như cũ, không đổi |
| 5.6 | AI chatbot mobile: nhấn FAB | Mở fullscreen ngay, X đóng hẳn, không có nút thu nhỏ |
| 5.7 | AI chatbot mobile + bàn phím | Header "SportHub AI" không bị đẩy khuất |
| 5.8 | AI trả lời khi Groq lỗi/hết quota | Thông báo lịch sự, không treo nút gửi |

## 6. Mobile UI/UX tổng quát

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 6.1 | Mọi form (đăng ký, đăng nhập, OTP, tạo trận, profile edit): focus từng ô khi bàn phím mở | Ô đang nhập tự cuộn vào giữa màn hình, không bị bàn phím che |
| 6.2 | Ô địa chỉ có autocomplete (Onboarding, popup hồ sơ, tạo trận) | Dropdown gợi ý nhìn thấy được phía trên bàn phím |
| 6.3 | Cỡ chữ/nút sau khi giảm ~22% | Đọc được thoải mái, không vỡ layout, không tràn chữ |
| 6.4 | Tour hướng dẫn: user mới (lần đầu đăng nhập) | Tự chạy tour navbar rồi tour từng trang trong phiên đầu |
| 6.5 | Tour: user cũ (LoginCount ≥ 2), kể cả trình duyệt mới | KHÔNG tự chạy; chỉ chạy khi bấm (?) hoặc "Xem lại hướng dẫn" |
| 6.6 | Dropdown avatar: mọi mục căn lề trái đều nhau | "Xem lại hướng dẫn" thẳng hàng với các mục khác |
| 6.7 | Menu Matchmaking mobile: mở/đóng từng accordion | Mũi tên xoay đúng, thu lại được |
| 6.8 | Thanh loading trên cùng | Hiện khi chuyển trang/submit form; KHÔNG hiện khi gửi chat/mở modal |

## 7. Admin

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 7.1 | User thường gõ thẳng URL /Admin/* | Bị chặn (403/redirect) |
| 7.2 | Users: khóa/mở, gán vai trò, toggle huy hiệu xác thực, ±xu | Đúng, có ghi lịch sử ví khi ±xu |
| 7.3 | Cột "Email đã/chưa xác thực" | Khớp EmailConfirmed thật trong DB |
| 7.4 | Promotions: tạo campaign từng loại + phát thưởng Manual | Đúng đối tượng, không phát trùng |
| 7.5 | Dashboard: số liệu doanh thu 7 ngày | Khớp DB |

## 8. Bảo mật (chạy /security-check sau mỗi đợt sửa)

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 8.1 | IDOR: đổi id trên URL (match, booking, user profile, wallet API) | Không xem/sửa được dữ liệu người khác |
| 8.2 | XSS: nhập `<script>alert(1)</script>` vào tên, mô tả trận, chat | Hiển thị dạng text, không thực thi |
| 8.3 | CSRF: form POST thiếu antiforgery token | Bị từ chối |
| 8.4 | SQL injection ở ô tìm kiếm | An toàn (EF parameterized) |
| 8.5 | Webhook SePay giả mạo (sai key/signature) | Bị từ chối |
| 8.6 | Secrets | appsettings.json không có trong git; log không in credentials |
| 8.7 | OTP brute-force: thử sai mã liên tục | Cooldown 60s gửi lại; cân nhắc thêm giới hạn số lần thử nếu chưa có |

## 9. Hiệu năng

| # | Ca test | Kỳ vọng |
|---|---------|---------|
| 9.1 | Trang Matchmaking với 200 trận | Tải < 3s trên 4G; nếu chậm hơn phải thấy thanh loading |
| 9.2 | Trang có nhiều ảnh (Courts, Community) | Ảnh lazy-load, không chặn render |
| 9.3 | SignalR reconnect khi rớt mạng | Tự nối lại, không mất tin nhắn hiển thị |
| 9.4 | Query chậm | Bật logging EF khi nghi ngờ, thêm index nếu thấy scan lớn (xem C1 trong CLAUDE.md) |

---

## Ghi chú kỹ thuật cho người test

- **Bàn phím ảo**: chỉ test được trên máy thật hoặc `chrome://inspect` (USB debug với Chrome Android).
- **Reset trạng thái tour**: xóa localStorage key `spTourDone` và `spTour:*`.
- **Reset popup hồ sơ**: xóa sessionStorage `spProfileSkipped`.
- **Giả lập user mới**: đăng ký email mới (dùng gmail + hậu tố `+test1@`).
- **iOS lưu ý**: Safari tự zoom khi focus input có font-size < 16px — hiện tượng có từ trước, nếu khó chịu cần cân nhắc nâng font-size riêng cho input.
