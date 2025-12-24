using Microsoft.EntityFrameworkCore;
using SanBong.Models;

namespace SanBong.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Xóa database cũ và tạo lại để có schema mới với CaLam, PhanCa
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Seed CaLam first
        var caLams = new CaLam[]
        {
            new CaLam { TenCa = "Ca sáng", GioBatDau = new TimeSpan(6, 0, 0), GioKetThuc = new TimeSpan(12, 0, 0), MoTa = "Ca làm việc buổi sáng" },
            new CaLam { TenCa = "Ca chiều", GioBatDau = new TimeSpan(12, 0, 0), GioKetThuc = new TimeSpan(18, 0, 0), MoTa = "Ca làm việc buổi chiều" },
            new CaLam { TenCa = "Ca tối", GioBatDau = new TimeSpan(18, 0, 0), GioKetThuc = new TimeSpan(23, 0, 0), MoTa = "Ca làm việc buổi tối" }
        };
        context.CaLam.AddRange(caLams);
        context.SaveChanges();

        // Seed TaiKhoan
        var taiKhoans = new TaiKhoan[]
        {
            new TaiKhoan { TenDangNhap = "admin", MatKhau = "admin123", VaiTro = "Admin", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "admin2", MatKhau = "admin456", VaiTro = "Admin", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "nv.nam", MatKhau = "nv123", VaiTro = "NhanVien", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "nv.hoa", MatKhau = "nv123", VaiTro = "NhanVien", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "kh.an", MatKhau = "kh123", VaiTro = "KhachHang", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "kh.binh", MatKhau = "kh123", VaiTro = "KhachHang", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "kh.cuong", MatKhau = "kh123", VaiTro = "KhachHang", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "kh.dung", MatKhau = "kh123", VaiTro = "KhachHang", TrangThai = 1 },
            new TaiKhoan { TenDangNhap = "kh.em", MatKhau = "kh123", VaiTro = "KhachHang", TrangThai = 1 }
        };
        context.TaiKhoan.AddRange(taiKhoans);
        context.SaveChanges();

        // Seed LoaiSan
        var loaiSans = new LoaiSan[]
        {
            new LoaiSan { TenLoai = "Sân 5 người", MoTa = "Sân bóng đá mini 5 người" },
            new LoaiSan { TenLoai = "Sân 7 người", MoTa = "Sân bóng đá 7 người" },
            new LoaiSan { TenLoai = "Sân 11 người", MoTa = "Sân bóng đá 11 người tiêu chuẩn" }
        };
        context.LoaiSan.AddRange(loaiSans);
        context.SaveChanges();

        // Seed SanBong
        var sanBongs = new Models.SanBong[]
        {
            new Models.SanBong { TenSan = "Sân A1", MaLoai = 1, GiaTheoGio = 300000, TrangThai = "Hoạt động", ViTri = "Khu A - Tầng 1", MoTa = "Sân 5 người có mái che, cỏ nhân tạo cao cấp", HinhAnh = "san_a1.jpg" },
            new Models.SanBong { TenSan = "Sân A2", MaLoai = 1, GiaTheoGio = 300000, TrangThai = "Hoạt động", ViTri = "Khu A - Tầng 1", MoTa = "Sân 5 người có mái che", HinhAnh = "san_a2.jpg" },
            new Models.SanBong { TenSan = "Sân B1", MaLoai = 2, GiaTheoGio = 500000, TrangThai = "Hoạt động", ViTri = "Khu B - Tầng 2", MoTa = "Sân 7 người sân cỏ nhân tạo", HinhAnh = "san_b1.jpg" },
            new Models.SanBong { TenSan = "Sân B2", MaLoai = 2, GiaTheoGio = 500000, TrangThai = "Hoạt động", ViTri = "Khu B - Tầng 2", MoTa = "Sân 7 người có đèn chiếu sáng", HinhAnh = "san_b2.jpg" },
            new Models.SanBong { TenSan = "Sân C1", MaLoai = 3, GiaTheoGio = 1000000, TrangThai = "Hoạt động", ViTri = "Khu C - Sân ngoài", MoTa = "Sân 11 người tiêu chuẩn FIFA", HinhAnh = "san_c1.jpg" },
            new Models.SanBong { TenSan = "Sân VIP", MaLoai = 1, GiaTheoGio = 400000, TrangThai = "Hoạt động", ViTri = "Khu VIP", MoTa = "Sân 5 người VIP có điều hòa", HinhAnh = "san_vip.jpg" }
        };
        context.SanBong.AddRange(sanBongs);
        context.SaveChanges();

        // Seed KhungGio
        var khungGios = new KhungGio[]
        {
            new KhungGio { GioBatDau = new TimeSpan(6, 0, 0), GioKetThuc = new TimeSpan(8, 0, 0), HeSoGia = 0.8m },
            new KhungGio { GioBatDau = new TimeSpan(8, 0, 0), GioKetThuc = new TimeSpan(10, 0, 0), HeSoGia = 1.0m },
            new KhungGio { GioBatDau = new TimeSpan(10, 0, 0), GioKetThuc = new TimeSpan(12, 0, 0), HeSoGia = 1.0m },
            new KhungGio { GioBatDau = new TimeSpan(12, 0, 0), GioKetThuc = new TimeSpan(14, 0, 0), HeSoGia = 0.9m },
            new KhungGio { GioBatDau = new TimeSpan(14, 0, 0), GioKetThuc = new TimeSpan(16, 0, 0), HeSoGia = 1.0m },
            new KhungGio { GioBatDau = new TimeSpan(16, 0, 0), GioKetThuc = new TimeSpan(18, 0, 0), HeSoGia = 1.2m },
            new KhungGio { GioBatDau = new TimeSpan(18, 0, 0), GioKetThuc = new TimeSpan(20, 0, 0), HeSoGia = 1.5m },
            new KhungGio { GioBatDau = new TimeSpan(20, 0, 0), GioKetThuc = new TimeSpan(22, 0, 0), HeSoGia = 1.5m },
            new KhungGio { GioBatDau = new TimeSpan(22, 0, 0), GioKetThuc = new TimeSpan(24, 0, 0), HeSoGia = 1.2m }
        };
        context.KhungGio.AddRange(khungGios);
        context.SaveChanges();

        // Seed NhanVien
        var nhanViens = new NhanVien[]
        {
            new NhanVien { HoTen = "Nguyễn Văn Nam", NgaySinh = new DateTime(1990, 4, 12), GioiTinh = "Nam", DienThoai = "0909000111", Email = "namnv@sanbong.vn", ChucVu = "Quản lý", MaTk = 3 },
            new NhanVien { HoTen = "Trần Thị Hoa", NgaySinh = new DateTime(1995, 8, 25), GioiTinh = "Nữ", DienThoai = "0909111222", Email = "hoatt@sanbong.vn", ChucVu = "Nhân viên lễ tân", MaTk = 4 }
        };
        context.NhanVien.AddRange(nhanViens);
        context.SaveChanges();

        // Seed KhachHang
        var khachHangs = new KhachHang[]
        {
            new KhachHang { HoTen = "Lê Văn An", DiaChi = "Quận 1, TP.HCM", DienThoai = "0905111222", Email = "levan.an@gmail.com", Cccd = "001234567890", DiemTichLuy = 50, MaTk = 5 },
            new KhachHang { HoTen = "Phạm Minh Bình", DiaChi = "Quận 3, TP.HCM", DienThoai = "0905222333", Email = "pham.binh@gmail.com", Cccd = "001234567891", DiemTichLuy = 120, MaTk = 6 },
            new KhachHang { HoTen = "Hoàng Văn Cường", DiaChi = "Quận 5, TP.HCM", DienThoai = "0905333444", Email = "hoang.cuong@gmail.com", Cccd = "001234567892", DiemTichLuy = 80, MaTk = 7 },
            new KhachHang { HoTen = "Trần Thị Dung", DiaChi = "Thủ Đức, TP.HCM", DienThoai = "0905444555", Email = "tran.dung@gmail.com", Cccd = "001234567893", DiemTichLuy = 30, MaTk = 8 },
            new KhachHang { HoTen = "Nguyễn Thị Em", DiaChi = "Bình Thạnh, TP.HCM", DienThoai = "0905555666", Email = "nguyen.em@gmail.com", Cccd = "001234567894", DiemTichLuy = 100, MaTk = 9 }
        };
        context.KhachHang.AddRange(khachHangs);
        context.SaveChanges();

        // Seed DichVu
        var dichVus = new DichVu[]
        {
            new DichVu { TenDv = "Nước suối Aquafina", DonGia = 10000, DonVi = "chai", MoTa = "Nước khoáng tinh khiết", HinhAnh = "aqua.jpg", SoLuongTon = 200 },
            new DichVu { TenDv = "Nước tăng lực Redbull", DonGia = 25000, DonVi = "lon", MoTa = "Nước tăng lực", HinhAnh = "redbull.jpg", SoLuongTon = 100 },
            new DichVu { TenDv = "Thuê giày đá bóng", DonGia = 30000, DonVi = "đôi/trận", MoTa = "Giày đá bóng chuyên dụng", HinhAnh = "giay.jpg", SoLuongTon = 50 },
            new DichVu { TenDv = "Thuê áo đấu", DonGia = 50000, DonVi = "bộ/trận", MoTa = "Áo đấu theo đội, đủ size", HinhAnh = "ao.jpg", SoLuongTon = 30 },
            new DichVu { TenDv = "Thuê bóng", DonGia = 20000, DonVi = "quả/trận", MoTa = "Bóng đá size 5 tiêu chuẩn", HinhAnh = "bong.jpg", SoLuongTon = 40 },
            new DichVu { TenDv = "Khăn lạnh", DonGia = 5000, DonVi = "chiếc", MoTa = "Khăn lạnh làm mát", HinhAnh = "khan.jpg", SoLuongTon = 150 }
        };
        context.DichVu.AddRange(dichVus);
        context.SaveChanges();

        // Seed DatSan
        var datSans = new DatSan[]
        {
            new DatSan { MaKh = 1, MaSan = 1, MaKhungGio = 7, NgayDat = DateTime.Now, NgaySd = DateTime.Now.AddDays(2), ThoiGianDat = DateTime.Now, TongTien = 450000, TrangThai = "Đã xác nhận", GhiChu = "Khách quen", MaNv = 1 },
            new DatSan { MaKh = 2, MaSan = 2, MaKhungGio = 8, NgayDat = DateTime.Now, NgaySd = DateTime.Now.AddDays(3), ThoiGianDat = DateTime.Now, TongTien = 450000, TrangThai = "Chờ xác nhận", GhiChu = null, MaNv = null },
            new DatSan { MaKh = 3, MaSan = 3, MaKhungGio = 6, NgayDat = DateTime.Now, NgaySd = DateTime.Now.AddDays(1), ThoiGianDat = DateTime.Now, TongTien = 600000, TrangThai = "Đã xác nhận", GhiChu = "Đặt cho công ty", MaNv = 2 },
            new DatSan { MaKh = 4, MaSan = 5, MaKhungGio = 7, NgayDat = DateTime.Now, NgaySd = DateTime.Now.AddDays(5), ThoiGianDat = DateTime.Now, TongTien = 1500000, TrangThai = "Đã xác nhận", GhiChu = "Giải đấu công ty", MaNv = 1 },
            new DatSan { MaKh = 5, MaSan = 1, MaKhungGio = 3, NgayDat = DateTime.Now.AddDays(-1), NgaySd = DateTime.Now, ThoiGianDat = DateTime.Now.AddDays(-1), TongTien = 300000, TrangThai = "Hoàn tất", GhiChu = "Đã sử dụng", MaNv = 1 }
        };
        context.DatSan.AddRange(datSans);
        context.SaveChanges();

        // Seed ChiTietDichVu
        var chiTietDichVus = new ChiTietDichVu[]
        {
            new ChiTietDichVu { MaDatSan = 1, MaDv = 1, SoLuong = 10, DonGia = 10000, ThanhTien = 100000 },
            new ChiTietDichVu { MaDatSan = 1, MaDv = 5, SoLuong = 1, DonGia = 20000, ThanhTien = 20000 },
            new ChiTietDichVu { MaDatSan = 3, MaDv = 2, SoLuong = 6, DonGia = 25000, ThanhTien = 150000 },
            new ChiTietDichVu { MaDatSan = 3, MaDv = 3, SoLuong = 7, DonGia = 30000, ThanhTien = 210000 },
            new ChiTietDichVu { MaDatSan = 4, MaDv = 4, SoLuong = 11, DonGia = 50000, ThanhTien = 550000 }
        };
        context.ChiTietDichVu.AddRange(chiTietDichVus);
        context.SaveChanges();

        // Seed ThanhToan
        var thanhToans = new ThanhToan[]
        {
            new ThanhToan { MaDatSan = 1, PhuongThuc = "Chuyển khoản", SoTien = 570000, NgayThanhToan = DateTime.Now, TrangThai = "Đã thanh toán" },
            new ThanhToan { MaDatSan = 5, PhuongThuc = "Tiền mặt", SoTien = 300000, NgayThanhToan = DateTime.Now, TrangThai = "Đã thanh toán" }
        };
        context.ThanhToan.AddRange(thanhToans);
        context.SaveChanges();

        // Seed DanhGia
        var danhGias = new DanhGia[]
        {
            new DanhGia { MaDatSan = 5, MaKh = 5, DiemDanhGia = 5, NoiDung = "Sân đẹp, cỏ mượt, nhân viên nhiệt tình", NgayDanhGia = DateTime.Now },
            new DanhGia { MaDatSan = 1, MaKh = 1, DiemDanhGia = 4, NoiDung = "Sân tốt, giá hợp lý", NgayDanhGia = DateTime.Now }
        };
        context.DanhGia.AddRange(danhGias);
        context.SaveChanges();

        // Seed PhanCa - sample shift assignments for current week
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
        var phanCas = new PhanCa[]
        {
            // Nhân viên 1 - Nguyễn Văn Nam
            new PhanCa { MaNv = 1, MaCa = 1, NgayLam = startOfWeek, TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 1, MaCa = 2, NgayLam = startOfWeek.AddDays(1), TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 1, MaCa = 1, NgayLam = startOfWeek.AddDays(2), TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 1, MaCa = 3, NgayLam = startOfWeek.AddDays(4), TrangThai = "Đã xác nhận", GhiChu = null },
            // Nhân viên 2 - Trần Thị Hoa
            new PhanCa { MaNv = 2, MaCa = 2, NgayLam = startOfWeek, TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 2, MaCa = 3, NgayLam = startOfWeek.AddDays(1), TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 2, MaCa = 2, NgayLam = startOfWeek.AddDays(3), TrangThai = "Đã xác nhận", GhiChu = null },
            new PhanCa { MaNv = 2, MaCa = 1, NgayLam = startOfWeek.AddDays(5), TrangThai = "Đã xác nhận", GhiChu = null }
        };
        context.PhanCa.AddRange(phanCas);
        context.SaveChanges();

        // Seed NgayLe - Ngày lễ Việt Nam (giảm giá 40%)
        var ngayLes = new NgayLe[]
        {
            // Ngày lễ dương lịch cố định hàng năm
            new NgayLe { TenNgayLe = "Tết Dương lịch", NgayBatDau = "01-01", NgayKetThuc = "01-01", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày đầu năm mới Dương lịch", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Lễ Valentine", NgayBatDau = "02-14", NgayKetThuc = "02-14", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày lễ tình nhân", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Quốc tế Phụ nữ", NgayBatDau = "03-08", NgayKetThuc = "03-08", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Quốc tế Phụ nữ 8/3", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Giải phóng miền Nam", NgayBatDau = "04-30", NgayKetThuc = "04-30", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Giải phóng miền Nam 30/4", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Quốc tế Lao động", NgayBatDau = "05-01", NgayKetThuc = "05-01", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Quốc tế Lao động 1/5", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Quốc tế Thiếu nhi", NgayBatDau = "06-01", NgayKetThuc = "06-01", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Quốc tế Thiếu nhi 1/6", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Gia đình Việt Nam", NgayBatDau = "06-28", NgayKetThuc = "06-28", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Gia đình Việt Nam 28/6", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Quốc khánh", NgayBatDau = "09-02", NgayKetThuc = "09-02", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Quốc khánh Việt Nam 2/9", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Phụ nữ Việt Nam", NgayBatDau = "10-20", NgayKetThuc = "10-20", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Phụ nữ Việt Nam 20/10", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Lễ Halloween", NgayBatDau = "10-31", NgayKetThuc = "10-31", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Lễ hội Halloween", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Nhà giáo Việt Nam", NgayBatDau = "11-20", NgayKetThuc = "11-20", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày Nhà giáo Việt Nam 20/11", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Ngày Quân đội Nhân dân", NgayBatDau = "12-22", NgayKetThuc = "12-22", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Ngày thành lập Quân đội Nhân dân Việt Nam", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Lễ Giáng sinh", NgayBatDau = "12-24", NgayKetThuc = "12-25", LoaiLich = "DuongLich", HeSoGiamGia = 0.6m, MoTa = "Lễ Giáng sinh - Christmas Eve và Christmas", TrangThai = 1 },
            
            // Ngày lễ âm lịch (cần cập nhật hàng năm)
            // Giỗ Tổ Hùng Vương (10/3 Âm lịch)
            new NgayLe { TenNgayLe = "Giỗ Tổ Hùng Vương 2025", NgayBatDau = "2025-04-07", NgayKetThuc = "2025-04-07", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Giỗ Tổ Hùng Vương - 10/3 Âm lịch năm 2025", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Giỗ Tổ Hùng Vương 2026", NgayBatDau = "2026-04-26", NgayKetThuc = "2026-04-26", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Giỗ Tổ Hùng Vương - 10/3 Âm lịch năm 2026", TrangThai = 1 },
            
            // Tết Nguyên Đán
            new NgayLe { TenNgayLe = "Tết Nguyên Đán 2025", NgayBatDau = "2025-01-28", NgayKetThuc = "2025-02-04", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Tết Nguyên Đán Ất Tỵ 2025 (30 Tết - Mùng 7)", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Tết Nguyên Đán 2026", NgayBatDau = "2026-02-16", NgayKetThuc = "2026-02-23", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Tết Nguyên Đán Bính Ngọ 2026 (30 Tết - Mùng 7)", TrangThai = 1 },
            
            // Tết Trung Thu (15/8 Âm lịch)
            new NgayLe { TenNgayLe = "Tết Trung Thu 2025", NgayBatDau = "2025-10-06", NgayKetThuc = "2025-10-06", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Tết Trung Thu - Rằm tháng 8 Âm lịch 2025", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Tết Trung Thu 2026", NgayBatDau = "2026-09-25", NgayKetThuc = "2026-09-25", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Tết Trung Thu - Rằm tháng 8 Âm lịch 2026", TrangThai = 1 },
            
            // Lễ Vu Lan (15/7 Âm lịch)
            new NgayLe { TenNgayLe = "Lễ Vu Lan 2025", NgayBatDau = "2025-09-06", NgayKetThuc = "2025-09-06", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Lễ Vu Lan Báo Hiếu - Rằm tháng 7 Âm lịch 2025", TrangThai = 1 },
            new NgayLe { TenNgayLe = "Lễ Vu Lan 2026", NgayBatDau = "2026-08-27", NgayKetThuc = "2026-08-27", LoaiLich = "AmLich", HeSoGiamGia = 0.6m, MoTa = "Lễ Vu Lan Báo Hiếu - Rằm tháng 7 Âm lịch 2026", TrangThai = 1 }
        };
        context.NgayLe.AddRange(ngayLes);
        context.SaveChanges();    }
}