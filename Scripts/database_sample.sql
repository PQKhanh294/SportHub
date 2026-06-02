USE SportHubDB;
GO

-- ============================================================
-- 1. XÓA TOÀN BỘ DỮ LIỆU (THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI)
-- ============================================================

DELETE FROM dbo.Notifications;
DELETE FROM dbo.UserSportProfiles;
DELETE FROM dbo.Reviews;
DELETE FROM dbo.MatchParticipants;
DELETE FROM dbo.Matches;
DELETE FROM dbo.Payments;
DELETE FROM dbo.BookingSlots;
DELETE FROM dbo.Bookings;
DELETE FROM dbo.PricingRules;
DELETE FROM dbo.CourtImages;
DELETE FROM dbo.Courts;
DELETE FROM dbo.CourtVenues;
DELETE FROM dbo.CourtOwners;
DELETE FROM dbo.ChatMessages;
DELETE FROM dbo.Friendships;
DELETE FROM dbo.UserRoles;
DELETE FROM dbo.Roles;
DELETE FROM dbo.Sports;
DELETE FROM dbo.TimeSlots;
DELETE FROM dbo.Users;
GO

-- ============================================================
-- 2. RESET LẠI IDENTITY (SỐ THỨ TỰ TỰ ĐỘNG) VỀ 0
-- ============================================================

DBCC CHECKIDENT ('dbo.Notifications', RESEED, 0);
DBCC CHECKIDENT ('dbo.UserSportProfiles', RESEED, 0);
DBCC CHECKIDENT ('dbo.Reviews', RESEED, 0);
DBCC CHECKIDENT ('dbo.MatchParticipants', RESEED, 0);
DBCC CHECKIDENT ('dbo.Matches', RESEED, 0);
DBCC CHECKIDENT ('dbo.Payments', RESEED, 0);
DBCC CHECKIDENT ('dbo.BookingSlots', RESEED, 0);
DBCC CHECKIDENT ('dbo.Bookings', RESEED, 0);
DBCC CHECKIDENT ('dbo.PricingRules', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtImages', RESEED, 0);
DBCC CHECKIDENT ('dbo.Courts', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtVenues', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtOwners', RESEED, 0);
DBCC CHECKIDENT ('dbo.ChatMessages', RESEED, 0);
DBCC CHECKIDENT ('dbo.Friendships', RESEED, 0);
DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);
DBCC CHECKIDENT ('dbo.Sports', RESEED, 0);
DBCC CHECKIDENT ('dbo.TimeSlots', RESEED, 0);
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
GO


-- ============================================================
-- ROLES
-- ============================================================

INSERT INTO dbo.Roles (RoleName, Description)
VALUES 
('Admin', N'Quản trị viên hệ thống'),
('CourtOwner', N'Chủ sân, đối tác'),
('Player', N'Người chơi thể thao');
GO

-- ============================================================
-- SPORTS
-- ============================================================

INSERT INTO dbo.Sports (SportName, Description)
VALUES
(N'Pickleball', N'Môn thể thao kết hợp giữa tennis, cầu lông và bóng bàn.'),
(N'Tennis', N'Môn quần vợt truyền thống.'),
(N'Cầu lông', N'Môn thể thao phổ biến với vợt và quả cầu lông.');
GO

-- ============================================================
-- TIME SLOTS (06:00 - 22:00, mỗi slot 1 tiếng)
-- ============================================================

INSERT INTO dbo.TimeSlots (StartTime, EndTime, SlotLabel)
VALUES
('06:00', '07:00', 'Slot 1'),
('07:00', '08:00', 'Slot 2'),
('08:00', '09:00', 'Slot 3'),
('09:00', '10:00', 'Slot 4'),
('10:00', '11:00', 'Slot 5'),
('11:00', '12:00', 'Slot 6'),
('12:00', '13:00', 'Slot 7'),
('13:00', '14:00', 'Slot 8'),
('14:00', '15:00', 'Slot 9'),
('15:00', '16:00', 'Slot 10'),
('16:00', '17:00', 'Slot 11'),
('17:00', '18:00', 'Slot 12'),
('18:00', '19:00', 'Slot 13'),
('19:00', '20:00', 'Slot 14'),
('20:00', '21:00', 'Slot 15'),
('21:00', '22:00', 'Slot 16');
GO

