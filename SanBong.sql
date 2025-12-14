CREATE DATABASE QL_DatSanBong;
GO
USE QL_DatSanBong;
GO

-- Bảng Tài khoản
CREATE TABLE TaiKhoan (
    MaTK INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) UNIQUE NOT NULL,
    MatKhau NVARCHAR(255) NOT NULL,
    VaiTro NVARCHAR(20) CHECK (VaiTro IN ('Admin', 'NhanVien', 'KhachHang')) NOT NULL,
    TrangThai BIT DEFAULT 1
);

-- Bảng Loại sân
CREATE TABLE LoaiSan (
    MaLoai INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai NVARCHAR(100) NOT NULL, -- Sân 5, Sân 7, Sân 11
    MoTa NVARCHAR(255)
);

-- Bảng Sân bóng
CREATE TABLE SanBong (
    MaSan INT IDENTITY(1,1) PRIMARY KEY,
    TenSan NVARCHAR(100) NOT NULL,
    MaLoai INT FOREIGN KEY REFERENCES LoaiSan(MaLoai),
    GiaTheoGio DECIMAL(10,2) NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Hoạt động', -- Hoạt động, Bảo trì, Ngưng hoạt động
    ViTri NVARCHAR(200),
    MoTa NVARCHAR(500),
    HinhAnh NVARCHAR(255)
);

-- Bảng Khung giờ
CREATE TABLE KhungGio (
    MaKhungGio INT IDENTITY(1,1) PRIMARY KEY,
    GioBatDau TIME NOT NULL,
    GioKetThuc TIME NOT NULL,
    HeSoGia DECIMAL(3,2) DEFAULT 1.0 -- Hệ số giá (giờ vàng x1.5, giờ thường x1.0)
);

-- Bảng Nhân viên
CREATE TABLE NhanVien (
    MaNV INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE,
    GioiTinh NVARCHAR(10),
    DienThoai NVARCHAR(20),
    Email NVARCHAR(100),
    ChucVu NVARCHAR(50),
    MaTK INT FOREIGN KEY REFERENCES TaiKhoan(MaTK)
);

-- Bảng Khách hàng
CREATE TABLE KhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    DiaChi NVARCHAR(200),
    DienThoai NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100),
    CCCD NVARCHAR(20),
    DiemTichLuy INT DEFAULT 0,
    MaTK INT FOREIGN KEY REFERENCES TaiKhoan(MaTK)
);

-- Bảng Đặt sân
CREATE TABLE DatSan (
    MaDatSan INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT FOREIGN KEY REFERENCES KhachHang(MaKH),
    MaSan INT FOREIGN KEY REFERENCES SanBong(MaSan),
    MaKhungGio INT FOREIGN KEY REFERENCES KhungGio(MaKhungGio),
    NgayDat DATE NOT NULL,
    NgaySD DATETIME NOT NULL, -- Ngày sử dụng sân
    ThoiGianDat DATETIME DEFAULT GETDATE(),
    TongTien DECIMAL(10,2),
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xác nhận', -- Chờ xác nhận, Đã xác nhận, Đang sử dụng, Hoàn tất, Đã hủy
    GhiChu NVARCHAR(500),
    MaNV INT FOREIGN KEY REFERENCES NhanVien(MaNV) -- Nhân viên xác nhận
);

-- Bảng Dịch vụ thêm (nước uống, thuê giày, áo đấu...)
CREATE TABLE DichVu (
    MaDV INT IDENTITY(1,1) PRIMARY KEY,
    TenDV NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(10,2) NOT NULL,
    DonVi NVARCHAR(50), -- chai, đôi, bộ
    MoTa NVARCHAR(255),
    HinhAnh NVARCHAR(255),
    SoLuongTon INT DEFAULT 0
);

-- Bảng Chi tiết đặt sân - Dịch vụ
CREATE TABLE ChiTietDichVu (
    MaCTDV INT IDENTITY(1,1) PRIMARY KEY,
    MaDatSan INT FOREIGN KEY REFERENCES DatSan(MaDatSan),
    MaDV INT FOREIGN KEY REFERENCES DichVu(MaDV),
    SoLuong INT NOT NULL,
    DonGia DECIMAL(10,2) NOT NULL,
    ThanhTien DECIMAL(10,2)
);

