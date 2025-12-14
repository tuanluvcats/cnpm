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
    }
}
