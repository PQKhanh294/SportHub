-- ============================================================
-- SPORT HUB - SQL SERVER DATABASE SCHEMA
-- Chuẩn: 3NF (Third Normal Form)
-- Phiên bản: 1.0
-- Ngày tạo: 2026-03-18
-- Mô tả: Hệ thống đặt sân Pickleball, Tennis, Cầu lông
-- ============================================================

USE master;
GO

-- Tạo database nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SportHubDB')
    CREATE DATABASE SportHubDB;
GO

USE SportHubDB;
GO

-- ============================================================
-- BƯỚC 1: XÓA CÁC BẢNG CŨ (NẾU CÓ) - Theo thứ tự phụ thuộc
-- ============================================================
IF OBJECT_ID('dbo.MatchParticipants', 'U') IS NOT NULL DROP TABLE dbo.MatchParticipants;
IF OBJECT_ID('dbo.Matches',           'U') IS NOT NULL DROP TABLE dbo.Matches;
IF OBJECT_ID('dbo.Reviews',           'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID('dbo.Payments',          'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.BookingSlots',      'U') IS NOT NULL DROP TABLE dbo.BookingSlots;
IF OBJECT_ID('dbo.Bookings',          'U') IS NOT NULL DROP TABLE dbo.Bookings;
IF OBJECT_ID('dbo.PricingRules',      'U') IS NOT NULL DROP TABLE dbo.PricingRules;
IF OBJECT_ID('dbo.TimeSlots',         'U') IS NOT NULL DROP TABLE dbo.TimeSlots;
IF OBJECT_ID('dbo.CourtImages',       'U') IS NOT NULL DROP TABLE dbo.CourtImages;
IF OBJECT_ID('dbo.Courts',            'U') IS NOT NULL DROP TABLE dbo.Courts;
IF OBJECT_ID('dbo.CourtVenues',       'U') IS NOT NULL DROP TABLE dbo.CourtVenues;
IF OBJECT_ID('dbo.CourtOwners',       'U') IS NOT NULL DROP TABLE dbo.CourtOwners;
IF OBJECT_ID('dbo.Sports',            'U') IS NOT NULL DROP TABLE dbo.Sports;
IF OBJECT_ID('dbo.UserRoles',         'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Roles',             'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Users',             'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- ============================================================
-- NHÓM 1: PHÂN QUYỀN & NGƯỜI DÙNG
-- ============================================================

-- Bảng: Roles (Vai trò)
-- Lý do tách riêng: đảm bảo 2NF/3NF, dễ mở rộng phân quyền
CREATE TABLE dbo.Roles (
    RoleID      INT           NOT NULL IDENTITY(1,1),
    RoleName    NVARCHAR(50)  NOT NULL,   -- 'Admin', 'CourtOwner', 'Player'
    Description NVARCHAR(200) NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleID),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

-- Bảng: Users (Người dùng)
-- Tách FullName → không vi phạm 3NF vì FullName không phụ thuộc vào thuộc tính nào khác ngoài UserID
CREATE TABLE dbo.Users (
    UserID          INT             NOT NULL IDENTITY(1,1),
    Email           NVARCHAR(150)   NOT NULL,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    FullName        NVARCHAR(100)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    AvatarUrl       NVARCHAR(500)   NULL,
    DateOfBirth     DATE            NULL,
    Gender          NCHAR(1)        NULL CHECK (Gender IN ('M','F','O')),  -- Male/Female/Other
    SkillLevel      NVARCHAR(20)    NULL CHECK (SkillLevel IN ('Beginner','Intermediate','Advanced','Professional')),
    IsActive        BIT             NOT NULL DEFAULT 1,
    IsVerified      BIT             NOT NULL DEFAULT 0,     -- Xác thực email/OTP
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

-- Bảng: UserRoles (Gán vai trò cho người dùng - quan hệ N:N)
-- Một người dùng có thể vừa là Player vừa là CourtOwner
CREATE TABLE dbo.UserRoles (
    UserID  INT NOT NULL,
    RoleID  INT NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserID, RoleID),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)  ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)  ON DELETE CASCADE
);

-- ============================================================
-- NHÓM 2: MÔN THỂ THAO & CƠ SỞ VẬT CHẤT
-- ============================================================

