using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanBong.Models;
using SanBong.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SanBong.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Middleware kiểm tra Admin
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        // GET: Admin Dashboard
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            // Get statistics
            ViewBag.TongSanBong = _context.SanBong.Count();
            ViewBag.TongKhachHang = _context.KhachHang.Count();
            ViewBag.TongNhanVien = _context.NhanVien.Count();
            ViewBag.TongDatSan = _context.DatSan.Count();
            
            // Count bookings pending confirmation
            ViewBag.ChoXacNhan = _context.DatSan.Count(d => d.TrangThai == "Chờ xác nhận");
            
            // Calculate monthly revenue
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            ViewBag.DoanhThuThang = _context.ThanhToan
                .AsEnumerable()
                .Where(t => t.NgayThanhToan.HasValue && t.NgayThanhToan.Value.Month == currentMonth && t.NgayThanhToan.Value.Year == currentYear)
                .Sum(t => (decimal?)t.SoTien) ?? 0;
            
            // SQLite doesn't support SUM over decimals in translated SQL. Force client-side aggregation.
            ViewBag.DoanhThu = _context.ThanhToan
                .AsEnumerable() // switch to LINQ to Objects so Sum runs in .NET and supports decimal
                .Sum(t => (decimal?)t.SoTien) ?? 0;

            // Get recent bookings with related data
            ViewBag.RecentBookings = _context.DatSan
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .OrderByDescending(d => d.ThoiGianDat)
                .Take(5)
                .ToList();

            return View();
        }

        // GET: Admin/DatSan
        public IActionResult DatSan()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.DatSan
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .Include(d => d.MaNvNavigation)
                .Include(d => d.ThanhToans) // Include thông tin thanh toán
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(bookings);
        }

        // POST: Admin/CapNhatTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CapNhatTrangThai(int mads, string trangthai)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var booking = _context.DatSan.Find(mads);
                if (booking != null)
                {
                    booking.TrangThai = trangthai;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Booking not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/SanBong
        public IActionResult SanBong()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var sanBongs = _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .ToList();

            return View(sanBongs);
        }

        // GET: Admin/KhachHang
        public IActionResult KhachHang()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var khachHangs = _context.KhachHang
                .Include(k => k.MaTkNavigation)
                .ToList();

            return View(khachHangs);
        }

        // POST: Admin/CreateKhachHang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateKhachHang(string hoTen, string dienThoai, string? email, string? diaChi, string? cccd, string? tenDangNhap, string? matKhau)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                // Tạo tài khoản nếu có thông tin đăng nhập
                int? maTk = null;
                if (!string.IsNullOrEmpty(tenDangNhap) && !string.IsNullOrEmpty(matKhau))
                {
                    // Kiểm tra tên đăng nhập đã tồn tại chưa
                    if (_context.TaiKhoan.Any(t => t.TenDangNhap == tenDangNhap))
                    {
                        return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                    }

                    var taiKhoan = new TaiKhoan
                    {
                        TenDangNhap = tenDangNhap,
                        MatKhau = matKhau,
                        VaiTro = "KhachHang",
                        TrangThai = 1
                    };
                    _context.TaiKhoan.Add(taiKhoan);
                    _context.SaveChanges();
                    maTk = taiKhoan.MaTk;
                }

                var khachHang = new KhachHang
                {
                    HoTen = hoTen,
                    DienThoai = dienThoai,
                    Email = email,
                    DiaChi = diaChi,
                    Cccd = cccd,
                    DiemTichLuy = 0,
                    MaTk = maTk
                };
                _context.KhachHang.Add(khachHang);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/EditKhachHang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditKhachHang(int maKh, string hoTen, string dienThoai, string? email, string? diaChi, string? cccd, int? diemTichLuy)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var khachHang = _context.KhachHang.Find(maKh);
                if (khachHang != null)
                {
                    khachHang.HoTen = hoTen;
                    khachHang.DienThoai = dienThoai;
                    khachHang.Email = email;
                    khachHang.DiaChi = diaChi;
                    khachHang.Cccd = cccd;
                    khachHang.DiemTichLuy = diemTichLuy ?? 0;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Khách hàng không tồn tại" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/DeleteKhachHang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteKhachHang(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var khachHang = _context.KhachHang.Include(k => k.DatSans).Include(k => k.DanhGias).FirstOrDefault(k => k.MaKh == id);
                if (khachHang != null)
                {
                    // Kiểm tra có đơn đặt sân không
                    if (khachHang.DatSans.Any())
                    {
                        return Json(new { success = false, message = "Không thể xóa khách hàng đã có đơn đặt sân" });
                    }

                    // Xóa đánh giá
                    _context.DanhGia.RemoveRange(khachHang.DanhGias);

                    // Xóa tài khoản nếu có
                    if (khachHang.MaTk.HasValue)
                    {
                        var taiKhoan = _context.TaiKhoan.Find(khachHang.MaTk.Value);
                        if (taiKhoan != null)
                        {
                            _context.TaiKhoan.Remove(taiKhoan);
                        }
                    }

                    _context.KhachHang.Remove(khachHang);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Khách hàng không tồn tại" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/ToggleKhachHangAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleKhachHangAccount(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var khachHang = _context.KhachHang.Include(k => k.MaTkNavigation).FirstOrDefault(k => k.MaKh == id);
                if (khachHang != null && khachHang.MaTkNavigation != null)
                {
                    // Toggle trạng thái: 1 -> 0, 0 -> 1
                    khachHang.MaTkNavigation.TrangThai = khachHang.MaTkNavigation.TrangThai == 1 ? 0 : 1;
                    _context.SaveChanges();
                    
                    var newStatus = khachHang.MaTkNavigation.TrangThai == 1 ? "Hoạt động" : "Đã khóa";
                    return Json(new { success = true, newStatus = newStatus, trangThai = khachHang.MaTkNavigation.TrangThai });
                }
                return Json(new { success = false, message = "Khách hàng không có tài khoản" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/GetKhachHang
        [HttpGet]
        public JsonResult GetKhachHang(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var khachHang = _context.KhachHang.Include(k => k.MaTkNavigation).FirstOrDefault(k => k.MaKh == id);
            if (khachHang != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        maKh = khachHang.MaKh,
                        hoTen = khachHang.HoTen,
                        dienThoai = khachHang.DienThoai,
                        email = khachHang.Email,
                        diaChi = khachHang.DiaChi,
                        cccd = khachHang.Cccd,
                        diemTichLuy = khachHang.DiemTichLuy,
                        tenDangNhap = khachHang.MaTkNavigation?.TenDangNhap,
                        trangThai = khachHang.MaTkNavigation?.TrangThai
                    }
                });
            }
            return Json(new { success = false, message = "Không tìm thấy khách hàng" });
        }

        // GET: Admin/NhanVien
        public IActionResult NhanVien()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var nhanViens = _context.NhanVien
                .Include(n => n.MaTkNavigation)
                .ToList();

            return View(nhanViens);
        }

        // POST: Admin/CreateNhanVien
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateNhanVien(NhanVien model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                _context.NhanVien.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/EditNhanVien
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditNhanVien(NhanVien model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var nhanVien = _context.NhanVien.Find(model.MaNv);
                if (nhanVien != null)
                {
                    nhanVien.HoTen = model.HoTen;
                    nhanVien.NgaySinh = model.NgaySinh;
                    nhanVien.GioiTinh = model.GioiTinh;
                    nhanVien.DienThoai = model.DienThoai;
                    nhanVien.Email = model.Email;
                    nhanVien.ChucVu = model.ChucVu;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Employee not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/DeleteNhanVien
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteNhanVien(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var nhanVien = _context.NhanVien.Find(id);
                if (nhanVien != null)
                {
                    _context.NhanVien.Remove(nhanVien);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Employee not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/DichVu
        public IActionResult DichVu()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var dichVus = _context.DichVu.ToList();

            return View(dichVus);
        }

        // POST: Admin/UpdateStockDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateStockDichVu(int id, int quantity)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var dichVu = _context.DichVu.Find(id);
                if (dichVu != null)
                {
                    dichVu.SoLuongTon = (dichVu.SoLuongTon ?? 0) + quantity;
                    _context.SaveChanges();
                    return Json(new { success = true, newStock = dichVu.SoLuongTon });
                }
                return Json(new { success = false, message = "Service not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/CreateDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateDichVu(DichVu model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                _context.DichVu.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/EditDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditDichVu(DichVu model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var dichVu = _context.DichVu.Find(model.MaDv);
                if (dichVu != null)
                {
                    dichVu.TenDv = model.TenDv;
                    dichVu.DonGia = model.DonGia;
                    dichVu.DonVi = model.DonVi;
                    dichVu.MoTa = model.MoTa;
                    dichVu.SoLuongTon = model.SoLuongTon;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Service not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/DeleteDichVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteDichVu(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var dichVu = _context.DichVu.Find(id);
                if (dichVu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy dịch vụ" });
                }

                // Check if service is being used in any bookings
                var isUsedInBookings = _context.ChiTietDichVu.Any(ct => ct.MaDv == id);
                if (isUsedInBookings)
                {
                    return Json(new { success = false, message = "Không thể xóa dịch vụ này vì đã được sử dụng trong các đơn đặt sân. Bạn có thể đặt số lượng tồn về 0 để ngừng cung cấp dịch vụ." });
                }

                _context.DichVu.Remove(dichVu);
                _context.SaveChanges();
                return Json(new { success = true, message = "Xóa dịch vụ thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/BaoCaoDoanhThu
        public IActionResult BaoCaoDoanhThu(int? thang, int? nam)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            int selectedMonth = thang ?? DateTime.Now.Month;
            int selectedYear = nam ?? DateTime.Now.Year;

            var startOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var doanhThu = _context.DatSan
                .Where(d => d.TrangThai == "Hoàn tất" && d.NgayDat >= startOfMonth && d.NgayDat <= endOfMonth)
                .AsEnumerable()
                .Sum(d => d.TongTien ?? 0);

            // Get daily revenue for chart
            var dailyRevenue = _context.DatSan
                .Where(d => d.TrangThai == "Hoàn tất" && d.NgayDat >= startOfMonth && d.NgayDat <= endOfMonth)
                .AsEnumerable()
                .GroupBy(d => d.NgayDat.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(d => d.TongTien ?? 0) })
                .OrderBy(x => x.Day)
                .ToList();

            ViewBag.Thang = selectedMonth;
            ViewBag.Nam = selectedYear;
            ViewBag.DoanhThu = doanhThu;
            ViewBag.DailyRevenue = dailyRevenue;

            return View();
        }

        // =============================================
        // QUẢN LÝ LỊCH LÀM VIỆC NHÂN VIÊN
        // =============================================

        // GET: Admin/LichLamViec
        public IActionResult LichLamViec()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // API: Lấy danh sách nhân viên
        [HttpGet]
        public JsonResult GetNhanVienList()
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var employees = _context.NhanVien
                    .Select(nv => new
                    {
                        ID_NV = nv.MaNv,
                        Ma_NV = "NV" + nv.MaNv.ToString("D3"),
                        Ho_ten = nv.HoTen,
                        Ten_ChucVu = nv.ChucVu ?? "Nhân viên"
                    })
                    .ToList();

                return Json(new { ok = true, employees });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Lấy danh sách ca làm
        [HttpGet]
        public JsonResult GetCaLamList()
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var shifts = _context.CaLam
                    .Select(ca => new
                    {
                        ID_Ca = ca.MaCa,
                        Ten_Ca = ca.TenCa,
                        Gio_bat_dau = ca.GioBatDau.ToString(@"hh\:mm"),
                        Gio_ket_thuc = ca.GioKetThuc.ToString(@"hh\:mm"),
                        Mo_ta = ca.MoTa
                    })
                    .ToList();

                return Json(new { ok = true, shifts });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Lấy danh sách phân ca
        [HttpGet]
        public JsonResult GetPhanCaList(string start_date, string end_date, int? employee_id)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var query = _context.PhanCa
                    .Include(pc => pc.MaNvNavigation)
                    .Include(pc => pc.MaCaNavigation)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(start_date) && DateTime.TryParse(start_date, out DateTime startDate))
                {
                    query = query.Where(pc => pc.NgayLam >= startDate);
                }

                if (!string.IsNullOrEmpty(end_date) && DateTime.TryParse(end_date, out DateTime endDate))
                {
                    query = query.Where(pc => pc.NgayLam <= endDate);
                }

                if (employee_id.HasValue)
                {
                    query = query.Where(pc => pc.MaNv == employee_id.Value);
                }

                var assignments = query
                    .Select(pc => new
                    {
                        ID_Phan_Ca = pc.MaPhanCa,
                        ID_NV = pc.MaNv,
                        ID_Ca = pc.MaCa,
                        Ngay_lam = pc.NgayLam.ToString("yyyy-MM-dd"),
                        Trang_thai = pc.TrangThai,
                        Ghi_chu = pc.GhiChu,
                        Ho_ten = pc.MaNvNavigation != null ? pc.MaNvNavigation.HoTen : "",
                        Ma_NV = pc.MaNvNavigation != null ? "NV" + pc.MaNvNavigation.MaNv.ToString("D3") : "",
                        Ten_Ca = pc.MaCaNavigation != null ? pc.MaCaNavigation.TenCa : "",
                        Gio_bat_dau = pc.MaCaNavigation != null ? pc.MaCaNavigation.GioBatDau.ToString(@"hh\:mm") : "",
                        Gio_ket_thuc = pc.MaCaNavigation != null ? pc.MaCaNavigation.GioKetThuc.ToString(@"hh\:mm") : ""
                    })
                    .ToList();

                return Json(new { ok = true, assignments });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Tạo phân ca mới
        [HttpPost]
        public JsonResult CreatePhanCa([FromBody] PhanCaRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                // Kiểm tra trùng lặp
                var exists = _context.PhanCa.Any(pc =>
                    pc.MaNv == data.id_nv &&
                    pc.NgayLam == DateTime.Parse(data.ngay_lam) &&
                    pc.MaCa == data.id_ca);

                if (exists)
                {
                    return Json(new { ok = false, error = "Nhân viên đã có ca làm này trong ngày" });
                }

                var phanCa = new PhanCa
                {
                    MaNv = data.id_nv,
                    MaCa = data.id_ca,
                    NgayLam = DateTime.Parse(data.ngay_lam),
                    TrangThai = data.trang_thai ?? "Đang chờ",
                    GhiChu = data.ghi_chu
                };

                _context.PhanCa.Add(phanCa);
                _context.SaveChanges();

                return Json(new { ok = true, message = "Thêm phân ca thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Cập nhật phân ca
        [HttpPut]
        public JsonResult UpdatePhanCa(int id, [FromBody] PhanCaRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var phanCa = _context.PhanCa.Find(id);
                if (phanCa == null)
                {
                    return Json(new { ok = false, error = "Không tìm thấy phân ca" });
                }

                phanCa.MaNv = data.id_nv;
                phanCa.MaCa = data.id_ca;
                phanCa.NgayLam = DateTime.Parse(data.ngay_lam);
                phanCa.TrangThai = data.trang_thai ?? phanCa.TrangThai;
                phanCa.GhiChu = data.ghi_chu;

                _context.SaveChanges();

                return Json(new { ok = true, message = "Cập nhật phân ca thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Xóa phân ca
        [HttpDelete]
        public JsonResult DeletePhanCa(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var phanCa = _context.PhanCa.Find(id);
                if (phanCa == null)
                {
                    return Json(new { ok = false, error = "Không tìm thấy phân ca" });
                }

                _context.PhanCa.Remove(phanCa);
                _context.SaveChanges();

                return Json(new { ok = true, message = "Xóa phân ca thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Đổi ca giữa 2 nhân viên
        [HttpPost]
        public JsonResult SwapPhanCa([FromBody] SwapPhanCaRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var assignment1 = _context.PhanCa.Find(data.assignment_id_1);
                var assignment2 = _context.PhanCa.Find(data.assignment_id_2);

                if (assignment1 == null || assignment2 == null)
                {
                    return Json(new { ok = false, error = "Không tìm thấy phân ca" });
                }

                // Đổi nhân viên
                var tempNv = assignment1.MaNv;
                assignment1.MaNv = assignment2.MaNv;
                assignment2.MaNv = tempNv;

                _context.SaveChanges();

                return Json(new { ok = true, message = "Đổi ca thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Điều chuyển ca (Drag drop sang ô trống)
        [HttpPost]
        public JsonResult ReassignPhanCa([FromBody] ReassignPhanCaRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var phanCa = _context.PhanCa.Find(data.assignment_id);
                if (phanCa == null)
                {
                    return Json(new { ok = false, error = "Không tìm thấy phân ca" });
                }

                if (data.new_employee_id.HasValue)
                {
                    phanCa.MaNv = data.new_employee_id.Value;
                }

                if (data.new_shift_id.HasValue)
                {
                    phanCa.MaCa = data.new_shift_id.Value;
                }

                if (!string.IsNullOrEmpty(data.new_date))
                {
                    phanCa.NgayLam = DateTime.Parse(data.new_date);
                }

                _context.SaveChanges();

                return Json(new { ok = true, message = "Cập nhật phân ca thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Tạo nhiều phân ca hàng loạt
        [HttpPost]
        public JsonResult BulkCreatePhanCa([FromBody] BulkPhanCaRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                int created = 0;
                var errors = new List<string>();

                foreach (var a in data.assignments)
                {
                    // Kiểm tra trùng lặp
                    var exists = _context.PhanCa.Any(pc =>
                        pc.MaNv == a.id_nv &&
                        pc.NgayLam == DateTime.Parse(a.ngay_lam) &&
                        pc.MaCa == a.id_ca);

                    if (exists)
                    {
                        errors.Add($"Nhân viên {a.ho_ten ?? a.id_nv.ToString()} đã có ca này");
                        continue;
                    }

                    var phanCa = new PhanCa
                    {
                        MaNv = a.id_nv,
                        MaCa = a.id_ca,
                        NgayLam = DateTime.Parse(a.ngay_lam),
                        TrangThai = a.trang_thai ?? "Đang chờ"
                    };

                    _context.PhanCa.Add(phanCa);
                    created++;
                }

                _context.SaveChanges();

                return Json(new { ok = true, message = $"Đã tạo {created}/{data.assignments.Count} phân ca", errors });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Tạo ca làm mới
        [HttpPost]
        public JsonResult CreateCaLam([FromBody] CaLamRequest data)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var caLam = new CaLam
                {
                    TenCa = data.ten_ca,
                    GioBatDau = TimeSpan.Parse(data.gio_bat_dau),
                    GioKetThuc = TimeSpan.Parse(data.gio_ket_thuc),
                    MoTa = data.mo_ta
                };

                _context.CaLam.Add(caLam);
                _context.SaveChanges();

                return Json(new { ok = true, message = "Thêm ca làm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // API: Xóa ca làm
        [HttpDelete]
        public JsonResult DeleteCaLam(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { ok = false, error = "Unauthorized" });
            }

            try
            {
                var caLam = _context.CaLam.Find(id);
                if (caLam == null)
                {
                    return Json(new { ok = false, error = "Không tìm thấy ca làm" });
                }

                // Kiểm tra xem có phân ca nào đang sử dụng ca này không
                var hasAssignments = _context.PhanCa.Any(pc => pc.MaCa == id);
                if (hasAssignments)
                {
                    return Json(new { ok = false, error = "Không thể xóa ca làm này vì đã có nhân viên được phân ca" });
                }

                _context.CaLam.Remove(caLam);
                _context.SaveChanges();

                return Json(new { ok = true, message = "Xóa ca làm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }

    // Request classes
    public class PhanCaRequest
    {
        public int id_nv { get; set; }
        public int id_ca { get; set; }
        public string ngay_lam { get; set; } = null!;
        public string? trang_thai { get; set; }
        public string? ghi_chu { get; set; }
        public string? ho_ten { get; set; }
    }

    public class SwapPhanCaRequest
    {
        public int assignment_id_1 { get; set; }
        public int assignment_id_2 { get; set; }
    }

    public class ReassignPhanCaRequest
    {
        public int assignment_id { get; set; }
        public int? new_employee_id { get; set; }
        public int? new_shift_id { get; set; }
        public string? new_date { get; set; }
    }

    public class BulkPhanCaRequest
    {
        public List<PhanCaRequest> assignments { get; set; } = new List<PhanCaRequest>();
    }

    public class CaLamRequest
    {
        public string ten_ca { get; set; } = null!;
        public string gio_bat_dau { get; set; } = null!;
        public string gio_ket_thuc { get; set; } = null!;
        public string? mo_ta { get; set; }
    }
}