-- ============================================================
-- USERS (1 Admin, 2 Owners, 7 Players)
-- ============================================================

INSERT INTO dbo.Users
(Email, PasswordHash, FullName, PhoneNumber, DateOfBirth, Gender,
 SkillLevel, FavoriteSport, DefaultAddress, DefaultLatitude, DefaultLongitude, 
 IsActive, IsVerified, CreatedAt, UpdatedAt)
VALUES
('admin@sporthub.vn','hash123',N'Nguyễn Văn Admin','0901000001','1995-01-01','M','Advanced',N'Tennis',N'Hải Châu',16.067,108.220, 1, 1, GETUTCDATE(), GETUTCDATE()),
('owner1@sporthub.vn','hash123',N'Trần Minh Owner','0901000002','1990-05-10','M','Advanced',N'Pickleball',N'Sơn Trà',16.080,108.240, 1, 1, GETUTCDATE(), GETUTCDATE()),
('owner2@sporthub.vn','hash123',N'Lê Thị Lan','0901000003','1992-08-12','F','Intermediate',N'Cầu lông',N'Ngũ Hành Sơn',16.040,108.250, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player1@sporthub.vn','hash123',N'Phạm Quốc Huy','0901000004','2000-03-15','M','Beginner',N'Cầu lông',N'Hải Châu',16.060,108.220, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player2@sporthub.vn','hash123',N'Nguyễn Thị Mai','0901000005','2001-06-20','F','Intermediate',N'Tennis',N'Cẩm Lệ',16.030,108.200, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player3@sporthub.vn','hash123',N'Đặng Hoàng Nam','0901000006','1999-09-09','M','Advanced',N'Pickleball',N'Liên Chiểu',16.090,108.150, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player4@sporthub.vn','hash123',N'Võ Khánh Vy','0901000007','2002-11-11','F','Intermediate',N'Cầu lông',N'Sơn Trà',16.081,108.241, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player5@sporthub.vn','hash123',N'Bùi Gia Hân','0901000008','1998-04-01','F','Advanced',N'Tennis',N'Ngũ Hành Sơn',16.041,108.261, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player6@sporthub.vn','hash123',N'Huỳnh Anh Tú','0901000009','1997-07-07','M','Beginner',N'Pickleball',N'Thanh Khê',16.070,108.190, 1, 1, GETUTCDATE(), GETUTCDATE()),
('player7@sporthub.vn','hash123',N'Phan Nhật Long','0901000010','2003-02-02','M','Intermediate',N'Cầu lông',N'Hòa Xuân',16.021,108.211, 1, 1, GETUTCDATE(), GETUTCDATE());
GO

-- ============================================================
-- USER ROLES
-- ============================================================

INSERT INTO dbo.UserRoles(UserID, RoleID)
VALUES
(1,1), -- admin
(2,2), -- owner 1
(3,2), -- owner 2
(4,3), -- player 1
(5,3), -- player 2
(6,3),
(7,3),
(8,3),
(9,3),
(10,3);
GO


-- ============================================================
-- COURT OWNERS
-- ============================================================

INSERT INTO dbo.CourtOwners
(UserID, BusinessName, TaxCode, BankAccount, BankName, IsApproved, CreatedAt)
VALUES
(2, N'Hệ thống sân Pickleball Đà Nẵng', '0401234567', '123456789', N'Vietcombank', 1, GETUTCDATE()),
(3, N'Cụm sân Tennis & Cầu lông ABC', '0409876543', '987654321', N'Techcombank', 1, GETUTCDATE());
GO

-- ============================================================
-- COURT VENUES
-- ============================================================

INSERT INTO dbo.CourtVenues
(OwnerID, VenueName, Address, Ward, District, City,
 Latitude, Longitude, PhoneContact, OpenTime, CloseTime,
 AmenityParking, AmenityShower, AmenityLocker, AmenityFood, IsActive, CreatedAt)
