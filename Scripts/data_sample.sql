USE SportHubDB;
GO

-- ============================================================
-- XÓA TOÀN BỘ DỮ LIỆU (THEO THỨ TỰ FK)
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
DELETE FROM dbo.UserRoles;
DELETE FROM dbo.Users;
DELETE FROM dbo.TimeSlots;
DELETE FROM dbo.Sports;
DELETE FROM dbo.Roles;
GO

-- Reset IDENTITY
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtOwners', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtVenues', RESEED, 0);
DBCC CHECKIDENT ('dbo.Courts', RESEED, 0);
DBCC CHECKIDENT ('dbo.CourtImages', RESEED, 0);
DBCC CHECKIDENT ('dbo.PricingRules', RESEED, 0);
DBCC CHECKIDENT ('dbo.Bookings', RESEED, 0);
DBCC CHECKIDENT ('dbo.BookingSlots', RESEED, 0);
DBCC CHECKIDENT ('dbo.Payments', RESEED, 0);
DBCC CHECKIDENT ('dbo.Matches', RESEED, 0);
DBCC CHECKIDENT ('dbo.MatchParticipants', RESEED, 0);
DBCC CHECKIDENT ('dbo.Reviews', RESEED, 0);
DBCC CHECKIDENT ('dbo.UserSportProfiles', RESEED, 0);
DBCC CHECKIDENT ('dbo.Notifications', RESEED, 0);
DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);
DBCC CHECKIDENT ('dbo.Sports', RESEED, 0);
DBCC CHECKIDENT ('dbo.TimeSlots', RESEED, 0);
GO

-- ============================================================
-- ROLES (10)
-- ============================================================

INSERT INTO dbo.Roles (RoleName, Description)
VALUES
('Admin', N'Quản trị viên hệ thống'),
('CourtOwner', N'Chủ sân thể thao'),
('Player', N'Người chơi / Khách hàng'),
('Moderator', N'Kiểm duyệt nội dung'),
('Coach', N'HLV / Hướng dẫn'),
('Referee', N'Trọng tài'),
('Support', N'Hỗ trợ khách hàng'),
('Marketing', N'Tiếp thị'),
('Finance', N'Tài chính'),
('Staff', N'Nhân viên vận hành');
GO

-- ============================================================
-- SPORTS (10)
-- ============================================================

INSERT INTO dbo.Sports (SportName, Description)
VALUES
(N'Pickleball', N'Môn thể thao kết hợp tennis, bóng bàn và cầu lông'),
(N'Tennis', N'Môn quần vợt sân cứng/đất nện/thảm'),
(N'Cầu lông', N'Badminton - môn vợt cầu trong nhà'),
(N'Bóng bàn', N'Table Tennis'),
(N'Bóng rổ', N'Basketball'),
(N'Bóng đá', N'Football/Soccer'),
(N'Bóng chuyền', N'Volleyball'),
(N'Padel', N'Padel Tennis'),
(N'Squash', N'Squash Court'),
(N'Cầu mây', N'Sepak Takraw');
GO

-- ============================================================
-- TIMESLOTS (10)
-- ============================================================

INSERT INTO dbo.TimeSlots (StartTime, EndTime, SlotLabel)
VALUES
('06:00','07:00','6:00 - 7:00'),
('07:00','08:00','7:00 - 8:00'),
('08:00','09:00','8:00 - 9:00'),
('09:00','10:00','9:00 - 10:00'),
('10:00','11:00','10:00 - 11:00'),
('11:00','12:00','11:00 - 12:00'),
('12:00','13:00','12:00 - 13:00'),
('13:00','14:00','13:00 - 14:00'),
('14:00','15:00','14:00 - 15:00'),
('15:00','16:00','15:00 - 16:00');
GO

-- ============================================================
-- USERS (10)
-- ============================================================

INSERT INTO dbo.Users
(Email, PasswordHash, FullName, PhoneNumber, DateOfBirth, Gender,
 SkillLevel, FavoriteSport, DefaultAddress,
 DefaultLatitude, DefaultLongitude)
