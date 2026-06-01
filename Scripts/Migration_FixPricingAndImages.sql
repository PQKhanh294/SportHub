USE SportHubDB;
GO

-- ============================================================
-- MIGRATION: Fix PricingRules + Image URLs
-- Ngày: 2026-05-30
-- Mô tả:
--   1. Xóa PricingRules cũ (quá thưa, 1 rule/sân)
--   2. Thêm PricingRules đầy đủ cho tất cả 10 sân × 10 slots × 2 DayType
--   3. Cập nhật CourtImages sang URL Unsplash thực
-- ============================================================

-- ── 1. Xóa PricingRules cũ ──────────────────────────────────
DELETE FROM dbo.PricingRules;
DBCC CHECKIDENT ('dbo.PricingRules', RESEED, 0);
GO

-- ── 2. Thêm PricingRules đầy đủ ─────────────────────────────
-- Mỗi sân có 10 slots (SlotID 1-10) × 2 DayType (Weekday/Weekend)
-- Giá Weekend thường cao hơn 20-30% so với Weekday

-- Court 1 - Sân Pickleball A1 (DaNang Arena)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(1,1,'Weekday',150000),(1,1,'Weekend',180000),
(1,2,'Weekday',150000),(1,2,'Weekend',180000),
(1,3,'Weekday',150000),(1,3,'Weekend',180000),
(1,4,'Weekday',150000),(1,4,'Weekend',180000),
(1,5,'Weekday',150000),(1,5,'Weekend',180000),
(1,6,'Weekday',150000),(1,6,'Weekend',180000),
(1,7,'Weekday',150000),(1,7,'Weekend',180000),
(1,8,'Weekday',180000),(1,8,'Weekend',220000),
(1,9,'Weekday',180000),(1,9,'Weekend',220000),
(1,10,'Weekday',150000),(1,10,'Weekend',180000);

-- Court 2 - Sân Tennis T1 (BlueSky Club)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(2,1,'Weekday',200000),(2,1,'Weekend',250000),
(2,2,'Weekday',200000),(2,2,'Weekend',250000),
(2,3,'Weekday',200000),(2,3,'Weekend',250000),
(2,4,'Weekday',200000),(2,4,'Weekend',250000),
(2,5,'Weekday',200000),(2,5,'Weekend',250000),
(2,6,'Weekday',200000),(2,6,'Weekend',250000),
(2,7,'Weekday',200000),(2,7,'Weekend',250000),
(2,8,'Weekday',220000),(2,8,'Weekend',280000),
(2,9,'Weekday',220000),(2,9,'Weekend',280000),
(2,10,'Weekday',200000),(2,10,'Weekend',250000);

-- Court 3 - Sân Cầu Lông B1 (Sunrise Court)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(3,1,'Weekday',100000),(3,1,'Weekend',130000),
(3,2,'Weekday',100000),(3,2,'Weekend',130000),
(3,3,'Weekday',100000),(3,3,'Weekend',130000),
(3,4,'Weekday',100000),(3,4,'Weekend',130000),
(3,5,'Weekday',100000),(3,5,'Weekend',130000),
(3,6,'Weekday',100000),(3,6,'Weekend',130000),
(3,7,'Weekday',100000),(3,7,'Weekend',130000),
(3,8,'Weekday',120000),(3,8,'Weekend',150000),
(3,9,'Weekday',120000),(3,9,'Weekend',150000),
(3,10,'Weekday',100000),(3,10,'Weekend',130000);

-- Court 4 - Sân Pickleball A2 (Green Arena)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(4,1,'Weekday',160000),(4,1,'Weekend',200000),
(4,2,'Weekday',160000),(4,2,'Weekend',200000),
(4,3,'Weekday',160000),(4,3,'Weekend',200000),
(4,4,'Weekday',160000),(4,4,'Weekend',200000),
(4,5,'Weekday',160000),(4,5,'Weekend',200000),
(4,6,'Weekday',160000),(4,6,'Weekend',200000),
(4,7,'Weekday',160000),(4,7,'Weekend',200000),
(4,8,'Weekday',190000),(4,8,'Weekend',230000),
(4,9,'Weekday',190000),(4,9,'Weekend',230000),
(4,10,'Weekday',160000),(4,10,'Weekend',200000);