VALUES
(1, N'Cơ sở Hải Châu', N'123 Nguyễn Văn Linh', N'Nam Dương', N'Hải Châu', N'Đà Nẵng', 16.060, 108.220, '0901111111', '06:00', '22:00', 1, 1, 1, 1, 1, GETUTCDATE()),
(1, N'Cơ sở Sơn Trà', N'456 Phạm Văn Đồng', N'An Hải Bắc', N'Sơn Trà', N'Đà Nẵng', 16.075, 108.240, '0902222222', '05:00', '23:00', 1, 1, 1, 0, 1, GETUTCDATE()),
(2, N'ABC Ngũ Hành Sơn', N'789 Lê Văn Hiến', N'Khuê Mỹ', N'Ngũ Hành Sơn', N'Đà Nẵng', 16.035, 108.250, '0903333333', '06:00', '22:00', 1, 1, 0, 1, 1, GETUTCDATE()),
(2, N'ABC Liên Chiểu', N'101 Tôn Đức Thắng', N'Hòa Minh', N'Liên Chiểu', N'Đà Nẵng', 16.085, 108.160, '0904444444', '05:30', '22:30', 1, 0, 1, 0, 1, GETUTCDATE());
GO

-- ============================================================
-- COURTS (10 sân mẫu)
-- ============================================================

INSERT INTO dbo.Courts
(VenueID, SportID, CourtName, CourtType, SurfaceType, MaxPlayers, Description, IsActive, CreatedAt)
VALUES
(1, 1, N'Sân Pickleball VIP 1', 'Indoor', 'Hard', 4, N'VIP sân trung tâm', 1, GETUTCDATE()),
(1, 1, N'Sân Pickleball 2', 'Indoor', 'Hard', 4, N'Sân chuẩn quốc tế', 1, GETUTCDATE()),
(2, 1, N'Sân Pickleball 3', 'Outdoor', 'Hard', 4, N'Sân gỗ chống trượt', 1, GETUTCDATE()),
(2, 1, N'Sân Pickleball 4', 'Outdoor', 'Hard', 4, N'Mái che mát mẻ', 1, GETUTCDATE()),
(3, 2, N'Sân Tennis 1', 'Outdoor', 'Clay', 4, N'Dành cho đánh đơn', 1, GETUTCDATE()),
(3, 2, N'Sân Tennis 2', 'Outdoor', 'Grass', 4, N'Không gian thoáng đãng', 1, GETUTCDATE()),
(3, 3, N'Sân Cầu Lông 1', 'Indoor', 'Wood', 4, N'Tiêu chuẩn mới', 1, GETUTCDATE()),
(4, 3, N'Sân Cầu Lông 2', 'Indoor', 'Wood', 4, N'Phục vụ giải đấu', 1, GETUTCDATE()),
(4, 2, N'Sân Tennis 3', 'Indoor', 'Hard', 4, N'Góc sân yên tĩnh', 1, GETUTCDATE()),
(4, 3, N'Sân Cầu Lông 3', 'Outdoor', 'Concrete', 4, N'Tiện ích đầy đủ', 1, GETUTCDATE());
GO

-- ============================================================
-- COURT IMAGES
-- ============================================================

INSERT INTO dbo.CourtImages(CourtID, ImageUrl, Caption, SortOrder, IsMain)
VALUES
(1,'https://img.com/1.jpg',N'Sân 1',1,1),
(2,'https://img.com/2.jpg',N'Sân 2',1,1),
(3,'https://img.com/3.jpg',N'Sân 3',1,1),
(4,'https://img.com/4.jpg',N'Sân 4',1,1),
(5,'https://img.com/5.jpg',N'Sân 5',1,1),
(6,'https://img.com/6.jpg',N'Sân 6',1,1),
(7,'https://img.com/7.jpg',N'Sân 7',1,1),
(8,'https://img.com/8.jpg',N'Sân 8',1,1),
(9,'https://img.com/9.jpg',N'Sân 9',1,1),
(10,'https://img.com/10.jpg',N'Sân 10',1,1);
GO

