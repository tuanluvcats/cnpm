-- Bảng Hóa Đơn
CREATE TABLE IF NOT EXISTS HoaDon (
    MaHoaDon INTEGER PRIMARY KEY AUTOINCREMENT,
    MaKh INTEGER,
    MaNv INTEGER,
    MaDatSan INTEGER,
    NgayLap TEXT DEFAULT (datetime('now')),
    TongTien REAL,
    GiamGia REAL DEFAULT 0,
    ThanhTien REAL,
    TrangThai TEXT DEFAULT 'ChuaThanhToan',
    GhiChu TEXT,
    FOREIGN KEY (MaKh) REFERENCES KhachHang(MaKh),
    FOREIGN KEY (MaNv) REFERENCES NhanVien(MaNv),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

-- Bảng Chi Tiết Hóa Đơn
CREATE TABLE IF NOT EXISTS ChiTietHoaDon (
    MaChiTiet INTEGER PRIMARY KEY AUTOINCREMENT,
    MaHoaDon INTEGER NOT NULL,
    LoaiChiTiet TEXT NOT NULL,
    MaDichVu INTEGER,
    MoTa TEXT,
    SoLuong INTEGER DEFAULT 1,
    DonGia REAL,
    ThanhTien REAL,
    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDv)
);

-- Bảng Đơn Hàng (Dịch vụ phụ)
CREATE TABLE IF NOT EXISTS DonHang (
    MaDonHang INTEGER PRIMARY KEY AUTOINCREMENT,
    MaKh INTEGER NOT NULL,
    MaNv INTEGER,
    MaDatSan INTEGER,
    NgayDat TEXT DEFAULT (datetime('now')),
    TongTien REAL,
    TrangThai TEXT DEFAULT 'ChoXuLy',
    GhiChu TEXT,
    FOREIGN KEY (MaKh) REFERENCES KhachHang(MaKh),
    FOREIGN KEY (MaNv) REFERENCES NhanVien(MaNv),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

-- Bảng Chi Tiết Đơn Hàng
CREATE TABLE IF NOT EXISTS ChiTietDonHang (
    MaChiTietDH INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDonHang INTEGER NOT NULL,
    MaDichVu INTEGER NOT NULL,
    SoLuong INTEGER DEFAULT 1,
    DonGia REAL,
    ThanhTien REAL,
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDv)
);

-- Bảng Giao Dịch Thanh Toán (Online Payment Transactions)
CREATE TABLE IF NOT EXISTS GiaoDichThanhToan (
    MaGiaoDich INTEGER PRIMARY KEY AUTOINCREMENT,
    MaThanhToan INTEGER NOT NULL,
    MaGiaoDichCode TEXT NOT NULL,
    NhaCungCap TEXT NOT NULL,
    SoTien REAL,
    ThoiGianTao TEXT DEFAULT (datetime('now')),
    ThoiGianCapNhat TEXT,
    TrangThai TEXT DEFAULT 'Pending',
    MaTraVe TEXT,
    ThongBao TEXT,
    DuLieuPhanHoi TEXT,
    FOREIGN KEY (MaThanhToan) REFERENCES ThanhToan(MaTt)
);

-- Bảng Khóa Sân (Field Lock during payment)
CREATE TABLE IF NOT EXISTS KhoaSan (
    MaKhoaSan INTEGER PRIMARY KEY AUTOINCREMENT,
    MaSan INTEGER NOT NULL,
    NgaySd TEXT NOT NULL,
    MaKhungGio INTEGER NOT NULL,
    MaKh INTEGER,
    MaDatSan INTEGER,
    SessionId TEXT NOT NULL,
    ThoiGianKhoa TEXT DEFAULT (datetime('now')),
    ThoiGianHetHan TEXT NOT NULL,
    TrangThai TEXT DEFAULT 'DangGiu',
    FOREIGN KEY (MaSan) REFERENCES SanBong(MaSan),
    FOREIGN KEY (MaKhungGio) REFERENCES KhungGio(MaKhungGio),
    FOREIGN KEY (MaKh) REFERENCES KhachHang(MaKh),
    FOREIGN KEY (MaDatSan) REFERENCES DatSan(MaDatSan)
);

-- Tạo index để tối ưu truy vấn
CREATE INDEX IF NOT EXISTS idx_hoadon_makh ON HoaDon(MaKh);
CREATE INDEX IF NOT EXISTS idx_hoadon_madatsan ON HoaDon(MaDatSan);
CREATE INDEX IF NOT EXISTS idx_chitiethoadon_mahoadon ON ChiTietHoaDon(MaHoaDon);
CREATE INDEX IF NOT EXISTS idx_donhang_makh ON DonHang(MaKh);
CREATE INDEX IF NOT EXISTS idx_donhang_madatsan ON DonHang(MaDatSan);
CREATE INDEX IF NOT EXISTS idx_chitietdonhang_madonhang ON ChiTietDonHang(MaDonHang);
CREATE INDEX IF NOT EXISTS idx_giaodich_mathanhtoan ON GiaoDichThanhToan(MaThanhToan);
CREATE INDEX IF NOT EXISTS idx_khoasan_lookup ON KhoaSan(MaSan, NgaySd, MaKhungGio);
CREATE INDEX IF NOT EXISTS idx_khoasan_trangthai ON KhoaSan(TrangThai);