-- Court 5 - Sân Tennis T2 (SeaSide Tennis)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(5,1,'Weekday',250000),(5,1,'Weekend',300000),
(5,2,'Weekday',250000),(5,2,'Weekend',300000),
(5,3,'Weekday',250000),(5,3,'Weekend',300000),
(5,4,'Weekday',250000),(5,4,'Weekend',300000),
(5,5,'Weekday',250000),(5,5,'Weekend',300000),
(5,6,'Weekday',250000),(5,6,'Weekend',300000),
(5,7,'Weekday',250000),(5,7,'Weekend',300000),
(5,8,'Weekday',280000),(5,8,'Weekend',350000),
(5,9,'Weekday',280000),(5,9,'Weekend',350000),
(5,10,'Weekday',250000),(5,10,'Weekend',300000);

-- Court 6 - Sân Cầu Lông B2 (Champion Club)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(6,1,'Weekday',120000),(6,1,'Weekend',150000),
(6,2,'Weekday',120000),(6,2,'Weekend',150000),
(6,3,'Weekday',120000),(6,3,'Weekend',150000),
(6,4,'Weekday',120000),(6,4,'Weekend',150000),
(6,5,'Weekday',120000),(6,5,'Weekend',150000),
(6,6,'Weekday',120000),(6,6,'Weekend',150000),
(6,7,'Weekday',120000),(6,7,'Weekend',150000),
(6,8,'Weekday',140000),(6,8,'Weekend',180000),
(6,9,'Weekday',140000),(6,9,'Weekend',180000),
(6,10,'Weekday',120000),(6,10,'Weekend',150000);

-- Court 7 - Sân Pickleball A3 (Elite Hub)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(7,1,'Weekday',170000),(7,1,'Weekend',210000),
(7,2,'Weekday',170000),(7,2,'Weekend',210000),
(7,3,'Weekday',170000),(7,3,'Weekend',210000),
(7,4,'Weekday',170000),(7,4,'Weekend',210000),
(7,5,'Weekday',170000),(7,5,'Weekend',210000),
(7,6,'Weekday',170000),(7,6,'Weekend',210000),
(7,7,'Weekday',170000),(7,7,'Weekend',210000),
(7,8,'Weekday',200000),(7,8,'Weekend',250000),
(7,9,'Weekday',200000),(7,9,'Weekend',250000),
(7,10,'Weekday',170000),(7,10,'Weekend',210000);

-- Court 8 - Sân Tennis T3 (Dragon Court)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(8,1,'Weekday',300000),(8,1,'Weekend',380000),
(8,2,'Weekday',300000),(8,2,'Weekend',380000),
(8,3,'Weekday',300000),(8,3,'Weekend',380000),
(8,4,'Weekday',300000),(8,4,'Weekend',380000),
(8,5,'Weekday',300000),(8,5,'Weekend',380000),
(8,6,'Weekday',300000),(8,6,'Weekend',380000),
(8,7,'Weekday',300000),(8,7,'Weekend',380000),
(8,8,'Weekday',350000),(8,8,'Weekend',420000),
(8,9,'Weekday',350000),(8,9,'Weekend',420000),
(8,10,'Weekday',300000),(8,10,'Weekend',380000);

-- Court 9 - Sân Cầu Lông B3 (Ocean Sports)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(9,1,'Weekday',130000),(9,1,'Weekend',160000),
(9,2,'Weekday',130000),(9,2,'Weekend',160000),
(9,3,'Weekday',130000),(9,3,'Weekend',160000),
(9,4,'Weekday',130000),(9,4,'Weekend',160000),
(9,5,'Weekday',130000),(9,5,'Weekend',160000),
(9,6,'Weekday',130000),(9,6,'Weekend',160000),
(9,7,'Weekday',130000),(9,7,'Weekend',160000),
(9,8,'Weekday',150000),(9,8,'Weekend',190000),
(9,9,'Weekday',150000),(9,9,'Weekend',190000),
(9,10,'Weekday',130000),(9,10,'Weekend',160000);

