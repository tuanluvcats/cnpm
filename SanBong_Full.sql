
CREATE DATABASE QL_DatSanBong;
GO
USE QL_DatSanBong;
GO

-- Bảng Tài khoản
CREATE TABLE IF NOT EXISTS TaiKhoan (
    MaTK INTEGER PRIMARY KEY AUTOINCREMENT,
    TenDangNhap TEXT UNIQUE NOT NULL,
    MatKhau TEXT NOT NULL,
    VaiTro TEXT CHECK (VaiTro IN ('Admin', 'NhanVien', 'KhachHang')) NOT NULL,
    TrangThai INTEGER DEFAULT 1
);

-- Bảng Loại sân
CREATE TABLE IF NOT EXISTS LoaiSan (
    MaLoai INTEGER PRIMARY KEY AUTOINCREMENT,
    TenLoai TEXT NOT NULL, -- Sân 5, Sân 7, Sân 11
    MoTa TEXT
);

-- Bảng Sân bóng
CREATE TABLE IF NOT EXISTS SanBong (
    MaSan INTEGER PRIMARY KEY AUTOINCREMENT,
    TenSan TEXT NOT NULL,
    MaLoai INTEGER,
    GiaTheoGio REAL NOT NULL,
    TrangThai TEXT DEFAULT 'Hoạt động', -- Hoạt động, Bảo trì, Ngưng hoạt động
    ViTri TEXT,
    MoTa TEXT,
    HinhAnh TEXT,
    FOREIGN KEY (MaLoai) REFERENCES LoaiSan(MaLoai)
);

-- Bảng Khung giờ
CREATE TABLE IF NOT EXISTS KhungGio (
    MaKhungGio INTEGER PRIMARY KEY AUTOINCREMENT,
    GioBatDau TEXT NOT NULL,
    GioKetThuc TEXT NOT NULL,
    HeSoGia REAL DEFAULT 1.0 -- Hệ số giá (giờ vàng x1.5, giờ thường x1.0)
);

-- Bảng Nhân viên
CREATE TABLE IF NOT EXISTS NhanVien (
    MaNV INTEGER PRIMARY KEY AUTOINCREMENT,
    HoTen TEXT NOT NULL,
    NgaySinh TEXT,
    GioiTinh TEXT,
    DienThoai TEXT,
    Email TEXT,
    ChucVu TEXT,
    MaTK INTEGER,
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);

-- Bảng Khách hàng
CREATE TABLE IF NOT EXISTS KhachHang (
    MaKH INTEGER PRIMARY KEY AUTOINCREMENT,
    HoTen TEXT NOT NULL,
    DiaChi TEXT,
    DienThoai TEXT NOT NULL,
    Email TEXT,
    CCCD TEXT,
    DiemTichLuy INTEGER DEFAULT 0,
    MaTK INTEGER,
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);


CREATE TABLE IF NOT EXISTS NgayLe (
    MaNgayLe INTEGER PRIMARY KEY AUTOINCREMENT,
    TenNgayLe TEXT NOT NULL,
    NgayBatDau TEXT NOT NULL, -- Format: MM-DD hoặc YYYY-MM-DD cho âm lịch
    NgayKetThuc TEXT, -- Null nếu chỉ 1 ngày
    LoaiLich TEXT DEFAULT 'DuongLich', -- DuongLich, AmLich
    HeSoGiamGia REAL DEFAULT 0.6, -- 0.6 = giảm 40%
    MoTa TEXT,
    TrangThai INTEGER DEFAULT 1 -- 1: Hoạt động, 0: Không hoạt động
);

