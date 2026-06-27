# /migrate — Thêm EF Core Migration & Cập nhật DB

Tạo migration mới và apply vào database SportHubDB.

## Cách dùng
```
/migrate <TenMigration>
```
Ví dụ: `/migrate AddMatchCancelFields`

## Các bước thực hiện

1. **Kiểm tra tên migration** từ $ARGUMENTS:
   - Nếu không có tên → yêu cầu người dùng cung cấp tên
   - Tên phải là PascalCase, không có khoảng trắng
   - Gợi ý tên theo nội dung thay đổi nếu người dùng chưa rõ

2. **Tạo migration:**
```powershell
dotnet ef migrations add $ARGUMENTS --project "d:\LAP TRINH\SUMMER 2026\SportHub"
```

3. **Đọc file migration vừa tạo** trong `Migrations/` để verify SQL sẽ chạy là đúng ý định. Báo tóm tắt: "Sẽ thêm/sửa/xóa gì trong DB?"

4. **Hỏi xác nhận** trước khi update DB: "Confirm chạy `database update`?"

5. **Update database:**
```powershell
dotnet ef database update --project "d:\LAP TRINH\SUMMER 2026\SportHub"
```

6. **Verify** bằng query:
```powershell
sqlcmd -S "." -d "SportHubDB" -E -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC" -W -C
```

7. Báo kết quả: migration name, thời gian, bảng được thay đổi.

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Fix |
|-----|-------------|-----|
| "No migrations configuration file found" | Chưa có DbContext | Kiểm tra `ApplicationDbContext.cs` |
| "There is already an object named 'X'" | Migration conflict | Xem xét rollback hoặc squash |
| "Cannot drop column referenced by FK" | Foreign key constraint | Thêm bước drop FK trước |
| Build failed trước khi migrate | Lỗi compile | Chạy `/build` trước |

## Rollback

Nếu cần rollback về migration trước:
```powershell
dotnet ef database update <TenMigrationTruoc> --project "d:\LAP TRINH\SUMMER 2026\SportHub"
dotnet ef migrations remove --project "d:\LAP TRINH\SUMMER 2026\SportHub"
```

## Migrations cần làm (theo plan)

- `AddMatchCancelAndLockFields` — B1: thêm `IsLockedByHost`, `CancelReason`, `CancelledAt` vào Matches
- `AddMatchDisputesTable` — B3: tạo bảng `MatchDisputes`
- `AddPromoRedemptionUniqueIndex` — A3: unique index `(UserId, PromoCodeID)` trong PromotionRedemptions
- `AddFullTextSearchIndexes` — C1: FTS catalog + indexes (có thể cần raw SQL migration)
- `AddUserNotifyByEmail` — B2: column `NotifyByEmail BIT DEFAULT 1` vào Users