-- Bảng Thanh toán
CREATE TABLE ThanhToan (
    MaTT INT IDENTITY(1,1) PRIMARY KEY,
    MaDatSan INT FOREIGN KEY REFERENCES DatSan(MaDatSan),
    PhuongThuc NVARCHAR(50) NOT NULL, -- Tiền mặt, Chuyển khoản, Ví điện tử
    SoTien DECIMAL(10,2) NOT NULL,
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) DEFAULT N'Đã thanh toán'
);

-- Bảng Đánh giá
CREATE TABLE DanhGia (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,
    MaDatSan INT FOREIGN KEY REFERENCES DatSan(MaDatSan),
    MaKH INT FOREIGN KEY REFERENCES KhachHang(MaKH),
    DiemDanhGia INT CHECK (DiemDanhGia BETWEEN 1 AND 5),
    NoiDung NVARCHAR(500),
    NgayDanhGia DATETIME DEFAULT GETDATE()
);

-- Bảng Liên hệ
CREATE TABLE LienHe (
    MaLienHe INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    SoDienThoai NVARCHAR(15),
    TieuDe NVARCHAR(200),
    NoiDung NVARCHAR(MAX) NOT NULL,
    NgayGui DATETIME DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) DEFAULT N'Chưa xử lý'
);


-- Tài khoản Admin
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro)
VALUES
('admin', 'admin123', 'Admin'),
('admin2', 'admin456', 'Admin');

-- Tài khoản Nhân viên
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro)
VALUES
('nv.nam', 'nv123', 'NhanVien'),
('nv.hoa', 'nv123', 'NhanVien');

-- Tài khoản Khách hàng
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro)
VALUES
('kh.an', 'kh123', 'KhachHang'),
('kh.binh', 'kh123', 'KhachHang'),
('kh.cuong', 'kh123', 'KhachHang'),
('kh.dung', 'kh123', 'KhachHang'),
('kh.em', 'kh123', 'KhachHang');

-- Loại sân
INSERT INTO LoaiSan (TenLoai, MoTa)
VALUES 
(N'Sân 5 người', N'Sân bóng đá mini 5 người'),
(N'Sân 7 người', N'Sân bóng đá 7 người'),
(N'Sân 11 người', N'Sân bóng đá 11 người tiêu chuẩn');

-- Sân bóng
INSERT INTO SanBong (TenSan, MaLoai, GiaTheoGio, TrangThai, ViTri, MoTa, HinhAnh)
VALUES
(N'Sân A1', 1, 300000, N'Hoạt động', N'Khu A - Tầng 1', N'Sân 5 người có mái che, cỏ nhân tạo cao cấp', 'san_a1.jpg'),
(N'Sân A2', 1, 300000, N'Hoạt động', N'Khu A - Tầng 1', N'Sân 5 người có mái che', 'san_a2.jpg'),
(N'Sân B1', 2, 500000, N'Hoạt động', N'Khu B - Tầng 2', N'Sân 7 người sân cỏ nhân tạo', 'san_b1.jpg'),
(N'Sân B2', 2, 500000, N'Hoạt động', N'Khu B - Tầng 2', N'Sân 7 người có đèn chiếu sáng', 'san_b2.jpg'),
(N'Sân C1', 3, 1000000, N'Hoạt động', N'Khu C - Sân ngoài', N'Sân 11 người tiêu chuẩn FIFA', 'san_c1.jpg'),
(N'Sân VIP', 1, 400000, N'Hoạt động', N'Khu VIP', N'Sân 5 người VIP có điều hòa', 'san_vip.jpg');

-- Khung giờ
INSERT INTO KhungGio (GioBatDau, GioKetThuc, HeSoGia)
VALUES
('06:00', '08:00', 0.8),  -- Giờ sáng sớm
('08:00', '10:00', 1.0),
('10:00', '12:00', 1.0),
('12:00', '14:00', 0.9),  -- Giờ trưa
('14:00', '16:00', 1.0),
('16:00', '18:00', 1.2),  -- Giờ chiều
('18:00', '20:00', 1.5),  -- Giờ vàng
('20:00', '22:00', 1.5),  -- Giờ vàng
('22:00', '24:00', 1.2);

-- Nhân viên
INSERT INTO NhanVien (HoTen, NgaySinh, GioiTinh, DienThoai, Email, ChucVu, MaTK)
VALUES 
(N'Nguyễn Văn Nam', '1990-04-12', N'Nam', '0909000111', 'namnv@sanbong.vn', N'Quản lý', 3),
(N'Trần Thị Hoa', '1995-08-25', N'Nữ', '0909111222', 'hoatt@sanbong.vn', N'Nhân viên lễ tân', 4);