-- Bảng: Sports (Danh sách môn thể thao)
-- Lý do tách riêng: tên môn sport có thể thay đổi độc lập, đảm bảo 3NF
CREATE TABLE dbo.Sports (
    SportID     INT           NOT NULL IDENTITY(1,1),
    SportName   NVARCHAR(100) NOT NULL,   -- 'Pickleball', 'Tennis', 'Cầu lông'
    IconUrl     NVARCHAR(500) NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT PK_Sports PRIMARY KEY (SportID),
    CONSTRAINT UQ_Sports_SportName UNIQUE (SportName)
);

-- Bảng: CourtOwners (Thông tin chủ sân - mở rộng từ Users)
-- Lý do tách riêng: CourtOwner có các thuộc tính nghiệp vụ riêng, không phải tất cả User đều là CourtOwner
CREATE TABLE dbo.CourtOwners (
    OwnerID         INT             NOT NULL IDENTITY(1,1),
    UserID          INT             NOT NULL,
    BusinessName    NVARCHAR(200)   NOT NULL,
    TaxCode         NVARCHAR(20)    NULL,
    BankAccount     NVARCHAR(50)    NULL,
    BankName        NVARCHAR(100)   NULL,
    IsApproved      BIT             NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CourtOwners PRIMARY KEY (OwnerID),
    CONSTRAINT FK_CourtOwners_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_CourtOwners_UserID UNIQUE (UserID)   -- 1 user chỉ có 1 hồ sơ chủ sân
);

-- Bảng: CourtVenues (Cơ sở / địa điểm chứa sân)
-- Lý do tách riêng với Courts: Địa chỉ, tên cơ sở là thuộc tính của Venue, không thuộc về từng sân riêng lẻ
-- → Loại bỏ phụ thuộc bắc cầu (3NF): Court → VenueID → Address/City
CREATE TABLE dbo.CourtVenues (
    VenueID         INT             NOT NULL IDENTITY(1,1),
    OwnerID         INT             NOT NULL,
    VenueName       NVARCHAR(200)   NOT NULL,
    Address         NVARCHAR(300)   NOT NULL,
    Ward            NVARCHAR(100)   NULL,
    District        NVARCHAR(100)   NOT NULL,
    City            NVARCHAR(100)   NOT NULL,
    Latitude        DECIMAL(10,8)   NULL,
    Longitude       DECIMAL(11,8)   NULL,
    PhoneContact    NVARCHAR(20)    NULL,
    OpenTime        TIME            NOT NULL DEFAULT '06:00',
    CloseTime       TIME            NOT NULL DEFAULT '22:00',
    AmenityParking  BIT             NOT NULL DEFAULT 0,
    AmenityShower   BIT             NOT NULL DEFAULT 0,
    AmenityLocker   BIT             NOT NULL DEFAULT 0,
    AmenityFood     BIT             NOT NULL DEFAULT 0,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CourtVenues PRIMARY KEY (VenueID),
    CONSTRAINT FK_CourtVenues_Owners FOREIGN KEY (OwnerID) REFERENCES dbo.CourtOwners(OwnerID)
);

-- Bảng: Courts (Từng sân cụ thể bên trong Venue)
-- Một Venue có thể có nhiều sân của nhiều môn thể thao khác nhau
CREATE TABLE dbo.Courts (
    CourtID         INT             NOT NULL IDENTITY(1,1),
    VenueID         INT             NOT NULL,
    SportID         INT             NOT NULL,
    CourtName       NVARCHAR(100)   NOT NULL,   -- 'Sân A1', 'Sân Tennis 01'
    CourtType       NVARCHAR(50)    NULL,        -- 'Indoor', 'Outdoor'
    SurfaceType     NVARCHAR(50)    NULL,        -- 'Hard', 'Clay', 'Grass', 'Synthetic'
    MaxPlayers      TINYINT         NOT NULL DEFAULT 4,
    Description     NVARCHAR(500)   NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Courts PRIMARY KEY (CourtID),
    CONSTRAINT FK_Courts_Venues FOREIGN KEY (VenueID) REFERENCES dbo.CourtVenues(VenueID),
    CONSTRAINT FK_Courts_Sports FOREIGN KEY (SportID) REFERENCES dbo.Sports(SportID)
);