-- ============================================================
-- PRICING RULES
-- ============================================================

INSERT INTO dbo.PricingRules(CourtID, SlotID, DayType, UnitPrice, Currency, ValidFrom)
VALUES
(1,1,'Weekday',150000, 'VND', GETUTCDATE()),
(2,2,'Weekday',200000, 'VND', GETUTCDATE()),
(3,3,'Weekday',100000, 'VND', GETUTCDATE()),
(4,4,'Weekend',180000, 'VND', GETUTCDATE()),
(5,5,'Weekend',250000, 'VND', GETUTCDATE()),
(6,6,'Weekday',120000, 'VND', GETUTCDATE()),
(7,7,'Holiday',220000, 'VND', GETUTCDATE()),
(8,8,'Weekday',300000, 'VND', GETUTCDATE()),
(9,9,'Weekend',130000, 'VND', GETUTCDATE()),
(10,10,'Holiday',210000, 'VND', GETUTCDATE());
GO

-- ============================================================
-- BOOKINGS
-- ============================================================

INSERT INTO dbo.Bookings
(UserID, CourtID, BookingDate, TotalAmount,
 DiscountAmount, FinalAmount, Status, BookedAt, UpdatedAt)
VALUES
(4,1,'2026-06-01',150000,0,150000,'Confirmed', GETUTCDATE(), GETUTCDATE()),
(5,2,'2026-06-01',200000,10000,190000,'Confirmed', GETUTCDATE(), GETUTCDATE()),
(6,3,'2026-06-02',100000,0,100000,'Pending', GETUTCDATE(), GETUTCDATE()),
(7,4,'2026-06-02',180000,0,180000,'Completed', GETUTCDATE(), GETUTCDATE()),
(8,5,'2026-06-03',250000,20000,230000,'Confirmed', GETUTCDATE(), GETUTCDATE()),
(9,6,'2026-06-03',120000,0,120000,'Cancelled', GETUTCDATE(), GETUTCDATE()),
(10,7,'2026-06-04',220000,0,220000,'Confirmed', GETUTCDATE(), GETUTCDATE()),
(4,8,'2026-06-04',300000,50000,250000,'Completed', GETUTCDATE(), GETUTCDATE()),
(5,9,'2026-06-05',130000,0,130000,'NoShow', GETUTCDATE(), GETUTCDATE()),
(6,10,'2026-06-05',210000,10000,200000,'Confirmed', GETUTCDATE(), GETUTCDATE());
GO

-- ============================================================
-- BOOKING SLOTS
-- ============================================================

INSERT INTO dbo.BookingSlots(BookingID, SlotID, UnitPrice)
VALUES
(1,1,150000),
(2,2,200000),
(3,3,100000),
(4,4,180000),
(5,5,250000),
(6,6,120000),
(7,7,220000),
(8,8,300000),
(9,9,130000),
(10,10,210000);
GO

-- ============================================================
-- PAYMENTS
-- ============================================================

INSERT INTO dbo.Payments
(BookingID, Amount, PaymentMethod,
 TransactionRef, Status, PaymentType, PaidAt, CreatedAt)
VALUES
(1,150000,'VNPay','TXN001','Success','Payment',GETDATE(), GETUTCDATE()),
(2,190000,'MoMo','TXN002','Success','Payment',GETDATE(), GETUTCDATE()),
(3,100000,'Cash','TXN003','Pending','Payment',NULL, GETUTCDATE()),
(4,180000,'VNPay','TXN004','Success','Payment',GETDATE(), GETUTCDATE()),
(5,230000,'BankTransfer','TXN005','Success','Payment',GETDATE(), GETUTCDATE()),
(6,120000,'Cash','TXN006','Refunded','Refund',GETDATE(), GETUTCDATE()),
(7,220000,'ZaloPay','TXN007','Success','Payment',GETDATE(), GETUTCDATE()),
(8,250000,'VNPay','TXN008','Success','Payment',GETDATE(), GETUTCDATE()),
(9,130000,'MoMo','TXN009','Failed','Payment',NULL, GETUTCDATE()),
(10,200000,'BankTransfer','TXN010','Success','Payment',GETDATE(), GETUTCDATE());
GO

