USE QL_DatSanBong;
GO

CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT FOREIGN KEY REFERENCES KhachHang(MaKH),
    MaDatSan INT FOREIGN KEY REFERENCES DatSan(MaDatSan),
    MaHoaDonCode NVARCHAR(50) NOT NULL, -- Mã hóa đơn hiển thị (VD: HD20231223001)
    NgayLap DATETIME DEFAULT GETDATE(),
    TongTienSan DECIMAL(10,2) NOT NULL DEFAULT 0, -- Tổng tiền thuê sân
    TongTienDichVu DECIMAL(10,2) NOT NULL DEFAULT 0, -- Tổng tiền dịch vụ
    GiamGia DECIMAL(10,2) DEFAULT 0, -- Số tiền giảm giá
    ThueVat DECIMAL(10,2) DEFAULT 0, -- Thuế VAT (nếu có)
    TongCong DECIMAL(10,2) NOT NULL, -- Tổng cộng phải thanh toán
    TrangThai NVARCHAR(50) DEFAULT N'Chưa thanh toán', -- Chưa thanh toán, Đã thanh toán, Đã hủy
    GhiChu NVARCHAR(500),
    MaNV INT FOREIGN KEY REFERENCES NhanVien(MaNV) -- Nhân viên lập hóa đơn
);

CREATE TABLE ChiTietHoaDon (
    MaChiTietHD INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT FOREIGN KEY REFERENCES HoaDon(MaHoaDon),
    LoaiMuc NVARCHAR(50) NOT NULL, -- "ThueSan", "DichVu"
    TenMuc NVARCHAR(200) NOT NULL, -- Tên sân hoặc tên dịch vụ
    SoLuong INT NOT NULL DEFAULT 1,
    DonGia DECIMAL(10,2) NOT NULL,
    ThanhTien DECIMAL(10,2) NOT NULL,
    GhiChu NVARCHAR(255)
);


CREATE TABLE DonHang (
    MaDonHang INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHangCode NVARCHAR(50) NOT NULL, -- Mã đơn hàng hiển thị (VD: DH20231223001)
    MaKH INT FOREIGN KEY REFERENCES KhachHang(MaKH),
    MaDatSan INT FOREIGN KEY REFERENCES DatSan(MaDatSan), -- Liên kết với đặt sân (nếu có)
    NgayDat DATETIME DEFAULT GETDATE(),
    TongTien DECIMAL(10,2) NOT NULL DEFAULT 0,
    GiamGia DECIMAL(10,2) DEFAULT 0,
    ThanhToan DECIMAL(10,2) NOT NULL DEFAULT 0, -- Số tiền phải thanh toán
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xác nhận', -- Chờ xác nhận, Đã xác nhận, Đang xử lý, Hoàn thành, Đã hủy
    GhiChu NVARCHAR(500),
    MaNV INT FOREIGN KEY REFERENCES NhanVien(MaNV) -- Nhân viên xử lý
);


CREATE TABLE ChiTietDonHang (
    MaChiTietDH INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang INT FOREIGN KEY REFERENCES DonHang(MaDonHang),
    MaDV INT FOREIGN KEY REFERENCES DichVu(MaDV), -- Mã dịch vụ/sản phẩm
    TenSanPham NVARCHAR(200) NOT NULL,
    SoLuong INT NOT NULL DEFAULT 1,
    DonGia DECIMAL(10,2) NOT NULL,
    ThanhTien DECIMAL(10,2) NOT NULL,
    GhiChu NVARCHAR(255)
);


ALTER TABLE ThanhToan
ADD MaHoaDon INT FOREIGN KEY REFERENCES HoaDon(MaHoaDon),
    MaGiaoDich NVARCHAR(100), -- Mã giao dịch thanh toán online
    GhiChu NVARCHAR(500);

CREATE TABLE GiaoDichThanhToan (
    MaGiaoDich INT IDENTITY(1,1) PRIMARY KEY,
    MaThanhToan INT FOREIGN KEY REFERENCES ThanhToan(MaTT),
    MaGiaoDichCode NVARCHAR(100) NOT NULL, -- Mã giao dịch nội bộ
    MaGiaoDichDoiTac NVARCHAR(100), -- Mã giao dịch từ đối tác (MoMo, ZaloPay)
    NhaCungCap NVARCHAR(50) NOT NULL, -- "MoMo", "ZaloPay", "VNPay", "Banking"
    SoTien DECIMAL(10,2) NOT NULL,
    MoTa NVARCHAR(500),
    ThoiGianTao DATETIME DEFAULT GETDATE(),
    ThoiGianCapNhat DATETIME,
    TrangThai NVARCHAR(50) DEFAULT 'Pending', -- "Pending", "Success", "Failed", "Cancelled", "Refunded"
    RequestData NVARCHAR(MAX), -- JSON data gửi đi
    ResponseData NVARCHAR(MAX), -- JSON data nhận về
    ErrorMessage NVARCHAR(500), -- Thông báo lỗi (nếu có)
    CallbackUrl NVARCHAR(500), -- URL callback
    ReturnUrl NVARCHAR(500), -- URL redirect sau khi thanh toán
    IpAddress NVARCHAR(50) -- IP của khách hàng
);