-- Khách hàng
INSERT INTO KhachHang (HoTen, DiaChi, DienThoai, Email, CCCD, DiemTichLuy, MaTK)
VALUES 
(N'Lê Văn An', N'Quận 1, TP.HCM', '0905111222', 'levan.an@gmail.com', '001234567890', 50, 5),
(N'Phạm Minh Bình', N'Quận 3, TP.HCM', '0905222333', 'pham.binh@gmail.com', '001234567891', 120, 6),
(N'Hoàng Văn Cường', N'Quận 5, TP.HCM', '0905333444', 'hoang.cuong@gmail.com', '001234567892', 80, 7),
(N'Trần Thị Dung', N'Thủ Đức, TP.HCM', '0905444555', 'tran.dung@gmail.com', '001234567893', 30, 8),
(N'Nguyễn Thị Em', N'Bình Thạnh, TP.HCM', '0905555666', 'nguyen.em@gmail.com', '001234567894', 100, 9);

-- Dịch vụ thêm
INSERT INTO DichVu (TenDV, DonGia, DonVi, MoTa, HinhAnh, SoLuongTon)
VALUES
(N'Nước suối Aquafina', 10000, N'chai', N'Nước khoáng tinh khiết', 'aqua.jpg', 200),
(N'Nước tăng lực Redbull', 25000, N'lon', N'Nước tăng lực', 'redbull.jpg', 100),
(N'Thuê giày đá bóng', 30000, N'đôi/trận', N'Giày đá bóng chuyên dụng', 'giay.jpg', 50),
(N'Thuê áo đấu', 50000, N'bộ/trận', N'Áo đấu theo đội, đủ size', 'ao.jpg', 30),
(N'Thuê bóng', 20000, N'quả/trận', N'Bóng đá size 5 tiêu chuẩn', 'bong.jpg', 40),
(N'Khăn lạnh', 5000, N'chiếc', N'Khăn lạnh làm mát', 'khan.jpg', 150);

-- Đặt sân mẫu
INSERT INTO DatSan (MaKH, MaSan, MaKhungGio, NgayDat, NgaySD, TongTien, TrangThai, GhiChu, MaNV)
VALUES 
(1, 1, 7, GETDATE(), DATEADD(DAY, 2, GETDATE()), 450000, N'Đã xác nhận', N'Khách quen', 1),
(2, 2, 8, GETDATE(), DATEADD(DAY, 3, GETDATE()), 450000, N'Chờ xác nhận', NULL, NULL),
(3, 3, 6, GETDATE(), DATEADD(DAY, 1, GETDATE()), 600000, N'Đã xác nhận', N'Đặt cho công ty', 2),
(4, 5, 7, GETDATE(), DATEADD(DAY, 5, GETDATE()), 1500000, N'Đã xác nhận', N'Giải đấu công ty', 1),
(5, 1, 3, GETDATE()-1, GETDATE(), 300000, N'Hoàn tất', N'Đã sử dụng', 1);

-- Chi tiết dịch vụ
INSERT INTO ChiTietDichVu (MaDatSan, MaDV, SoLuong, DonGia, ThanhTien)
VALUES
(1, 1, 10, 10000, 100000),
(1, 5, 1, 20000, 20000),
(3, 2, 6, 25000, 150000),
(3, 3, 7, 30000, 210000),
(4, 4, 11, 50000, 550000);

-- Thanh toán
INSERT INTO ThanhToan (MaDatSan, PhuongThuc, SoTien, TrangThai)
VALUES
(1, N'Chuyển khoản', 570000, N'Đã thanh toán'),
(5, N'Tiền mặt', 300000, N'Đã thanh toán');

-- Đánh giá
INSERT INTO DanhGia (MaDatSan, MaKH, DiemDanhGia, NoiDung)
VALUES
(5, 5, 5, N'Sân đẹp, cỏ mượt, nhân viên nhiệt tình'),
(1, 1, 4, N'Sân tốt, giá hợp lý');

--FUNCTION

