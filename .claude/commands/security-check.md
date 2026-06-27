# /security-check — Kiểm tra bảo mật code vừa thay đổi

Scan các file vừa chỉnh sửa để phát hiện lỗ hổng bảo mật phổ biến trong SportHub.

## Cách dùng
```
/security-check                    # Scan các file đang thay đổi (git diff)
/security-check <đường dẫn file>   # Scan file cụ thể
```

## Các bước thực hiện

### Bước 1 — Xác định file cần scan
Nếu không có $ARGUMENTS, lấy danh sách file đang thay đổi:
```powershell
git diff --name-only HEAD
git diff --cached --name-only
```
Chỉ scan file `.cs` và `.cshtml`.

### Bước 2 — Đọc từng file và kiểm tra theo checklist

#### 🔴 CRITICAL — Race condition trong financial operations
Tìm pattern nguy hiểm trong `Services/Implementations/`:
```
// Nguy hiểm — read-modify-write không atomic:
entity.Counter++;
await _context.SaveChangesAsync();

// An toàn — atomic:
await _context.Table.Where(...).ExecuteUpdateAsync(...)
```
**File cần chú ý đặc biệt:** `PromotionService.cs`, `WalletService.cs`, `MatchPaymentService.cs`

#### 🔴 CRITICAL — Thiếu authorization
Kiểm tra mọi controller action và page handler:
- `[ApiController]` không có `[Authorize]` → báo ngay
- Admin pages không có `[Authorize(Roles = "Admin")]` → báo ngay
- Endpoint tài chính không kiểm tra userId khớp với resource owner

#### 🟠 HIGH — Missing antiforgery token
Mọi `<form method="post">` trong `.cshtml` phải có:
```html
@Html.AntiForgeryToken()
<!-- hoặc -->
<input name="__RequestVerificationToken" ...>
```
Mọi AJAX POST phải gửi `RequestVerificationToken` header.

#### 🟠 HIGH — SQL Injection
Tìm raw SQL không dùng parameterized query:
```csharp
// Nguy hiểm:
FromSqlRaw($"SELECT * FROM Users WHERE Name = '{userInput}'")

// An toàn:
FromSqlRaw("SELECT * FROM Users WHERE Name = {0}", userInput)
// hoặc dùng LINQ
```

#### 🟡 MEDIUM — Thiếu rate limiting
Các endpoint cần rate limiting mà chưa có `[EnableRateLimiting(...)]`:
- `POST /api/wallet/redeem-code`
- `POST /api/wallet/save-code`
- `POST /Auth/Login`
- Bất kỳ endpoint nào nhận tiền hoặc thực hiện hành động có giá trị

#### 🟡 MEDIUM — Lộ thông tin nhạy cảm
Tìm các pattern:
- `Console.WriteLine` hoặc `_logger.LogInformation` chứa `password`, `token`, `secret`, `key`, `balance`
- Exception message trả thẳng về client trong API response
- Stack trace trong response JSON

#### 🟡 MEDIUM — Business logic bypass
Kiểm tra các điều kiện quan trọng:
- Wallet operation: có kiểm tra `userId` từ claim, không từ request body?
- Promotion redeem: có check user chưa dùng mã chưa?
- Match cancel: có check `CreatedByUserID == currentUserId` không?

#### 🟢 LOW — Thông tin thừa trong response
API không nên trả về toàn bộ entity — chỉ trả về field cần thiết.

### Bước 3 — Báo cáo

Format kết quả:
```
🔴 CRITICAL: [File:Line] Mô tả vấn đề
   → Fix: Hướng dẫn fix cụ thể

🟠 HIGH: [File:Line] Mô tả vấn đề
   → Fix: ...

✅ Không phát hiện vấn đề trong [danh sách file đã scan]
```

### Bước 4 — Tự động fix nếu có thể
Nếu phát hiện lỗi CRITICAL và fix đơn giản (ví dụ: thêm `[Authorize]`, thêm antiforgery):
- Hỏi: "Muốn tôi fix ngay không?"
- Nếu đồng ý → thực hiện fix và chạy lại `/build`

## Lưu ý cho plan hiện tại
Sau mỗi lần implement item trong plan (A1-C5), luôn chạy `/security-check` trên file vừa sửa trước khi commit.
Đặc biệt quan trọng cho: **A3** (promo atomic), **A4** (campaign distribute), **B1** (cancel match + refund).