-- =============================================
-- INDEX ĐỂ TỐI ƯU TRUY VẤN
-- =============================================
CREATE INDEX IX_HoaDon_MaKH ON HoaDon(MaKH);
CREATE INDEX IX_HoaDon_MaDatSan ON HoaDon(MaDatSan);
CREATE INDEX IX_HoaDon_NgayLap ON HoaDon(NgayLap);
CREATE INDEX IX_HoaDon_TrangThai ON HoaDon(TrangThai);

CREATE INDEX IX_DonHang_MaKH ON DonHang(MaKH);
CREATE INDEX IX_DonHang_MaDatSan ON DonHang(MaDatSan);
CREATE INDEX IX_DonHang_NgayDat ON DonHang(NgayDat);
CREATE INDEX IX_DonHang_TrangThai ON DonHang(TrangThai);

CREATE INDEX IX_GiaoDichThanhToan_MaThanhToan ON GiaoDichThanhToan(MaThanhToan);
CREATE INDEX IX_GiaoDichThanhToan_MaGiaoDichCode ON GiaoDichThanhToan(MaGiaoDichCode);
CREATE INDEX IX_GiaoDichThanhToan_NhaCungCap ON GiaoDichThanhToan(NhaCungCap);
CREATE INDEX IX_GiaoDichThanhToan_TrangThai ON GiaoDichThanhToan(TrangThai);


CREATE FUNCTION fn_TaoMaHoaDon()
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @MaHD NVARCHAR(50);
    DECLARE @SoThuTu INT;
    
    SELECT @SoThuTu = COUNT(*) + 1 
    FROM HoaDon 
    WHERE CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE);
    
    SET @MaHD = 'HD' + FORMAT(GETDATE(), 'yyyyMMdd') + RIGHT('000' + CAST(@SoThuTu AS NVARCHAR), 3);
    
    RETURN @MaHD;
END;
GO


CREATE FUNCTION fn_TaoMaDonHang()
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @MaDH NVARCHAR(50);
    DECLARE @SoThuTu INT;
    
    SELECT @SoThuTu = COUNT(*) + 1 
    FROM DonHang 
    WHERE CAST(NgayDat AS DATE) = CAST(GETDATE() AS DATE);
    
    SET @MaDH = 'DH' + FORMAT(GETDATE(), 'yyyyMMdd') + RIGHT('000' + CAST(@SoThuTu AS NVARCHAR), 3);
    
    RETURN @MaDH;
END;
GO

CREATE PROCEDURE sp_TaoHoaDon
    @MaDatSan INT,
    @MaNV INT = NULL
AS
BEGIN
    DECLARE @MaHoaDon INT;
    DECLARE @MaKH INT;
    DECLARE @TongTienSan DECIMAL(10,2);
    DECLARE @TongTienDichVu DECIMAL(10,2);
    DECLARE @TongCong DECIMAL(10,2);
    DECLARE @MaHoaDonCode NVARCHAR(50);
    
    -- Lấy thông tin đặt sân
    SELECT @MaKH = MaKH, @TongTienSan = ISNULL(TongTien, 0)
    FROM DatSan 
    WHERE MaDatSan = @MaDatSan;
    
    -- Tính tổng tiền dịch vụ
    SELECT @TongTienDichVu = ISNULL(SUM(ThanhTien), 0)
    FROM ChiTietDichVu
    WHERE MaDatSan = @MaDatSan;
    
    SET @TongCong = @TongTienSan + @TongTienDichVu;
    SET @MaHoaDonCode = dbo.fn_TaoMaHoaDon();
    
    -- Tạo hóa đơn
    INSERT INTO HoaDon (MaKH, MaDatSan, MaHoaDonCode, TongTienSan, TongTienDichVu, TongCong, MaNV)
    VALUES (@MaKH, @MaDatSan, @MaHoaDonCode, @TongTienSan, @TongTienDichVu, @TongCong, @MaNV);
    
    SET @MaHoaDon = SCOPE_IDENTITY();
    
    -- Thêm chi tiết hóa đơn - Thuê sân
    INSERT INTO ChiTietHoaDon (MaHoaDon, LoaiMuc, TenMuc, SoLuong, DonGia, ThanhTien)
    SELECT @MaHoaDon, N'ThueSan', 
           s.TenSan + N' - ' + FORMAT(d.NgaySD, 'dd/MM/yyyy') + N' - ' + 
           FORMAT(k.GioBatDau, 'hh\:mm') + N' đến ' + FORMAT(k.GioKetThuc, 'hh\:mm'),
           1, @TongTienSan, @TongTienSan
    FROM DatSan d
    JOIN SanBong s ON d.MaSan = s.MaSan
    JOIN KhungGio k ON d.MaKhungGio = k.MaKhungGio
    WHERE d.MaDatSan = @MaDatSan;
    
    -- Thêm chi tiết hóa đơn - Dịch vụ
    INSERT INTO ChiTietHoaDon (MaHoaDon, LoaiMuc, TenMuc, SoLuong, DonGia, ThanhTien)
    SELECT @MaHoaDon, N'DichVu', dv.TenDV, ct.SoLuong, ct.DonGia, ct.ThanhTien
    FROM ChiTietDichVu ct
    JOIN DichVu dv ON ct.MaDV = dv.MaDV
    WHERE ct.MaDatSan = @MaDatSan;
    
    SELECT @MaHoaDon AS MaHoaDon, @MaHoaDonCode AS MaHoaDonCode;