-- ============================================================
-- MATCHES (10)
-- ============================================================

INSERT INTO dbo.Matches
(CreatedByUserID, CourtID, BookingID, SportID, MatchDate, StartTime, EndTime,
 MatchType, SkillRequired, MaxParticipants, Title, Description, Status, RequiresApproval, CreatedAt)
VALUES
(2,1,1,1,'2026-06-10','07:00','08:00','Doubles','Beginner',4,N'Pickleball sáng sớm',N'Giao lưu thân thiện', 'Open', 1, GETUTCDATE()),
(3,2,2,2,'2026-06-11','08:00','09:00','Singles','Intermediate',2,N'Tennis trung cấp',N'Tập luyện nâng cao', 'Open', 1, GETUTCDATE()),
(4,3,3,3,'2026-06-12','09:00','10:00','Doubles','Advanced',4,N'Cầu lông nâng cao',N'Đánh đôi tốc độ', 'Open', 1, GETUTCDATE()),
(5,4,4,1,'2026-06-13','10:00','11:00','Mixed','Any',4,N'Pickleball vui vẻ',N'Không yêu cầu trình độ', 'Open', 1, GETUTCDATE()),
(6,5,5,2,'2026-06-14','11:00','12:00','Singles','Professional',2,N'Tennis pro',N'Thách đấu chuyên nghiệp', 'Open', 1, GETUTCDATE()),
(7,6,6,3,'2026-06-15','12:00','13:00','Doubles','Intermediate',4,N'Cầu lông trưa',N'Giao lưu', 'Open', 1, GETUTCDATE()),
(8,7,7,1,'2026-06-16','13:00','14:00','Doubles','Beginner',4,N'Pickleball học hỏi',N'Người mới', 'Open', 1, GETUTCDATE()),
(9,8,8,2,'2026-06-17','14:00','15:00','Singles','Advanced',2,N'Tennis chiều',N'Tập luyện', 'Open', 1, GETUTCDATE()),
(10,9,9,3,'2026-06-18','15:00','16:00','Doubles','Any',4,N'Cầu lông tự do',N'Thân thiện', 'Open', 1, GETUTCDATE()),
(4,10,10,1,'2026-06-19','16:00','17:00','Mixed','Intermediate',4,N'Pickleball chiều',N'Giao lưu', 'Open', 1, GETUTCDATE());
GO

-- ============================================================
-- MATCH PARTICIPANTS (10)
-- ============================================================

INSERT INTO dbo.MatchParticipants (MatchID, UserID, TeamSide, JoinStatus, JoinedAt)
VALUES
(1,2,'A','Accepted', GETUTCDATE()),
(1,4,'B','Accepted', GETUTCDATE()),
(2,3,'A','Accepted', GETUTCDATE()),
(3,5,'A','Pending', GETUTCDATE()),
(4,6,'B','Accepted', GETUTCDATE()),
(5,7,'A','Accepted', GETUTCDATE()),
(6,8,'B','Pending', GETUTCDATE()),
(7,9,'A','Accepted', GETUTCDATE()),
(8,10,'B','Accepted', GETUTCDATE()),
(9,4,'A','Pending', GETUTCDATE());
GO

-- ============================================================
-- REVIEWS (10)
-- ============================================================

INSERT INTO dbo.Reviews (CourtID, UserID, BookingID, Rating, Comment, ReviewedAt)
VALUES
(1,4,1,5,N'Sân đẹp, sạch.', GETUTCDATE()),
(2,5,2,4,N'Sân ổn, giá hợp lý.', GETUTCDATE()),
(3,6,3,5,N'Ánh sáng tốt.', GETUTCDATE()),
(4,7,4,3,N'Hơi đông.', GETUTCDATE()),
(5,8,5,5,N'Sân mới, chất lượng.', GETUTCDATE()),
(6,9,6,4,N'Nhân viên thân thiện.', GETUTCDATE()),
(7,10,7,4,N'Vị trí thuận tiện.', GETUTCDATE()),
(8,4,8,5,N'Sân cỏ đẹp.', GETUTCDATE()),
(9,5,9,3,N'Trải nghiệm ổn.', GETUTCDATE()),
(10,6,10,5,N'Rất hài lòng.', GETUTCDATE());
GO