-- Bảng Đặt sân 
CREATE TABLE IF NOT EXISTS DatSan (
    MaDatSan INTEGER PRIMARY KEY AUTOINCREMENT,
    MaKH INTEGER,
    MaSan INTEGER,
    MaKhungGio INTEGER,
    NgayDat TEXT NOT NULL,
    NgaySD TEXT NOT NULL, -- Ngày sử dụng sân
    ThoiGianDat TEXT DEFAULT (datetime('now')),
    GiaGoc REAL, -- Giá gốc (chưa giảm)
    GiamGiaNgayLe REAL DEFAULT 0, -- Số tiền được giảm do ngày lễ
    TongTien REAL, -- Giá sau khi áp dụng giảm giá
    TrangThai TEXT DEFAULT 'Chờ xác nhận', -- Chờ xác nhận, Đã xác nhận, Đang sử dụng, Hoàn tất, Đã hủy
    GhiChu TEXT,
    MaNV INTEGER, -- Nhân viên xác nhận
    MaNgayLe INTEGER, -- Ngày lễ được áp dụng (nếu có)
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    FOREIGN KEY (MaSan) REFERENCES SanBong(MaSan),
    FOREIGN KEY (MaKhungGio) REFERENCES KhungGio(MaKhungGio),
    FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    FOREIGN KEY (MaNgayLe) REFERENCES NgayLe(MaNgayLe)
);

-- Bảng Dịch vụ thêm 
CREATE TABLE IF NOT EXISTS DichVu (
    MaDV INTEGER PRIMARY KEY AUTOINCREMENT,
    TenDV TEXT NOT NULL,
    DonGia REAL NOT NULL,
    DonVi TEXT, -- chai, đôi, bộ
    MoTa TEXT,
    HinhAnh TEXT,
    SoLuongTon INTEGER DEFAULT 0
);

-- Bảng Chi tiết đặt sân - Dịch vụ
CREATE TABLE IF NOT EXISTS ChiTietDichVu (
    MaCTDV INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDatSan INTEGER,
    MaDV INTEGER,
    SoLuong INTEGER NOT NULL,
    DonGia REAL NOT NULL,
    ThanhTien REAL,
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan),
    FOREIGN KEY (MaDV) REFERENCES DichVu(MaDV)
);

-- Bảng Thanh toán
CREATE TABLE IF NOT EXISTS ThanhToan (
    MaTT INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDatSan INTEGER,
    MaHoaDon INTEGER,
    PhuongThuc TEXT NOT NULL, -- Tiền mặt, Chuyển khoản, Ví điện tử
    SoTien REAL NOT NULL,
    NgayThanhToan TEXT DEFAULT (datetime('now')),
    TrangThai TEXT DEFAULT 'Đã thanh toán',
    MaGiaoDich TEXT,
    GhiChu TEXT,
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan),
    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon)
);

-- Bảng Đánh giá
CREATE TABLE IF NOT EXISTS DanhGia (
    MaDanhGia INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDatSan INTEGER,
    MaKH INTEGER,
    DiemDanhGia INTEGER CHECK (DiemDanhGia BETWEEN 1 AND 5),
    NoiDung TEXT,
    NgayDanhGia TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan),
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH)
);

-- Bảng Liên hệ
CREATE TABLE IF NOT EXISTS LienHe (
    MaLienHe INTEGER PRIMARY KEY AUTOINCREMENT,
    HoTen TEXT NOT NULL,
    Email TEXT NOT NULL,
    SoDienThoai TEXT,
    TieuDe TEXT,
    NoiDung TEXT NOT NULL,
    NgayGui TEXT DEFAULT (datetime('now')),
    TrangThai TEXT DEFAULT 'Chưa xử lý'
);

-- Bảng Ca làm việc
CREATE TABLE IF NOT EXISTS CaLam (
    MaCa INTEGER PRIMARY KEY AUTOINCREMENT,
    TenCa TEXT NOT NULL,
    GioBatDau TEXT NOT NULL,
    GioKetThuc TEXT NOT NULL,
    MoTa TEXT
);

-- Bảng Phân ca làm việc
CREATE TABLE IF NOT EXISTS PhanCa (
    MaPhanCa INTEGER PRIMARY KEY AUTOINCREMENT,
    MaNV INTEGER,
    MaCa INTEGER,
    NgayLam TEXT NOT NULL,
    TrangThai TEXT DEFAULT 'Đang chờ', -- Đi làm, Đang chờ, Hoàn thành, Nghỉ có phép, Vắng mặt
    GhiChu TEXT,
    FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    FOREIGN KEY (MaCa) REFERENCES CaLam(MaCa)
);