VALUES
('admin@sporthub.vn','hash123',N'Nguyễn Văn Admin','0901000001','1995-01-01','M','Advanced',N'Tennis',N'Hải Châu',16.067,108.220),
('owner1@sporthub.vn','hash123',N'Trần Minh Owner','0901000002','1990-05-10','M','Advanced',N'Pickleball',N'Sơn Trà',16.080,108.240),
('owner2@sporthub.vn','hash123',N'Lê Thị Lan','0901000003','1992-08-12','F','Intermediate',N'Cầu lông',N'Ngũ Hành Sơn',16.040,108.250),
('player1@sporthub.vn','hash123',N'Phạm Quốc Huy','0901000004','2000-03-15','M','Beginner',N'Cầu lông',N'Hải Châu',16.060,108.220),
('player2@sporthub.vn','hash123',N'Nguyễn Thị Mai','0901000005','2001-06-20','F','Intermediate',N'Tennis',N'Cẩm Lệ',16.030,108.200),
('player3@sporthub.vn','hash123',N'Đặng Hoàng Nam','0901000006','1999-09-09','M','Advanced',N'Pickleball',N'Liên Chiểu',16.090,108.150),
('player4@sporthub.vn','hash123',N'Võ Khánh Vy','0901000007','2002-11-11','F','Intermediate',N'Cầu lông',N'Sơn Trà',16.081,108.241),
('player5@sporthub.vn','hash123',N'Bùi Gia Hân','0901000008','1998-04-01','F','Advanced',N'Tennis',N'Ngũ Hành Sơn',16.041,108.261),
('player6@sporthub.vn','hash123',N'Huỳnh Anh Tú','0901000009','1997-07-07','M','Beginner',N'Pickleball',N'Thanh Khê',16.070,108.190),
('player7@sporthub.vn','hash123',N'Phan Nhật Long','0901000010','2003-02-02','M','Intermediate',N'Cầu lông',N'Hòa Xuân',16.021,108.211);
GO

-- ============================================================
-- USER ROLES
-- ============================================================

INSERT INTO dbo.UserRoles(UserID, RoleID)
VALUES
(1,1),
(2,2),
(3,2),
(4,3),
(5,3),
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
(UserID, BusinessName, TaxCode, BankAccount, BankName, IsApproved)
VALUES
(2,N'DaNang Sport Center','TX001','123456789','Vietcombank',1),
(3,N'BlueSky Courts','TX002','223456789','ACB',1);
GO

-- ============================================================
-- COURT VENUES (10)
-- ============================================================

INSERT INTO dbo.CourtVenues
(OwnerID, VenueName, Address, Ward, District, City,
 Latitude, Longitude, PhoneContact,
 AmenityParking, AmenityShower, AmenityLocker, AmenityFood)
VALUES
(1,N'DaNang Arena',N'12 Nguyễn Văn Linh',N'Hải Châu 1',N'Hải Châu',N'Đà Nẵng',16.067,108.220,'0905111111',1,1,1,1),
(2,N'BlueSky Club',N'45 Võ Văn Kiệt',N'An Hải',N'Sơn Trà',N'Đà Nẵng',16.080,108.240,'0905222222',1,1,0,1),
(1,N'Sunrise Court',N'89 Hồ Xuân Hương',N'Mỹ An',N'Ngũ Hành Sơn',N'Đà Nẵng',16.040,108.250,'0905333333',1,0,0,1),
(2,N'Green Arena',N'100 Điện Biên Phủ',N'Thanh Khê Đông',N'Thanh Khê',N'Đà Nẵng',16.070,108.190,'0905444444',1,1,1,0),
(1,N'SeaSide Tennis',N'20 Trường Sa',N'Khuê Mỹ',N'Ngũ Hành Sơn',N'Đà Nẵng',16.030,108.260,'0905555555',1,1,0,1),
(2,N'Champion Club',N'11 Lê Duẩn',N'Thạch Thang',N'Hải Châu',N'Đà Nẵng',16.075,108.223,'0905666666',1,1,1,1),
(1,N'Elite Hub',N'56 Nguyễn Tất Thành',N'Xuân Hà',N'Thanh Khê',N'Đà Nẵng',16.085,108.180,'0905777777',1,0,1,0),
(2,N'Dragon Court',N'77 2/9',N'Hòa Cường',N'Hải Châu',N'Đà Nẵng',16.050,108.230,'0905888888',1,1,1,1),
(1,N'Ocean Sports',N'15 Hoàng Sa',N'Mân Thái',N'Sơn Trà',N'Đà Nẵng',16.100,108.260,'0905999999',1,0,0,1),
(2,N'Star Court',N'200 Tôn Đức Thắng',N'Hòa Minh',N'Liên Chiểu',N'Đà Nẵng',16.090,108.170,'0905000000',1,1,0,0);
GO

-- ============================================================
-- COURTS (10)
-- ============================================================

INSERT INTO dbo.Courts
(VenueID, SportID, CourtName, CourtType, SurfaceType, MaxPlayers, Description)
VALUES
(1,1,N'Sân Pickleball A1','Indoor','Synthetic',4,N'Sân tiêu chuẩn'),
(2,2,N'Sân Tennis T1','Outdoor','Hard',4,N'Sân tennis'),
(3,3,N'Sân Cầu Lông B1','Indoor','Synthetic',4,N'Sân máy lạnh'),
(4,1,N'Sân Pickleball A2','Indoor','Synthetic',4,N'Sân mới'),
(5,2,N'Sân Tennis T2','Outdoor','Clay',4,N'Sân đất nện'),
(6,3,N'Sân Cầu Lông B2','Indoor','Wood',4,N'Sàn gỗ'),
(7,1,N'Sân Pickleball A3','Outdoor','Synthetic',4,N'Sân đẹp'),
(8,2,N'Sân Tennis T3','Outdoor','Grass',4,N'Sân cỏ'),
(9,3,N'Sân Cầu Lông B3','Indoor','Synthetic',4,N'Sân rộng'),
(10,1,N'Sân Pickleball A4','Indoor','Synthetic',4,N'Sân VIP');
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

