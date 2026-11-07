
CREATE DATABASE BookLy;
GO

USE BookLy;
GO

-- 1. Tai khoan
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    StudentCode NVARCHAR(30) NULL,
    Username NVARCHAR(50),
    Passwordd NVARCHAR(255),
    FullName NVARCHAR(150),
    Email NVARCHAR(100),
    Role NVARCHAR(20),
    Status NVARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- 2. The loai sach
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100)
);

-- 3. Sach
CREATE TABLE Books (
    BookId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT,
    Title NVARCHAR(200),
    Author NVARCHAR(150),
    Description NVARCHAR(MAX),
    ImageUrl NVARCHAR(500),
    TotalQuantity INT,
    AvailableQuantity INT,
    IsDeleted BIT DEFAULT 0,

    FOREIGN KEY (CategoryId)
    REFERENCES Categories(CategoryId)
);

-- 4. Phieu muon
CREATE TABLE BorrowRequests (
    BorrowId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    RequestedAt DATETIME DEFAULT GETDATE(),
    ApprovedBy INT NULL,
    DueDate DATETIME NULL,
    Status NVARCHAR(30),

    FOREIGN KEY (UserId)
    REFERENCES Users(UserId),

    FOREIGN KEY (ApprovedBy)
    REFERENCES Users(UserId)
);

-- 5. Chi tiet phieu muon
CREATE TABLE BorrowDetails (
    BorrowDetailId INT IDENTITY(1,1) PRIMARY KEY,
    BorrowId INT,
    BookId INT,
    Quantity INT,

    FOREIGN KEY (BorrowId)
    REFERENCES BorrowRequests(BorrowId),

    FOREIGN KEY (BookId)
    REFERENCES Books(BookId)
);

-- 6. Sach yeu thich
CREATE TABLE Favorites (
    FavoriteId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    BookId INT,

    FOREIGN KEY (UserId)
    REFERENCES Users(UserId),

    FOREIGN KEY (BookId)
    REFERENCES Books(BookId)
);

--insert

INSERT INTO Users
    (Username, Passwordd, FullName, Email, StudentCode, Role, Status)
VALUES
('admin', '123456', N'Quản trị viên', 'admin@bookly.com', NULL, 'Admin', 'Active'),
('student01', '123456', N'Nguyễn Văn An', 'an@student.com', 'SV001', 'Student', 'Active'),
('student02', '123456', N'Trần Thị Bình', 'binh@student.com', 'SV002', 'Student', 'Active'),
('student03', '123456', N'Lê Văn Minh', 'minh@student.com', 'SV003', 'Student', 'Locked');

INSERT INTO Categories (CategoryName)
VALUES
(N'Công nghệ thông tin'),
(N'Văn học'),
(N'Khoa học'),
(N'Lịch sử'),
(N'Kỹ năng sống');

INSERT INTO Books (
    CategoryId, Title, Author, Description,
    ImageUrl, TotalQuantity, AvailableQuantity
)
VALUES
(1, N'Lập trình C# cơ bản', N'Nguyễn Văn A',
 N'Sách hướng dẫn lập trình C# cho người mới bắt đầu.',
 NULL, 10, 10),

(1, N'Lập trình ASP.NET Core MVC', N'Trần Văn B',
 N'Sách học xây dựng website bằng ASP.NET Core MVC.',
 NULL, 8, 7),

(1, N'Cơ sở dữ liệu SQL Server', N'Lê Văn C',
 N'Sách giới thiệu SQL Server và ngôn ngữ SQL.',
 NULL, 6, 6),

(2, N'Dế Mèn phiêu lưu ký', N'Tô Hoài',
 N'Tác phẩm văn học nổi tiếng dành cho thiếu nhi.',
 NULL, 10, 9),

(2, N'Tắt đèn', N'Ngô Tất Tố',
 N'Tác phẩm văn học hiện thực Việt Nam.',
 NULL, 5, 5),

(3, N'Lược sử thời gian', N'Stephen Hawking',
 N'Sách khám phá vũ trụ và các lý thuyết khoa học.',
 NULL, 4, 4),

(4, N'Đại Việt sử ký toàn thư', N'Ngô Sĩ Liên',
 N'Tác phẩm ghi chép lịch sử Việt Nam.',
 NULL, 3, 3),

(5, N'Đắc nhân tâm', N'Dale Carnegie',
 N'Sách về giao tiếp và cách ứng xử trong cuộc sống.',
 NULL, 7, 7);