-- Bảng Hóa Đơn
CREATE TABLE IF NOT EXISTS HoaDon (
    MaHoaDon INTEGER PRIMARY KEY AUTOINCREMENT,
    MaKH INTEGER,
    MaNV INTEGER,
    MaDatSan INTEGER,
    MaHoaDonCode TEXT,
    NgayLap TEXT DEFAULT (datetime('now')),
    TongTienSan REAL DEFAULT 0,
    TongTienDichVu REAL DEFAULT 0,
    GiamGia REAL DEFAULT 0,
    ThueVat REAL DEFAULT 0,
    TongCong REAL,
    TrangThai TEXT DEFAULT 'ChuaThanhToan',
    GhiChu TEXT,
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

-- Bảng Chi Tiết Hóa Đơn
CREATE TABLE IF NOT EXISTS ChiTietHoaDon (
    MaChiTiet INTEGER PRIMARY KEY AUTOINCREMENT,
    MaHoaDon INTEGER NOT NULL,
    LoaiChiTiet TEXT NOT NULL,
    MaDichVu INTEGER,
    TenMuc TEXT,
    MoTa TEXT,
    SoLuong INTEGER DEFAULT 1,
    DonGia REAL,
    ThanhTien REAL,
    GhiChu TEXT,
    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDV)
);

-- Bảng Đơn Hàng (Dịch vụ phụ)
CREATE TABLE IF NOT EXISTS DonHang (
    MaDonHang INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDonHangCode TEXT,
    MaKH INTEGER NOT NULL,
    MaNV INTEGER,
    MaDatSan INTEGER,
    NgayDat TEXT DEFAULT (datetime('now')),
    TongTien REAL DEFAULT 0,
    GiamGia REAL DEFAULT 0,
    ThanhToan REAL DEFAULT 0,
    TrangThai TEXT DEFAULT 'ChoXuLy',
    GhiChu TEXT,
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

-- Bảng Chi Tiết Đơn Hàng
CREATE TABLE IF NOT EXISTS ChiTietDonHang (
    MaChiTietDH INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDonHang INTEGER NOT NULL,
    MaDV INTEGER,
    MaDichVu INTEGER NOT NULL,
    TenSanPham TEXT,
    SoLuong INTEGER DEFAULT 1,
    DonGia REAL,
    ThanhTien REAL,
    GhiChu TEXT,
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDV)
);

-- Bảng Giao Dịch Thanh Toán 
CREATE TABLE IF NOT EXISTS GiaoDichThanhToan (
    MaGiaoDich INTEGER PRIMARY KEY AUTOINCREMENT,
    MaThanhToan INTEGER NOT NULL,
    MaGiaoDichCode TEXT NOT NULL,
    MaGiaoDichDoiTac TEXT,
    NhaCungCap TEXT NOT NULL,
    SoTien REAL,
    MoTa TEXT,
    ThoiGianTao TEXT DEFAULT (datetime('now')),
    ThoiGianCapNhat TEXT,
    TrangThai TEXT DEFAULT 'Pending',
    MaTraVe TEXT,
    ThongBao TEXT,
    RequestData TEXT,
    ResponseData TEXT,
    DuLieuPhanHoi TEXT,
    ErrorMessage TEXT,
    CallbackUrl TEXT,
    ReturnUrl TEXT,
    IpAddress TEXT,
    FOREIGN KEY (MaThanhToan) REFERENCES ThanhToan(MaTT)
);