END;
GO

CREATE PROCEDURE sp_TaoDonHang
    @MaKH INT,
    @MaDatSan INT = NULL,
    @GhiChu NVARCHAR(500) = NULL
AS
BEGIN
    DECLARE @MaDonHang INT;
    DECLARE @MaDonHangCode NVARCHAR(50);
    
    SET @MaDonHangCode = dbo.fn_TaoMaDonHang();
    
    INSERT INTO DonHang (MaDonHangCode, MaKH, MaDatSan, GhiChu)
    VALUES (@MaDonHangCode, @MaKH, @MaDatSan, @GhiChu);
    
    SET @MaDonHang = SCOPE_IDENTITY();
    
    SELECT @MaDonHang AS MaDonHang, @MaDonHangCode AS MaDonHangCode;
END;
GO


CREATE PROCEDURE sp_ThemSanPhamVaoDonHang
    @MaDonHang INT,
    @MaDV INT,
    @SoLuong INT
AS
BEGIN
    DECLARE @DonGia DECIMAL(10,2);
    DECLARE @ThanhTien DECIMAL(10,2);
    DECLARE @TenSanPham NVARCHAR(200);
    
    SELECT @DonGia = DonGia, @TenSanPham = TenDV 
    FROM DichVu 
    WHERE MaDV = @MaDV;
    
    SET @ThanhTien = @DonGia * @SoLuong;
    
    INSERT INTO ChiTietDonHang (MaDonHang, MaDV, TenSanPham, SoLuong, DonGia, ThanhTien)
    VALUES (@MaDonHang, @MaDV, @TenSanPham, @SoLuong, @DonGia, @ThanhTien);
    
    -- Cập nhật tổng tiền đơn hàng
    UPDATE DonHang
    SET TongTien = (SELECT SUM(ThanhTien) FROM ChiTietDonHang WHERE MaDonHang = @MaDonHang),
        ThanhToan = (SELECT SUM(ThanhTien) FROM ChiTietDonHang WHERE MaDonHang = @MaDonHang) - GiamGia
    WHERE MaDonHang = @MaDonHang;
END;
GO


CREATE PROCEDURE sp_TaoThanhToanOnline
    @MaDatSan INT,
    @MaHoaDon INT = NULL,
    @PhuongThuc NVARCHAR(50),
    @SoTien DECIMAL(10,2),
    @MaGiaoDichCode NVARCHAR(100)
AS
BEGIN
    DECLARE @MaTT INT;
    
    INSERT INTO ThanhToan (MaDatSan, MaHoaDon, PhuongThuc, SoTien, TrangThai, MaGiaoDich)
    VALUES (@MaDatSan, @MaHoaDon, @PhuongThuc, @SoTien, N'ChoDuyet', @MaGiaoDichCode);
    
    SET @MaTT = SCOPE_IDENTITY();
    
    -- Tạo giao dịch thanh toán online
    INSERT INTO GiaoDichThanhToan (MaThanhToan, MaGiaoDichCode, NhaCungCap, SoTien, TrangThai)
    VALUES (@MaTT, @MaGiaoDichCode, @PhuongThuc, @SoTien, 'Pending');
    
    SELECT @MaTT AS MaThanhToan;
END;
GO


CREATE PROCEDURE sp_CapNhatThanhToanOnline
    @MaGiaoDichCode NVARCHAR(100),
    @MaGiaoDichDoiTac NVARCHAR(100),
    @TrangThai NVARCHAR(50),
    @ResponseData NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(500) = NULL