INSERT INTO dbo.PricingRules(CourtID, SlotID, DayType, UnitPrice)
VALUES
(1,1,'Weekday',150000),
(2,2,'Weekday',200000),
(3,3,'Weekday',100000),
(4,4,'Weekend',180000),
(5,5,'Weekend',250000),
(6,6,'Weekday',120000),
(7,7,'Holiday',220000),
(8,8,'Weekday',300000),
(9,9,'Weekend',130000),
(10,10,'Holiday',210000);
GO

-- ============================================================
-- BOOKINGS
-- ============================================================

INSERT INTO dbo.Bookings
(UserID, CourtID, BookingDate, TotalAmount,
 DiscountAmount, FinalAmount, Status)
VALUES
(4,1,'2026-06-01',150000,0,150000,'Confirmed'),
(5,2,'2026-06-01',200000,10000,190000,'Confirmed'),
(6,3,'2026-06-02',100000,0,100000,'Pending'),
(7,4,'2026-06-02',180000,0,180000,'Completed'),
(8,5,'2026-06-03',250000,20000,230000,'Confirmed'),
(9,6,'2026-06-03',120000,0,120000,'Cancelled'),
(10,7,'2026-06-04',220000,0,220000,'Confirmed'),
(4,8,'2026-06-04',300000,50000,250000,'Completed'),
(5,9,'2026-06-05',130000,0,130000,'NoShow'),
(6,10,'2026-06-05',210000,10000,200000,'Confirmed');
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
 TransactionRef, Status, PaymentType, PaidAt)
VALUES
(1,150000,'VNPay','TXN001','Success','Payment',GETDATE()),
(2,190000,'MoMo','TXN002','Success','Payment',GETDATE()),
(3,100000,'Cash','TXN003','Pending','Payment',NULL),
(4,180000,'VNPay','TXN004','Success','Payment',GETDATE()),
(5,230000,'BankTransfer','TXN005','Success','Payment',GETDATE()),
(6,120000,'Cash','TXN006','Refunded','Refund',GETDATE()),
(7,220000,'ZaloPay','TXN007','Success','Payment',GETDATE()),
(8,250000,'VNPay','TXN008','Success','Payment',GETDATE()),
(9,130000,'MoMo','TXN009','Failed','Payment',NULL),
(10,200000,'BankTransfer','TXN010','Success','Payment',GETDATE());
GO

-- ============================================================
-- MATCHES (10)
-- ============================================================

INSERT INTO dbo.Matches
(CreatedByUserID, CourtID, BookingID, SportID, MatchDate, StartTime, EndTime,
 MatchType, SkillRequired, MaxParticipants, Title, Description, Status, RequiresApproval)
VALUES
(2,1,1,1,'2026-06-10','07:00','08:00','Doubles','Beginner',4,N'Pickleball sáng sớm',N'Giao lưu thân thiện', 'Open', 1),
(3,2,2,2,'2026-06-11','08:00','09:00','Singles','Intermediate',2,N'Tennis trung cấp',N'Tập luyện nâng cao', 'Open', 1),
(4,3,3,3,'2026-06-12','09:00','10:00','Doubles','Advanced',4,N'Cầu lông nâng cao',N'Đánh đôi tốc độ', 'Open', 1),
(5,4,4,1,'2026-06-13','10:00','11:00','Mixed','Any',4,N'Pickleball vui vẻ',N'Không yêu cầu trình độ', 'Open', 1),
(6,5,5,2,'2026-06-14','11:00','12:00','Singles','Professional',2,N'Tennis pro',N'Thách đấu chuyên nghiệp', 'Open', 1),
(7,6,6,3,'2026-06-15','12:00','13:00','Doubles','Intermediate',4,N'Cầu lông trưa',N'Giao lưu', 'Open', 1),
(8,7,7,1,'2026-06-16','13:00','14:00','Doubles','Beginner',4,N'Pickleball học hỏi',N'Người mới', 'Open', 1),
(9,8,8,2,'2026-06-17','14:00','15:00','Singles','Advanced',2,N'Tennis chiều',N'Tập luyện', 'Open', 1),
(10,9,9,3,'2026-06-18','15:00','16:00','Doubles','Any',4,N'Cầu lông tự do',N'Thân thiện', 'Open', 1),
(4,10,10,1,'2026-06-19','16:00','17:00','Mixed','Intermediate',4,N'Pickleball chiều',N'Giao lưu', 'Open', 1);
GO

