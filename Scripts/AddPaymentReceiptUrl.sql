-- Migration: Thêm cột ReceiptUrl (Lưu đường dẫn ảnh biên lai chuyển khoản) vào bảng Payments
-- Ngày tạo: 2026-06-01

ALTER TABLE [Payments] 
ADD [ReceiptUrl] NVARCHAR(MAX) NULL;
GO