-- Court 10 - Sân Pickleball A4 (Star Court - VIP)
INSERT INTO dbo.PricingRules (CourtID, SlotID, DayType, UnitPrice) VALUES
(10,1,'Weekday',200000),(10,1,'Weekend',250000),
(10,2,'Weekday',200000),(10,2,'Weekend',250000),
(10,3,'Weekday',200000),(10,3,'Weekend',250000),
(10,4,'Weekday',200000),(10,4,'Weekend',250000),
(10,5,'Weekday',200000),(10,5,'Weekend',250000),
(10,6,'Weekday',200000),(10,6,'Weekend',250000),
(10,7,'Weekday',200000),(10,7,'Weekend',250000),
(10,8,'Weekday',230000),(10,8,'Weekend',290000),
(10,9,'Weekday',230000),(10,9,'Weekend',290000),
(10,10,'Weekday',200000),(10,10,'Weekend',250000);
GO

-- ── 3. Cập nhật CourtImages sang URL thực (Unsplash) ────────
-- Mỗi sân cần ít nhất 5 ảnh (vì Details.cshtml hiển thị gallery 5 ảnh)
-- Xóa ảnh cũ rồi thêm mới

DELETE FROM dbo.CourtImages;
DBCC CHECKIDENT ('dbo.CourtImages', RESEED, 0);
GO