-- Bảng: CourtImages (Hình ảnh sân hoặc venue)
-- Lý do tách riêng: 1 sân có thể nhiều ảnh → vi phạm 1NF nếu lưu chung bằng cột
CREATE TABLE dbo.CourtImages (
    ImageID     INT             NOT NULL IDENTITY(1,1),
    CourtID     INT             NOT NULL,
    ImageUrl    NVARCHAR(500)   NOT NULL,
    Caption     NVARCHAR(200)   NULL,
    SortOrder   TINYINT         NOT NULL DEFAULT 0,
    IsMain      BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_CourtImages PRIMARY KEY (ImageID),
    CONSTRAINT FK_CourtImages_Courts FOREIGN KEY (CourtID) REFERENCES dbo.Courts(CourtID) ON DELETE CASCADE
);

-- ============================================================
-- NHÓM 3: LỊCH & BẢNG GIÁ
-- ============================================================

-- Bảng: TimeSlots (Khung giờ hoạt động của sân)
-- Tách riêng để chuẩn hóa, tránh lưu lặp thông tin giờ vào từng booking
CREATE TABLE dbo.TimeSlots (
    SlotID      INT         NOT NULL IDENTITY(1,1),
    StartTime   TIME        NOT NULL,   -- VD: '07:00'
    EndTime     TIME        NOT NULL,   -- VD: '08:00'
    SlotLabel   NVARCHAR(20) NULL,      -- VD: '7:00 - 8:00'
    CONSTRAINT PK_TimeSlots PRIMARY KEY (SlotID),
    CONSTRAINT UQ_TimeSlots UNIQUE (StartTime, EndTime)
);

-- Bảng: PricingRules (Bảng giá theo sân + khung giờ + ngày)
-- Lý do tách riêng: Giá phụ thuộc vào (CourtID, SlotID, DayType) → không vi phạm 3NF
-- nếu để trong Courts thì gây dư thừa khi giá thay đổi theo ngày/giờ
CREATE TABLE dbo.PricingRules (
    PricingID   INT             NOT NULL IDENTITY(1,1),
    CourtID     INT             NOT NULL,
    SlotID      INT             NOT NULL,
    DayType     NVARCHAR(20)    NOT NULL CHECK (DayType IN ('Weekday','Weekend','Holiday')),
    UnitPrice   DECIMAL(12,2)   NOT NULL,
    Currency    NCHAR(3)        NOT NULL DEFAULT 'VND',
    ValidFrom   DATE            NOT NULL DEFAULT GETDATE(),
    ValidTo     DATE            NULL,
    CONSTRAINT PK_PricingRules PRIMARY KEY (PricingID),
    CONSTRAINT FK_PricingRules_Courts FOREIGN KEY (CourtID) REFERENCES dbo.Courts(CourtID),
    CONSTRAINT FK_PricingRules_Slots  FOREIGN KEY (SlotID)  REFERENCES dbo.TimeSlots(SlotID),
    CONSTRAINT UQ_PricingRules UNIQUE (CourtID, SlotID, DayType, ValidFrom)
);

-- ============================================================
-- NHÓM 4: ĐẶT SÂN & THANH TOÁN
-- ============================================================

-- Bảng: Bookings (Đơn đặt sân)
-- Chỉ lưu UserID và CourtID (FK), không lưu tên sân hay tên khách → đảm bảo 3NF
CREATE TABLE dbo.Bookings (
    BookingID       INT             NOT NULL IDENTITY(1,1),
    UserID          INT             NOT NULL,
    CourtID         INT             NOT NULL,
    BookingDate     DATE            NOT NULL,   -- Ngày đặt sân
    TotalAmount     DECIMAL(12,2)   NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(12,2)   NOT NULL DEFAULT 0,
    FinalAmount     DECIMAL(12,2)   NOT NULL DEFAULT 0,
    Status          NVARCHAR(30)    NOT NULL DEFAULT 'Pending'
                        CHECK (Status IN ('Pending','Confirmed','Cancelled','Completed','NoShow')),
    Note            NVARCHAR(500)   NULL,
    CancelReason    NVARCHAR(300)   NULL,
    BookedAt        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Bookings PRIMARY KEY (BookingID),
    CONSTRAINT FK_Bookings_Users  FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Bookings_Courts FOREIGN KEY (CourtID) REFERENCES dbo.Courts(CourtID)
);