AS
BEGIN
    -- Cập nhật giao dịch thanh toán
    UPDATE GiaoDichThanhToan
    SET MaGiaoDichDoiTac = @MaGiaoDichDoiTac,
        TrangThai = @TrangThai,
        ThoiGianCapNhat = GETDATE(),
        ResponseData = @ResponseData,
        ErrorMessage = @ErrorMessage
    WHERE MaGiaoDichCode = @MaGiaoDichCode;
    
    -- Cập nhật bảng thanh toán
    DECLARE @MaTT INT;
    SELECT @MaTT = MaThanhToan FROM GiaoDichThanhToan WHERE MaGiaoDichCode = @MaGiaoDichCode;
    
    IF @TrangThai = 'Success'
    BEGIN
        UPDATE ThanhToan
        SET TrangThai = N'DaThanhToan',
            NgayThanhToan = GETDATE()
        WHERE MaTT = @MaTT;
        
        -- Cập nhật trạng thái đặt sân
        UPDATE d
        SET TrangThai = N'Đã xác nhận'
        FROM DatSan d
        JOIN ThanhToan t ON d.MaDatSan = t.MaDatSan
        WHERE t.MaTT = @MaTT;
        
        -- Cập nhật trạng thái hóa đơn
        UPDATE hd
        SET TrangThai = N'Đã thanh toán'
        FROM HoaDon hd
        JOIN ThanhToan t ON hd.MaHoaDon = t.MaHoaDon
        WHERE t.MaTT = @MaTT;
    END
    ELSE IF @TrangThai = 'Failed'
    BEGIN
        UPDATE ThanhToan
        SET TrangThai = N'ThatBai'
        WHERE MaTT = @MaTT;
    END
    ELSE IF @TrangThai = 'Cancelled'
    BEGIN
        UPDATE ThanhToan
        SET TrangThai = N'DaHuy'
        WHERE MaTT = @MaTT;
    END
END;
GO


CREATE VIEW vw_ThongKeThanhToanOnline AS
SELECT 
    CAST(ThoiGianTao AS DATE) AS Ngay,
    NhaCungCap,
    COUNT(*) AS SoGiaoDich,
    SUM(CASE WHEN TrangThai = 'Success' THEN 1 ELSE 0 END) AS GiaoDichThanhCong,
    SUM(CASE WHEN TrangThai = 'Failed' THEN 1 ELSE 0 END) AS GiaoDichThatBai,
    SUM(CASE WHEN TrangThai = 'Cancelled' THEN 1 ELSE 0 END) AS GiaoDichHuy,
    SUM(CASE WHEN TrangThai = 'Success' THEN SoTien ELSE 0 END) AS TongTienThanhCong
FROM GiaoDichThanhToan
GROUP BY CAST(ThoiGianTao AS DATE), NhaCungCap;
GO

-- =============================================
-- DỮ LIỆU MẪU
-- =============================================

-- Hóa đơn mẫu
INSERT INTO HoaDon (MaKH, MaDatSan, MaHoaDonCode, TongTienSan, TongTienDichVu, TongCong, TrangThai, MaNV)
VALUES 
(1, 1, 'HD20231220001', 450000, 120000, 570000, N'Đã thanh toán', 1),
(5, 5, 'HD20231221001', 300000, 0, 300000, N'Đã thanh toán', 1);

-- Chi tiết hóa đơn mẫu
INSERT INTO ChiTietHoaDon (MaHoaDon, LoaiMuc, TenMuc, SoLuong, DonGia, ThanhTien)
VALUES 
(1, N'ThueSan', N'Sân A1 - 18:00-20:00', 1, 450000, 450000),
(1, N'DichVu', N'Nước suối Aquafina', 10, 10000, 100000),
(1, N'DichVu', N'Thuê bóng', 1, 20000, 20000),
(2, N'ThueSan', N'Sân A1 - 10:00-12:00', 1, 300000, 300000);

-- Đơn hàng mẫu
INSERT INTO DonHang (MaDonHangCode, MaKH, MaDatSan, TongTien, ThanhToan, TrangThai)
VALUES 
('DH20231220001', 1, 1, 120000, 120000, N'Hoàn thành'),
('DH20231221001', 2, 2, 150000, 150000, N'Chờ xác nhận');

-- Chi tiết đơn hàng mẫu
INSERT INTO ChiTietDonHang (MaDonHang, MaDV, TenSanPham, SoLuong, DonGia, ThanhTien)
VALUES 
(1, 1, N'Nước suối Aquafina', 10, 10000, 100000),
(1, 5, N'Thuê bóng', 1, 20000, 20000),
(2, 2, N'Nước tăng lực Redbull', 6, 25000, 150000);

PRINT N'Đã tạo thành công các bảng mở rộng!';
GO