-- Court 1 - Pickleball
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(1,'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?w=1200&auto=format&fit=crop',N'Sân Pickleball A1',1,1),
(1,'https://images.unsplash.com/photo-1599474924187-334a4ae5bd3c?w=800&auto=format&fit=crop',N'Khu vực sân',2,0),
(1,'https://images.unsplash.com/photo-1588286492390-4e8e4a947de4?w=800&auto=format&fit=crop',N'Nhà thi đấu',3,0),
(1,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Tiện ích',4,0),
(1,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Bãi giữ xe',5,0);

-- Court 2 - Tennis outdoor
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(2,'https://images.unsplash.com/photo-1542144582-1ba00456b5e3?w=1200&auto=format&fit=crop',N'Sân Tennis T1',1,1),
(2,'https://images.unsplash.com/photo-1564769625905-50e93615e769?w=800&auto=format&fit=crop',N'Sân cứng ngoài trời',2,0),
(2,'https://images.unsplash.com/photo-1545809074-59472b3f5ecc?w=800&auto=format&fit=crop',N'Khu vực ghế khán giả',3,0),
(2,'https://images.unsplash.com/photo-1587280501635-68a0e82cd5ff?w=800&auto=format&fit=crop',N'Phòng thay đồ',4,0),
(2,'https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=800&auto=format&fit=crop',N'Canteen',5,0);

-- Court 3 - Cầu lông indoor
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(3,'https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=1200&auto=format&fit=crop',N'Sân Cầu Lông B1',1,1),
(3,'https://images.unsplash.com/photo-1599474924187-334a4ae5bd3c?w=800&auto=format&fit=crop',N'Sân trong nhà máy lạnh',2,0),
(3,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Hệ thống chiếu sáng',3,0),
(3,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Khu vực nghỉ ngơi',4,0),
(3,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Tiện ích đi kèm',5,0);

-- Court 4 - Pickleball A2
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(4,'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?w=1200&auto=format&fit=crop',N'Sân Pickleball A2',1,1),
(4,'https://images.unsplash.com/photo-1588286492390-4e8e4a947de4?w=800&auto=format&fit=crop',N'Sân mới khai trương',2,0),
(4,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Phòng thay đồ',3,0),
(4,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Hệ thống đèn LED',4,0),
(4,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Bãi đậu xe rộng',5,0);

-- Court 5 - Tennis T2 outdoor clay
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(5,'https://images.unsplash.com/photo-1519311965067-36d3e5f33d39?w=1200&auto=format&fit=crop',N'Sân Tennis T2',1,1),
(5,'https://images.unsplash.com/photo-1545809074-59472b3f5ecc?w=800&auto=format&fit=crop',N'Sân đất nện cao cấp',2,0),
(5,'https://images.unsplash.com/photo-1564769625905-50e93615e769?w=800&auto=format&fit=crop',N'View biển tuyệt đẹp',3,0),
(5,'https://images.unsplash.com/photo-1587280501635-68a0e82cd5ff?w=800&auto=format&fit=crop',N'Phòng VIP',4,0),
(5,'https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=800&auto=format&fit=crop',N'Nhà hàng sân',5,0);

-- Court 6 - Cầu lông B2 sàn gỗ
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(6,'https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=1200&auto=format&fit=crop',N'Sân Cầu Lông B2',1,1),
(6,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Sàn gỗ cao cấp',2,0),
(6,'https://images.unsplash.com/photo-1599474924187-334a4ae5bd3c?w=800&auto=format&fit=crop',N'Ánh sáng chuyên nghiệp',3,0),
(6,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Tủ đựng đồ',4,0),
(6,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Quầy nước',5,0);

-- Court 7 - Pickleball A3 outdoor
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(7,'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?w=1200&auto=format&fit=crop',N'Sân Pickleball A3',1,1),
(7,'https://images.unsplash.com/photo-1588286492390-4e8e4a947de4?w=800&auto=format&fit=crop',N'Sân ngoài trời thoáng mát',2,0),
(7,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Khu vực nghỉ ngơi',3,0),
(7,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Cơ sở vật chất',4,0),
(7,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Bãi đậu xe',5,0);

-- Court 8 - Tennis T3 cỏ
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(8,'https://images.unsplash.com/photo-1519311965067-36d3e5f33d39?w=1200&auto=format&fit=crop',N'Sân Tennis T3',1,1),
(8,'https://images.unsplash.com/photo-1564769625905-50e93615e769?w=800&auto=format&fit=crop',N'Sân cỏ thiên nhiên',2,0),
(8,'https://images.unsplash.com/photo-1545809074-59472b3f5ecc?w=800&auto=format&fit=crop',N'Khu vực VIP',3,0),
(8,'https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=800&auto=format&fit=crop',N'Nhà hàng và canteen',4,0),
(8,'https://images.unsplash.com/photo-1587280501635-68a0e82cd5ff?w=800&auto=format&fit=crop',N'Phòng thay đồ VIP',5,0);

-- Court 9 - Cầu lông B3
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(9,'https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=1200&auto=format&fit=crop',N'Sân Cầu Lông B3',1,1),
(9,'https://images.unsplash.com/photo-1599474924187-334a4ae5bd3c?w=800&auto=format&fit=crop',N'Sân rộng 4 lane',2,0),
(9,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Hệ thống gió mát',3,0),
(9,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Canteen và đồ uống',4,0),
(9,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Bãi xe máy miễn phí',5,0);

-- Court 10 - Pickleball A4 VIP
INSERT INTO dbo.CourtImages (CourtID, ImageUrl, Caption, SortOrder, IsMain) VALUES
(10,'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?w=1200&auto=format&fit=crop',N'Sân Pickleball A4 VIP',1,1),
(10,'https://images.unsplash.com/photo-1588286492390-4e8e4a947de4?w=800&auto=format&fit=crop',N'Sân tiêu chuẩn quốc tế',2,0),
(10,'https://images.unsplash.com/photo-1629904853893-c2c8981a1dc5?w=800&auto=format&fit=crop',N'Màn hình tỉ số điện tử',3,0),
(10,'https://images.unsplash.com/photo-1541534741688-6078c6bfb5c5?w=800&auto=format&fit=crop',N'Phòng VIP',4,0),
(10,'https://images.unsplash.com/photo-1576458088443-04a19bb13da6?w=800&auto=format&fit=crop',N'Nhà hàng cao cấp',5,0);
GO

PRINT N'✅ Migration hoàn thành: PricingRules đầy đủ (200 rules) + CourtImages URL thực (50 ảnh)';
GO