-- Bảng: BookingSlots (Chi tiết các khung giờ trong một đơn đặt sân)
-- Lý do tách riêng: 1 booking có thể bao gồm nhiều khung giờ liên tiếp → tránh vi phạm 1NF
CREATE TABLE dbo.BookingSlots (
    BookingSlotID   INT             NOT NULL IDENTITY(1,1),
    BookingID       INT             NOT NULL,
    SlotID          INT             NOT NULL,
    UnitPrice       DECIMAL(12,2)   NOT NULL,   -- Giá tại thời điểm đặt (snapshot)
    CONSTRAINT PK_BookingSlots PRIMARY KEY (BookingSlotID),
    CONSTRAINT FK_BookingSlots_Bookings  FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID)  ON DELETE CASCADE,
    CONSTRAINT FK_BookingSlots_TimeSlots FOREIGN KEY (SlotID)    REFERENCES dbo.TimeSlots(SlotID),
    CONSTRAINT UQ_BookingSlots UNIQUE (BookingID, SlotID)
);

-- Bảng: Payments (Lịch sử thanh toán)
-- Tách riêng với Bookings: 1 booking có thể có nhiều lần thanh toán (hoàn tiền, trả thêm)
CREATE TABLE dbo.Payments (
    PaymentID       INT             NOT NULL IDENTITY(1,1),
    BookingID       INT             NOT NULL,
    Amount          DECIMAL(12,2)   NOT NULL,
    PaymentMethod   NVARCHAR(50)    NOT NULL CHECK (PaymentMethod IN ('VNPay','MoMo','ZaloPay','BankTransfer','Cash')),
    TransactionRef  NVARCHAR(100)   NULL,       -- Mã giao dịch từ cổng thanh toán
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending'
                        CHECK (Status IN ('Pending','Success','Failed','Refunded')),
    PaymentType     NVARCHAR(20)    NOT NULL DEFAULT 'Payment'
                        CHECK (PaymentType IN ('Payment','Refund')),
    PaidAt          DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Payments PRIMARY KEY (PaymentID),
    CONSTRAINT FK_Payments_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID)
);

-- ============================================================
-- NHÓM 5: CHỨC NĂNG TÌM ĐỐI & TRẬN ĐẤU
-- ============================================================

-- Bảng: Matches (Trận đấu / Tìm đối thủ)
CREATE TABLE dbo.Matches (
    MatchID         INT             NOT NULL IDENTITY(1,1),
    CreatedByUserID INT             NOT NULL,
    CourtID         INT             NULL,           -- Có thể chưa chọn sân khi tạo trận
    BookingID       INT             NULL,           -- Liên kết đơn đặt sân khi đã book
    SportID         INT             NOT NULL,
    MatchDate       DATE            NOT NULL,
    StartTime       TIME            NOT NULL,
    EndTime         TIME            NOT NULL,
    MatchType       NVARCHAR(20)    NOT NULL CHECK (MatchType IN ('Singles','Doubles','Mixed')),
    SkillRequired   NVARCHAR(20)    NULL CHECK (SkillRequired IN ('Beginner','Intermediate','Advanced','Professional','Any')),
    MaxParticipants TINYINT         NOT NULL DEFAULT 4,
    Title           NVARCHAR(200)   NULL,
    Description     NVARCHAR(500)   NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Open'
                        CHECK (Status IN ('Open','Full','InProgress','Completed','Cancelled')),
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Matches PRIMARY KEY (MatchID),
    CONSTRAINT FK_Matches_Users    FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Matches_Courts   FOREIGN KEY (CourtID)         REFERENCES dbo.Courts(CourtID),
    CONSTRAINT FK_Matches_Bookings FOREIGN KEY (BookingID)        REFERENCES dbo.Bookings(BookingID),
    CONSTRAINT FK_Matches_Sports   FOREIGN KEY (SportID)          REFERENCES dbo.Sports(SportID)
);

-- Bảng: MatchParticipants (Thành viên tham gia trận đấu - quan hệ N:N)
CREATE TABLE dbo.MatchParticipants (
    ParticipantID   INT             NOT NULL IDENTITY(1,1),
    MatchID         INT             NOT NULL,
    UserID          INT             NOT NULL,
    TeamSide        NCHAR(1)        NULL CHECK (TeamSide IN ('A','B')),   -- Đội A hoặc B
    JoinStatus      NVARCHAR(20)    NOT NULL DEFAULT 'Pending'
                        CHECK (JoinStatus IN ('Pending','Accepted','Declined','Cancelled')),
    JoinedAt        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MatchParticipants PRIMARY KEY (ParticipantID),
    CONSTRAINT FK_MatchParticipants_Matches FOREIGN KEY (MatchID) REFERENCES dbo.Matches(MatchID) ON DELETE CASCADE,
    CONSTRAINT FK_MatchParticipants_Users   FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_MatchParticipants UNIQUE (MatchID, UserID)
);

