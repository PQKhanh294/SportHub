# /build — Build SportHub & Báo lỗi

Build project SportHub và hiển thị kết quả rõ ràng.

## Các bước thực hiện

1. Chạy lệnh build:
```powershell
dotnet build "d:\LAP TRINH\SUMMER 2026\SportHub" 2>&1
```

2. Phân tích output:
   - Nếu **"Build succeeded"**: báo "✅ Build OK" + số warning (nếu có)
   - Nếu **có error**: liệt kê từng lỗi theo format:
     ```
     ❌ [TenFile.cs:LineNumber] error CSxxxx: Mô tả lỗi
     ```
   - Nhóm các lỗi cùng file lại với nhau

3. Nếu có lỗi, phân tích nguyên nhân và đề xuất fix ngắn gọn (1-2 câu mỗi lỗi).

4. Không hiển thị toàn bộ raw output — chỉ hiển thị phần có giá trị.

## Ghi chú
- Project path: `d:\LAP TRINH\SUMMER 2026\SportHub`
- Framework: .NET 8
- Nếu lỗi liên quan đến EF migrations, gợi ý chạy `/migrate`
- Nếu lỗi "package not found", gợi ý lệnh `dotnet add package`