-- Bảng Khóa Sân
CREATE TABLE IF NOT EXISTS KhoaSan (
    MaKhoaSan INTEGER PRIMARY KEY AUTOINCREMENT,
    MaSan INTEGER NOT NULL,
    NgaySd TEXT NOT NULL,
    MaKhungGio INTEGER NOT NULL,
    MaKH INTEGER,
    MaDatSan INTEGER,
    SessionId TEXT NOT NULL,
    ThoiGianKhoa TEXT DEFAULT (datetime('now')),
    ThoiGianHetHan TEXT NOT NULL,
    TrangThai TEXT DEFAULT 'DangGiu',
    FOREIGN KEY (MaSan) REFERENCES SanBong(MaSan),
    FOREIGN KEY (MaKhungGio) REFERENCES KhungGio(MaKhungGio),
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

--CÁC INDEX TỐI ƯU TRUY VẤN

CREATE INDEX IF NOT EXISTS idx_hoadon_makh ON HoaDon(MaKH);
CREATE INDEX IF NOT EXISTS idx_hoadon_madatsan ON HoaDon(MaDatSan);
CREATE INDEX IF NOT EXISTS idx_hoadon_ngaylap ON HoaDon(NgayLap);
CREATE INDEX IF NOT EXISTS idx_hoadon_trangthai ON HoaDon(TrangThai);
CREATE INDEX IF NOT EXISTS idx_chitiethoadon_mahoadon ON ChiTietHoaDon(MaHoaDon);
CREATE INDEX IF NOT EXISTS idx_donhang_makh ON DonHang(MaKH);
CREATE INDEX IF NOT EXISTS idx_donhang_madatsan ON DonHang(MaDatSan);
CREATE INDEX IF NOT EXISTS idx_donhang_ngaydat ON DonHang(NgayDat);
CREATE INDEX IF NOT EXISTS idx_donhang_trangthai ON DonHang(TrangThai);
CREATE INDEX IF NOT EXISTS idx_chitietdonhang_madonhang ON ChiTietDonHang(MaDonHang);
CREATE INDEX IF NOT EXISTS idx_giaodich_mathanhtoan ON GiaoDichThanhToan(MaThanhToan);
CREATE INDEX IF NOT EXISTS idx_giaodich_magiaodichcode ON GiaoDichThanhToan(MaGiaoDichCode);
CREATE INDEX IF NOT EXISTS idx_giaodich_nhacungcap ON GiaoDichThanhToan(NhaCungCap);
CREATE INDEX IF NOT EXISTS idx_giaodich_trangthai ON GiaoDichThanhToan(TrangThai);
CREATE INDEX IF NOT EXISTS idx_khoasan_lookup ON KhoaSan(MaSan, NgaySd, MaKhungGio);
CREATE INDEX IF NOT EXISTS idx_khoasan_trangthai ON KhoaSan(TrangThai);
CREATE INDEX IF NOT EXISTS idx_datsan_ngaysd ON DatSan(NgaySD);
CREATE INDEX IF NOT EXISTS idx_ngayle_ngaybatdau ON NgayLe(NgayBatDau);


-- Tài khoản Admin
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES
('admin', 'admin123', 'Admin'),
('admin2', 'admin456', 'Admin');

-- Tài khoản Nhân viên
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES
('nv.nam', 'nv123', 'NhanVien'),
('nv.hoa', 'nv123', 'NhanVien');

-- Tài khoản Khách hàng
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES
('kh.an', 'kh123', 'KhachHang'),
('kh.binh', 'kh123', 'KhachHang'),
('kh.cuong', 'kh123', 'KhachHang'),
('kh.dung', 'kh123', 'KhachHang'),
('kh.em', 'kh123', 'KhachHang');

-- Loại sân
INSERT INTO LoaiSan (TenLoai, MoTa) VALUES 
('Sân 5 người', 'Sân bóng đá mini 5 người'),
('Sân 7 người', 'Sân bóng đá 7 người'),
('Sân 11 người', 'Sân bóng đá 11 người tiêu chuẩn');

-- Sân bóng
INSERT INTO SanBong (TenSan, MaLoai, GiaTheoGio, TrangThai, ViTri, MoTa, HinhAnh) VALUES
('Sân A1', 1, 300000, 'Hoạt động', 'Khu A - Tầng 1', 'Sân 5 người có mái che, cỏ nhân tạo cao cấp', 'san_a1.jpg'),
('Sân A2', 1, 300000, 'Hoạt động', 'Khu A - Tầng 1', 'Sân 5 người có mái che', 'san_a2.jpg'),
('Sân B1', 2, 500000, 'Hoạt động', 'Khu B - Tầng 2', 'Sân 7 người sân cỏ nhân tạo', 'san_b1.jpg'),
('Sân B2', 2, 500000, 'Hoạt động', 'Khu B - Tầng 2', 'Sân 7 người có đèn chiếu sáng', 'san_b2.jpg'),
('Sân C1', 3, 1000000, 'Hoạt động', 'Khu C - Sân ngoài', 'Sân 11 người tiêu chuẩn FIFA', 'san_c1.jpg'),
('Sân VIP', 1, 400000, 'Hoạt động', 'Khu VIP', 'Sân 5 người VIP có điều hòa', 'san_vip.jpg');

-- Khung giờ
INSERT INTO KhungGio (GioBatDau, GioKetThuc, HeSoGia) VALUES
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
INSERT INTO NhanVien (HoTen, NgaySinh, GioiTinh, DienThoai, Email, ChucVu, MaTK) VALUES 
('Nguyễn Văn Nam', '1990-04-12', 'Nam', '0909000111', 'namnv@sanbong.vn', 'Quản lý', 3),
('Trần Thị Hoa', '1995-08-25', 'Nữ', '0909111222', 'hoatt@sanbong.vn', 'Nhân viên lễ tân', 4);

-- Ca làm việc
INSERT INTO CaLam (TenCa, GioBatDau, GioKetThuc, MoTa) VALUES
('Ca sáng', '06:00', '12:00', 'Ca làm việc buổi sáng'),
('Ca chiều', '12:00', '18:00', 'Ca làm việc buổi chiều'),
('Ca tối', '18:00', '23:00', 'Ca làm việc buổi tối');

-- Khách hàng
INSERT INTO KhachHang (HoTen, DiaChi, DienThoai, Email, CCCD, DiemTichLuy, MaTK) VALUES 
('Lê Văn An', 'Quận 1, TP.HCM', '0905111222', 'levan.an@gmail.com', '001234567890', 50, 5),
('Phạm Minh Bình', 'Quận 3, TP.HCM', '0905222333', 'pham.binh@gmail.com', '001234567891', 120, 6),
('Hoàng Văn Cường', 'Quận 5, TP.HCM', '0905333444', 'hoang.cuong@gmail.com', '001234567892', 80, 7),
('Trần Thị Dung', 'Thủ Đức, TP.HCM', '0905444555', 'tran.dung@gmail.com', '001234567893', 30, 8),
('Nguyễn Thị Em', 'Bình Thạnh, TP.HCM', '0905555666', 'nguyen.em@gmail.com', '001234567894', 100, 9);

-- Dịch vụ thêm
INSERT INTO DichVu (TenDV, DonGia, DonVi, MoTa, HinhAnh, SoLuongTon) VALUES
('Nước suối Aquafina', 10000, 'chai', 'Nước khoáng tinh khiết', 'aqua.jpg', 200),
('Nước tăng lực Redbull', 25000, 'lon', 'Nước tăng lực', 'redbull.jpg', 100),
('Thuê giày đá bóng', 30000, 'đôi/trận', 'Giày đá bóng chuyên dụng', 'giay.jpg', 50),
('Thuê áo đấu', 50000, 'bộ/trận', 'Áo đấu theo đội, đủ size', 'ao.jpg', 30),
('Thuê bóng', 20000, 'quả/trận', 'Bóng đá size 5 tiêu chuẩn', 'bong.jpg', 40),
('Khăn lạnh', 5000, 'chiếc', 'Khăn lạnh làm mát', 'khan.jpg', 150);



-- Ngày lễ Dương lịch 
INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
-- Tết Dương lịch
('Tết Dương lịch', '01-01', '01-01', 'DuongLich', 0.6, 'Ngày đầu năm mới Dương lịch', 1),

-- Valentine
('Lễ Valentine', '02-14', '02-14', 'DuongLich', 0.6, 'Ngày lễ tình nhân', 1),

-- Ngày Quốc tế Phụ nữ
('Ngày Quốc tế Phụ nữ', '03-08', '03-08', 'DuongLich', 0.6, 'Ngày Quốc tế Phụ nữ 8/3', 1),

-- Giỗ Tổ Hùng Vương
('Giỗ Tổ Hùng Vương 2025', '2025-04-07', '2025-04-07', 'DuongLich', 0.6, 'Giỗ Tổ Hùng Vương - 10/3 Âm lịch năm 2025', 1),
('Giỗ Tổ Hùng Vương 2026', '2026-04-26', '2026-04-26', 'DuongLich', 0.6, 'Giỗ Tổ Hùng Vương - 10/3 Âm lịch năm 2026', 1),

-- Ngày Giải phóng miền Nam
('Ngày Giải phóng miền Nam', '04-30', '04-30', 'DuongLich', 0.6, 'Ngày Giải phóng miền Nam 30/4', 1),

-- Ngày Quốc tế Lao động
('Ngày Quốc tế Lao động', '05-01', '05-01', 'DuongLich', 0.6, 'Ngày Quốc tế Lao động 1/5', 1),

-- Ngày Quốc khánh
('Ngày Quốc khánh', '09-02', '09-02', 'DuongLich', 0.6, 'Ngày Quốc khánh Việt Nam 2/9', 1),

-- Halloween
('Lễ Halloween', '10-31', '10-31', 'DuongLich', 0.6, 'Lễ hội Halloween', 1),

-- Ngày Nhà giáo Việt Nam
('Ngày Nhà giáo Việt Nam', '11-20', '11-20', 'DuongLich', 0.6, 'Ngày Nhà giáo Việt Nam 20/11', 1),

-- Giáng sinh (Noel)
('Lễ Giáng sinh', '12-24', '12-25', 'DuongLich', 0.6, 'Lễ Giáng sinh - Christmas Eve và Christmas', 1),

-- Ngày Phụ nữ Việt Nam
('Ngày Phụ nữ Việt Nam', '10-20', '10-20', 'DuongLich', 0.6, 'Ngày Phụ nữ Việt Nam 20/10', 1),

-- Ngày Quân đội Nhân dân Việt Nam
('Ngày Quân đội Nhân dân', '12-22', '12-22', 'DuongLich', 0.6, 'Ngày thành lập Quân đội Nhân dân Việt Nam', 1);

-- Tết Nguyên Đán 
INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
('Tết Nguyên Đán 2025', '2025-01-28', '2025-02-04', 'AmLich', 0.6, 'Tết Nguyên Đán Ất Tỵ 2025 (30 Tết - Mùng 7)', 1),
('Tết Nguyên Đán 2026', '2026-02-16', '2026-02-23', 'AmLich', 0.6, 'Tết Nguyên Đán Bính Ngọ 2026 (30 Tết - Mùng 7)', 1),
('Tết Nguyên Đán 2027', '2027-02-05', '2027-02-12', 'AmLich', 0.6, 'Tết Nguyên Đán Đinh Mùi 2027 (30 Tết - Mùng 7)', 1);

-- Rằm Trung Thu 
INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
('Tết Trung Thu 2025', '2025-10-06', '2025-10-06', 'AmLich', 0.6, 'Tết Trung Thu - Rằm tháng 8 Âm lịch 2025', 1),
('Tết Trung Thu 2026', '2026-09-25', '2026-09-25', 'AmLich', 0.6, 'Tết Trung Thu - Rằm tháng 8 Âm lịch 2026', 1);

-- Ngày Vu Lan 

INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
('Lễ Vu Lan 2025', '2025-09-06', '2025-09-06', 'AmLich', 0.6, 'Lễ Vu Lan Báo Hiếu - Rằm tháng 7 Âm lịch 2025', 1),
('Lễ Vu Lan 2026', '2026-08-27', '2026-08-27', 'AmLich', 0.6, 'Lễ Vu Lan Báo Hiếu - Rằm tháng 7 Âm lịch 2026', 1);

-- Ngày lễ Quốc tế thiếu nhi 
INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
('Ngày Quốc tế Thiếu nhi', '06-01', '06-01', 'DuongLich', 0.6, 'Ngày Quốc tế Thiếu nhi 1/6', 1);

-- Ngày Gia đình Việt Nam 
INSERT INTO NgayLe (TenNgayLe, NgayBatDau, NgayKetThuc, LoaiLich, HeSoGiamGia, MoTa, TrangThai) VALUES
('Ngày Gia đình Việt Nam', '06-28', '06-28', 'DuongLich', 0.6, 'Ngày Gia đình Việt Nam 28/6', 1);

-- DỮ LIỆU MẪU ĐẶT SÂN

INSERT INTO DatSan (MaKH, MaSan, MaKhungGio, NgayDat, NgaySD, GiaGoc, GiamGiaNgayLe, TongTien, TrangThai, GhiChu, MaNV) VALUES 
(1, 1, 7, date('now'), date('now', '+2 days'), 450000, 0, 450000, 'Đã xác nhận', 'Khách quen', 1),
(2, 2, 8, date('now'), date('now', '+3 days'), 450000, 0, 450000, 'Chờ xác nhận', NULL, NULL),
(3, 3, 6, date('now'), date('now', '+1 day'), 600000, 0, 600000, 'Đã xác nhận', 'Đặt cho công ty', 2),
(4, 5, 7, date('now'), date('now', '+5 days'), 1500000, 0, 1500000, 'Đã xác nhận', 'Giải đấu công ty', 1),
(5, 1, 3, date('now', '-1 day'), date('now'), 300000, 0, 300000, 'Hoàn tất', 'Đã sử dụng', 1);

-- Chi tiết dịch vụ
INSERT INTO ChiTietDichVu (MaDatSan, MaDV, SoLuong, DonGia, ThanhTien) VALUES
(1, 1, 10, 10000, 100000),
(1, 5, 1, 20000, 20000),
(3, 2, 6, 25000, 150000),
(3, 3, 7, 30000, 210000),
(4, 4, 11, 50000, 550000);

-- Thanh toán
INSERT INTO ThanhToan (MaDatSan, PhuongThuc, SoTien, TrangThai) VALUES
(1, 'Chuyển khoản', 570000, 'Đã thanh toán'),
(5, 'Tiền mặt', 300000, 'Đã thanh toán');

-- Đánh giá
INSERT INTO DanhGia (MaDatSan, MaKH, DiemDanhGia, NoiDung) VALUES
(5, 5, 5, 'Sân đẹp, cỏ mượt, nhân viên nhiệt tình'),
(1, 1, 4, 'Sân tốt, giá hợp lý');

-- Hóa đơn mẫu
INSERT INTO HoaDon (MaKH, MaDatSan, MaHoaDonCode, TongTienSan, TongTienDichVu, TongCong, TrangThai, MaNV) VALUES 
(1, 1, 'HD20231220001', 450000, 120000, 570000, 'Đã thanh toán', 1),
(5, 5, 'HD20231221001', 300000, 0, 300000, 'Đã thanh toán', 1);

-- Chi tiết hóa đơn mẫu
INSERT INTO ChiTietHoaDon (MaHoaDon, LoaiChiTiet, TenMuc, SoLuong, DonGia, ThanhTien) VALUES 
(1, 'ThueSan', 'Sân A1 - 18:00-20:00', 1, 450000, 450000),
(1, 'DichVu', 'Nước suối Aquafina', 10, 10000, 100000),
(1, 'DichVu', 'Thuê bóng', 1, 20000, 20000),
(2, 'ThueSan', 'Sân A1 - 10:00-12:00', 1, 300000, 300000);

-- Đơn hàng mẫu
INSERT INTO DonHang (MaDonHangCode, MaKH, MaDatSan, TongTien, ThanhToan, TrangThai) VALUES 
('DH20231220001', 1, 1, 120000, 120000, 'Hoàn thành'),
('DH20231221001', 2, 2, 150000, 150000, 'Chờ xác nhận');

-- Chi tiết đơn hàng mẫu
INSERT INTO ChiTietDonHang (MaDonHang, MaDichVu, TenSanPham, SoLuong, DonGia, ThanhTien) VALUES 
(1, 1, 'Nước suối Aquafina', 10, 10000, 100000),
(1, 5, 'Thuê bóng', 1, 20000, 20000),
(2, 2, 'Nước tăng lực Redbull', 6, 25000, 150000);