-- ============================================================
-- NHÓM 6: ĐÁNH GIÁ
-- ============================================================

-- Bảng: Reviews (Đánh giá sân sau khi hoàn thành lịch sân)
-- Chỉ lưu CourtID và UserID (FK), không lưu tên sân → đảm bảo 3NF
CREATE TABLE dbo.Reviews (
    ReviewID    INT             NOT NULL IDENTITY(1,1),
    CourtID     INT             NOT NULL,
    UserID      INT             NOT NULL,
    BookingID   INT             NOT NULL,             -- Phải có booking hợp lệ mới review được
    Rating      TINYINT         NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment     NVARCHAR(1000)  NULL,
    ReviewedAt  DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Reviews PRIMARY KEY (ReviewID),
    CONSTRAINT FK_Reviews_Courts   FOREIGN KEY (CourtID)   REFERENCES dbo.Courts(CourtID),
    CONSTRAINT FK_Reviews_Users    FOREIGN KEY (UserID)    REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Reviews_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID),
    CONSTRAINT UQ_Reviews UNIQUE (BookingID, UserID)  -- Mỗi user chỉ review một lần / booking
);
GO

-- ============================================================
-- DỮ LIỆU MẪU (SEED DATA)
-- ============================================================

-- Roles
INSERT INTO dbo.Roles (RoleName, Description) VALUES
('Admin',       N'Quản trị viên hệ thống'),
('CourtOwner',  N'Chủ sân thể thao'),
('Player',      N'Người chơi / Khách hàng');

-- Sports
INSERT INTO dbo.Sports (SportName, Description) VALUES
(N'Pickleball', N'Môn thể thao kết hợp tennis, bóng bàn và cầu lông'),
(N'Tennis',     N'Môn quần vợt sân cứng/đất nện/thảm'),
(N'Cầu lông',   N'Badminton - môn vợt cầu trong nhà');

-- TimeSlots (Mỗi slot 1 giờ, từ 6:00 đến 22:00)
INSERT INTO dbo.TimeSlots (StartTime, EndTime, SlotLabel) VALUES
('06:00','07:00','6:00 - 7:00'),
('07:00','08:00','7:00 - 8:00'),
('08:00','09:00','8:00 - 9:00'),
('09:00','10:00','9:00 - 10:00'),
('10:00','11:00','10:00 - 11:00'),
('11:00','12:00','11:00 - 12:00'),
('12:00','13:00','12:00 - 13:00'),
('13:00','14:00','13:00 - 14:00'),
('14:00','15:00','14:00 - 15:00'),
('15:00','16:00','15:00 - 16:00'),
('16:00','17:00','16:00 - 17:00'),
('17:00','18:00','17:00 - 18:00'),
('18:00','19:00','18:00 - 19:00'),
('19:00','20:00','19:00 - 20:00'),
('20:00','21:00','20:00 - 21:00'),
('21:00','22:00','21:00 - 22:00');
GO

-- ============================================================
-- INDEX HỖ TRỢ TRUY VẤN
-- ============================================================

-- Tìm sân theo thành phố / quận
CREATE NONCLUSTERED INDEX IX_CourtVenues_City     ON dbo.CourtVenues (City, District);
-- Tìm sân theo môn thể thao
CREATE NONCLUSTERED INDEX IX_Courts_SportID       ON dbo.Courts (SportID, VenueID);
-- Lịch sử đặt sân của user
CREATE NONCLUSTERED INDEX IX_Bookings_UserID      ON dbo.Bookings (UserID, BookingDate DESC);
-- Kiểm tra sân trống theo ngày
CREATE NONCLUSTERED INDEX IX_Bookings_CourtDate   ON dbo.Bookings (CourtID, BookingDate, Status);
-- Tìm trận đấu mở
CREATE NONCLUSTERED INDEX IX_Matches_Status       ON dbo.Matches (Status, MatchDate, SportID);
GO

PRINT N'✅ SportHubDB - Tạo CSDL thành công! Tổng: 16 bảng, chuẩn 3NF.';
GO