-- ============================================================
-- USER SPORT PROFILES (10)
-- ============================================================

INSERT INTO dbo.UserSportProfiles
(UserID, SportID, CourtPosition, PlayStyle, StrokeStrength, SelfRatedLevel, ExperienceYears, SkillLevel, Notes, UpdatedAt)
VALUES
(4,3,'BackCourt','Aggressive','Smash',4,3,'Intermediate',N'Ưa tấn công', GETUTCDATE()),
(5,2,'AllRound','Balanced','Drive',3,2,'Intermediate',N'Đánh đều tay', GETUTCDATE()),
(6,1,'FrontCourt','Aggressive','Smash',5,4,'Advanced',N'Tốc độ cao', GETUTCDATE()),
(7,3,'AllRound','Defensive','Drop',2,1,'Beginner',N'Mới chơi', GETUTCDATE()),
(8,2,'BackCourt','Balanced','Drive',4,3,'Advanced',N'Ổn định', GETUTCDATE()),
(9,1,'FrontCourt','Aggressive','Smash',3,2,'Intermediate',N'Ưa phản xạ nhanh', GETUTCDATE()),
(10,3,'AllRound','Balanced','AllRound',3,2,'Intermediate',N'Tự do', GETUTCDATE()),
(4,1,'BackCourt','Aggressive','Smash',4,3,'Advanced',N'Pickleball tốt', GETUTCDATE()),
(5,3,'FrontCourt','Defensive','Drop',2,1,'Beginner',N'Ưa phòng thủ', GETUTCDATE()),
(6,2,'AllRound','Balanced','Drive',5,5,'Professional',N'Thực chiến', GETUTCDATE());
GO

-- ============================================================
-- NOTIFICATIONS (10)
-- ============================================================

INSERT INTO dbo.Notifications (UserID, Type, Title, Message, LinkUrl, IsRead, CreatedAt)
VALUES
(4,'MatchJoin',N'Yêu cầu tham gia',N'Bạn có 1 yêu cầu tham gia trận đấu mới.',N'/Matchmaking/Index',0, GETUTCDATE()),
(5,'MatchApprove',N'Được duyệt',N'Yêu cầu tham gia đã được duyệt.',N'/Matchmaking/Index',0, GETUTCDATE()),
(6,'MatchReject',N'Bị từ chối',N'Yêu cầu tham gia đã bị từ chối.',N'/Matchmaking/Index',1, GETUTCDATE()),
(7,'BookingConfirmed',N'Đặt sân thành công',N'Đơn đặt sân đã xác nhận.',N'/Bookings/MyBookings',0, GETUTCDATE()),
(8,'BookingCancelled',N'Hủy đặt sân',N'Đơn đặt sân đã bị hủy.',N'/Bookings/MyBookings',0, GETUTCDATE()),
(9,'System',N'Thông báo hệ thống',N'Cập nhật điều khoản sử dụng.',N'/Settings',1, GETUTCDATE()),
(10,'MatchJoin',N'Yêu cầu mới',N'Có người muốn tham gia trận đấu của bạn.',N'/Matchmaking/Index',0, GETUTCDATE()),
(4,'BookingConfirmed',N'Xác nhận thanh toán',N'Thanh toán đã thành công.',N'/Bookings/MyBookings',0, GETUTCDATE()),
(5,'System',N'Bảo trì',N'Hệ thống sẽ bảo trì tối nay.',N'/',1, GETUTCDATE()),
(6,'MatchApprove',N'Duyệt tham gia',N'Bạn đã được duyệt vào trận đấu.',N'/Matchmaking/Index',0, GETUTCDATE());
GO