-- ============================================================
-- MATCH PARTICIPANTS (10)
-- ============================================================

INSERT INTO dbo.MatchParticipants (MatchID, UserID, TeamSide, JoinStatus)
VALUES
(1,2,'A','Accepted'),
(1,4,'B','Accepted'),
(2,3,'A','Accepted'),
(3,5,'A','Pending'),
(4,6,'B','Accepted'),
(5,7,'A','Accepted'),
(6,8,'B','Pending'),
(7,9,'A','Accepted'),
(8,10,'B','Accepted'),
(9,4,'A','Pending');
GO

-- ============================================================
-- REVIEWS (10)
-- ============================================================

INSERT INTO dbo.Reviews (CourtID, UserID, BookingID, Rating, Comment)
VALUES
(1,4,1,5,N'Sân đẹp, sạch.'),
(2,5,2,4,N'Sân ổn, giá hợp lý.'),
(3,6,3,5,N'Ánh sáng tốt.'),
(4,7,4,3,N'Hơi đông.'),
(5,8,5,5,N'Sân mới, chất lượng.'),
(6,9,6,4,N'Nhân viên thân thiện.'),
(7,10,7,4,N'Vị trí thuận tiện.'),
(8,4,8,5,N'Sân cỏ đẹp.'),
(9,5,9,3,N'Trải nghiệm ổn.'),
(10,6,10,5,N'Rất hài lòng.');
GO

-- ============================================================
-- USER SPORT PROFILES (10)
-- ============================================================

INSERT INTO dbo.UserSportProfiles
(UserID, SportID, CourtPosition, PlayStyle, StrokeStrength, SelfRatedLevel, ExperienceYears, SkillLevel, Notes)
VALUES
(4,3,'BackCourt','Aggressive','Smash',4,3,'Intermediate',N'Ưa tấn công'),
(5,2,'AllRound','Balanced','Drive',3,2,'Intermediate',N'Đánh đều tay'),
(6,1,'FrontCourt','Aggressive','Smash',5,4,'Advanced',N'Tốc độ cao'),
(7,3,'AllRound','Defensive','Drop',2,1,'Beginner',N'Mới chơi'),
(8,2,'BackCourt','Balanced','Drive',4,3,'Advanced',N'Ổn định'),
(9,1,'FrontCourt','Aggressive','Smash',3,2,'Intermediate',N'Ưa phản xạ nhanh'),
(10,3,'AllRound','Balanced','AllRound',3,2,'Intermediate',N'Tự do'),
(4,1,'BackCourt','Aggressive','Smash',4,3,'Advanced',N'Pickleball tốt'),
(5,3,'FrontCourt','Defensive','Drop',2,1,'Beginner',N'Ưa phòng thủ'),
(6,2,'AllRound','Balanced','Drive',5,5,'Professional',N'Thực chiến');
GO

-- ============================================================
-- NOTIFICATIONS (10)
-- ============================================================

INSERT INTO dbo.Notifications (UserID, Type, Title, Message, LinkUrl, IsRead)
VALUES
(4,'MatchJoin',N'Yêu cầu tham gia',N'Bạn có 1 yêu cầu tham gia trận đấu mới.',N'/Matchmaking/Index',0),
(5,'MatchApprove',N'Được duyệt',N'Yêu cầu tham gia đã được duyệt.',N'/Matchmaking/Index',0),
(6,'MatchReject',N'Bị từ chối',N'Yêu cầu tham gia đã bị từ chối.',N'/Matchmaking/Index',1),
(7,'BookingConfirmed',N'Đặt sân thành công',N'Đơn đặt sân đã xác nhận.',N'/Bookings/MyBookings',0),
(8,'BookingCancelled',N'Hủy đặt sân',N'Đơn đặt sân đã bị hủy.',N'/Bookings/MyBookings',0),
(9,'System',N'Thông báo hệ thống',N'Cập nhật điều khoản sử dụng.',N'/Settings',1),
(10,'MatchJoin',N'Yêu cầu mới',N'Có người muốn tham gia trận đấu của bạn.',N'/Matchmaking/Index',0),
(4,'BookingConfirmed',N'Xác nhận thanh toán',N'Thanh toán đã thành công.',N'/Bookings/MyBookings',0),
(5,'System',N'Bảo trì',N'Hệ thống sẽ bảo trì tối nay.',N'/',1),
(6,'MatchApprove',N'Duyệt tham gia',N'Bạn đã được duyệt vào trận đấu.',N'/Matchmaking/Index',0);
GO