-- Tính tổng tiền đặt sân (bao gồm giá sân + dịch vụ)
CREATE FUNCTION fn_TinhTongTienDatSan(@MaDatSan INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @TienSan DECIMAL(10,2);
    DECLARE @TienDichVu DECIMAL(10,2);
    
    -- Tính tiền sân
    SELECT @TienSan = s.GiaTheoGio * k.HeSoGia
    FROM DatSan d
    JOIN SanBong s ON d.MaSan = s.MaSan
    JOIN KhungGio k ON d.MaKhungGio = k.MaKhungGio
    WHERE d.MaDatSan = @MaDatSan;
    
    -- Tính tiền dịch vụ
    SELECT @TienDichVu = SUM(ThanhTien)
    FROM ChiTietDichVu
    WHERE MaDatSan = @MaDatSan;
    
    RETURN ISNULL(@TienSan, 0) + ISNULL(@TienDichVu, 0);
END;
GO

-- Kiểm tra sân có trống không
CREATE FUNCTION fn_KiemTraSanTrong(
    @MaSan INT,
    @NgaySD DATETIME,
    @MaKhungGio INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @KetQua BIT = 1;
    
    IF EXISTS (
        SELECT 1 
        FROM DatSan 
        WHERE MaSan = @MaSan 
        AND CAST(NgaySD AS DATE) = CAST(@NgaySD AS DATE)
        AND MaKhungGio = @MaKhungGio
        AND TrangThai NOT IN (N'Đã hủy')
    )
    BEGIN
        SET @KetQua = 0;
    END
    
    RETURN @KetQua;
END;
GO

--PROCEDURE

-- Thêm sân bóng
CREATE PROCEDURE sp_ThemSanBong
    @TenSan NVARCHAR(100),
    @MaLoai INT,
    @GiaTheoGio DECIMAL(10,2),
    @ViTri NVARCHAR(200),
    @MoTa NVARCHAR(500),
    @HinhAnh NVARCHAR(255)
AS
BEGIN
    INSERT INTO SanBong (TenSan, MaLoai, GiaTheoGio, ViTri, MoTa, HinhAnh)
    VALUES (@TenSan, @MaLoai, @GiaTheoGio, @ViTri, @MoTa, @HinhAnh);
END;
GO

-- Sửa thông tin sân
CREATE PROCEDURE sp_SuaSanBong
    @MaSan INT,
    @TenSan NVARCHAR(100),
    @MaLoai INT,
    @GiaTheoGio DECIMAL(10,2),
    @TrangThai NVARCHAR(50),
    @ViTri NVARCHAR(200),
    @MoTa NVARCHAR(500),
    @HinhAnh NVARCHAR(255)
AS
BEGIN
    UPDATE SanBong
    SET TenSan = @TenSan,
        MaLoai = @MaLoai,
        GiaTheoGio = @GiaTheoGio,
        TrangThai = @TrangThai,
        ViTri = @ViTri,
        MoTa = @MoTa,
        HinhAnh = @HinhAnh
    WHERE MaSan = @MaSan;
END;
GO

-- Xóa sân
CREATE PROCEDURE sp_XoaSanBong
    @MaSan INT
AS
BEGIN
    DELETE FROM SanBong WHERE MaSan = @MaSan;
END;
GO

-- Đặt sân
CREATE PROCEDURE sp_DatSan
    @MaKH INT,
    @MaSan INT,
    @MaKhungGio INT,
    @NgaySD DATETIME,
    @GhiChu NVARCHAR(500) = NULL
AS
BEGIN
    -- Kiểm tra sân có trống không
    IF dbo.fn_KiemTraSanTrong(@MaSan, @NgaySD, @MaKhungGio) = 0
    BEGIN
        RAISERROR(N'Sân đã được đặt trong khung giờ này!', 16, 1);
        RETURN;
    END
    
    DECLARE @TongTien DECIMAL(10,2);
    
    -- Tính tiền sân
    SELECT @TongTien = s.GiaTheoGio * k.HeSoGia
    FROM SanBong s, KhungGio k
    WHERE s.MaSan = @MaSan AND k.MaKhungGio = @MaKhungGio;
    
    -- Tạo đơn đặt sân
    INSERT INTO DatSan (MaKH, MaSan, MaKhungGio, NgayDat, NgaySD, TongTien, TrangThai, GhiChu)
    VALUES (@MaKH, @MaSan, @MaKhungGio, GETDATE(), @NgaySD, @TongTien, N'Chờ xác nhận', @GhiChu);
    
    SELECT SCOPE_IDENTITY() AS MaDatSan;
END;
GO

-- Xác nhận đặt sân (Nhân viên/Admin)
CREATE PROCEDURE sp_XacNhanDatSan
    @MaDatSan INT,
    @MaNV INT
AS
BEGIN
    UPDATE DatSan
    SET TrangThai = N'Đã xác nhận',
        MaNV = @MaNV
    WHERE MaDatSan = @MaDatSan;
END;
GO

-- Hủy đặt sân
CREATE PROCEDURE sp_HuyDatSan
    @MaDatSan INT
AS
BEGIN
    UPDATE DatSan
    SET TrangThai = N'Đã hủy'
    WHERE MaDatSan = @MaDatSan;
END;
GO

-- Thêm dịch vụ vào đơn đặt sân
CREATE PROCEDURE sp_ThemDichVu
    @MaDatSan INT,
    @MaDV INT,
    @SoLuong INT
AS
BEGIN
    DECLARE @DonGia DECIMAL(10,2);
    DECLARE @ThanhTien DECIMAL(10,2);
    
    SELECT @DonGia = DonGia FROM DichVu WHERE MaDV = @MaDV;
    SET @ThanhTien = @DonGia * @SoLuong;
    
    INSERT INTO ChiTietDichVu (MaDatSan, MaDV, SoLuong, DonGia, ThanhTien)
    VALUES (@MaDatSan, @MaDV, @SoLuong, @DonGia, @ThanhTien);
    
    -- Cập nhật tổng tiền
    DECLARE @TongTien DECIMAL(10,2);
    SET @TongTien = dbo.fn_TinhTongTienDatSan(@MaDatSan);
    UPDATE DatSan SET TongTien = @TongTien WHERE MaDatSan = @MaDatSan;
END;
GO

-- Thanh toán
CREATE PROCEDURE sp_ThanhToan
    @MaDatSan INT,
    @PhuongThuc NVARCHAR(50),
    @SoTien DECIMAL(10,2)
AS
BEGIN
    INSERT INTO ThanhToan (MaDatSan, PhuongThuc, SoTien)
    VALUES (@MaDatSan, @PhuongThuc, @SoTien);
    
    -- Cập nhật trạng thái đơn đặt
    UPDATE DatSan
    SET TrangThai = N'Hoàn tất'
    WHERE MaDatSan = @MaDatSan;
END;
GO

--TRIGGER

-- Tự động cập nhật tổng tiền khi thêm/sửa/xóa dịch vụ
CREATE TRIGGER trg_CapNhatTongTienDatSan
ON ChiTietDichVu
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @MaDatSan INT;
    
    SELECT TOP 1 @MaDatSan = COALESCE(i.MaDatSan, d.MaDatSan)
    FROM inserted i
    FULL JOIN deleted d ON i.MaCTDV = d.MaCTDV;
    
    IF @MaDatSan IS NOT NULL
    BEGIN
        DECLARE @TongTien DECIMAL(10,2);
        SET @TongTien = dbo.fn_TinhTongTienDatSan(@MaDatSan);
        
        UPDATE DatSan
        SET TongTien = @TongTien
        WHERE MaDatSan = @MaDatSan;
    END
END;
GO

-- Tự động giảm số lượng dịch vụ khi thêm vào đơn
CREATE TRIGGER trg_GiamSoLuongDichVu
ON ChiTietDichVu
AFTER INSERT
AS
BEGIN
    UPDATE dv
    SET dv.SoLuongTon = dv.SoLuongTon - i.SoLuong
    FROM DichVu dv
    JOIN inserted i ON dv.MaDV = i.MaDV;
END;
GO

-- Tự động tăng điểm tích lũy sau khi hoàn tất
CREATE TRIGGER trg_TangDiemTichLuy
ON DatSan
AFTER UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        JOIN deleted d ON i.MaDatSan = d.MaDatSan
        WHERE i.TrangThai = N'Hoàn tất' AND d.TrangThai != N'Hoàn tất'
    )
    BEGIN
        UPDATE kh
        SET kh.DiemTichLuy = kh.DiemTichLuy + CAST(i.TongTien / 10000 AS INT)
        FROM KhachHang kh
        JOIN inserted i ON kh.MaKH = i.MaKH
        JOIN deleted d ON i.MaDatSan = d.MaDatSan
        WHERE i.TrangThai = N'Hoàn tất' AND d.TrangThai != N'Hoàn tất';
    END
END;
GO