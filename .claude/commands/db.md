# /db — Query Database SportHubDB

Chạy SQL query nhanh trên database `SportHubDB` để kiểm tra dữ liệu.

## Cách dùng
```
/db <câu SQL hoặc tên query có sẵn>
```
Ví dụ:
- `/db SELECT TOP 5 * FROM Users ORDER BY UserID DESC`
- `/db users` — xem 5 user mới nhất
- `/db schema Matches` — xem cấu trúc bảng Matches
- `/db promos` — xem trạng thái promo codes

## Query shortcuts có sẵn

Nếu $ARGUMENTS là một trong các từ khoá sau, dùng query tương ứng:

**`users`**
```sql
SELECT TOP 10 UserID, FullName, Email, Role, IsBanned, WalletBalance, CreatedAt
FROM Users ORDER BY UserID DESC
```

**`promos`**
```sql
SELECT pc.PromoCodeID, pc.Code, pc.UseCount, pc.MaxUses, pc.IsActive, pc.ExpiresAt,
       c.Name as Campaign, c.TriggerType, c.Amount
FROM PromoCodes pc JOIN PromotionCampaigns c ON c.CampaignID = pc.CampaignID
ORDER BY pc.PromoCodeID DESC
```

**`matches`**
```sql
SELECT TOP 10 MatchID, Title, Status, IsLockedByHost, MatchDate, CreatedByUserID, CancelledAt
FROM Matches ORDER BY MatchID DESC
```

**`wallet`**
```sql
SELECT TOP 10 wt.WalletTransactionID, u.FullName, wt.Amount, wt.Type, wt.Description, wt.CreatedAt
FROM WalletTransactions wt JOIN Users u ON u.UserID = wt.UserID
ORDER BY wt.WalletTransactionID DESC
```

**`topups`**
```sql
SELECT TOP 10 wtr.TopUpRequestID, u.FullName, wtr.Amount, wtr.Status, wtr.TransactionRef, wtr.CreatedAt, wtr.ExpiresAt
FROM WalletTopUpRequests wtr JOIN Users u ON u.UserID = wtr.UserID
ORDER BY wtr.TopUpRequestID DESC
```

**`redemptions`**
```sql
SELECT TOP 10 pr.RedemptionID, u.FullName, c.Name as Campaign, pc.Code, pr.AmountCredited, pr.RedeemedAt
FROM PromotionRedemptions pr
JOIN Users u ON u.UserID = pr.UserID
JOIN PromotionCampaigns c ON c.CampaignID = pr.CampaignID
LEFT JOIN PromoCodes pc ON pc.PromoCodeID = pr.PromoCodeID
ORDER BY pr.RedemptionID DESC
```

**`disputes`**
```sql
SELECT d.Id, u.FullName as Reporter, d.DisputeType, d.Status, d.Resolution, d.CreatedAt
FROM MatchDisputes d JOIN Users u ON u.UserID = d.ReporterId
ORDER BY d.Id DESC
```

**`migrations`**
```sql
SELECT TOP 10 MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId DESC
```

**`schema <TableName>`**
```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '<TableName>'
ORDER BY ORDINAL_POSITION
```

## Thực hiện

1. Xác định query: shortcut hay SQL trực tiếp từ $ARGUMENTS
2. Chạy:
```powershell
sqlcmd -S "." -d "SportHubDB" -E -Q "<SQL>" -W -C
```
3. Format kết quả thành bảng dễ đọc
4. Nếu không có kết quả, thông báo "Không có dữ liệu" thay vì để trống
5. Với schema query, thêm ghi chú về foreign keys nếu nhìn thấy

## Lưu ý bảo mật
- Chỉ chạy SELECT và kiểm tra schema — không chạy UPDATE/DELETE/DROP trực tiếp
- Nếu người dùng yêu cầu UPDATE/DELETE, hiển thị câu lệnh và hỏi xác nhận trước
