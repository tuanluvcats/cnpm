using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanBong.Models;
using SanBong.Data;
using System;
using System.Linq;

namespace SanBong.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy danh sách sân bóng
            var sanBongs = _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .Where(s => s.TrangThai == "Hoạt động")
                .ToList();
            
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            
            return View(sanBongs);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult DatSan(int? maLoai)
        {
            // Lấy danh sách sân bóng có thể đặt
            var sanBongs = _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .Where(s => s.TrangThai == "Hoạt động");
            
            if (maLoai.HasValue)
            {
                sanBongs = sanBongs.Where(s => s.MaLoai == maLoai.Value);
            }
            
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            ViewBag.KhungGio = _context.KhungGio.OrderBy(k => k.GioBatDau).ToList();
            
            return View(sanBongs.ToList());
        }

        public IActionResult ChiTietSan(int id)
        {
            var san = _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .FirstOrDefault(s => s.MaSan == id);

            if (san == null) return NotFound();

            // Lấy các đánh giá của sân
            var danhGias = _context.DanhGia
                .Include(d => d.MaKhNavigation)
                .Where(d => d.MaDatSanNavigation!.MaSan == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .Take(5)
                .ToList();

            ViewBag.DanhGias = danhGias;
            ViewBag.KhungGio = _context.KhungGio.OrderBy(k => k.GioBatDau).ToList();
            
            return View(san);
        }

        [HttpGet]
        public IActionResult KiemTraSanTrong(int maSan, DateTime ngaySd, int maKhungGio)
        {
            var daDat = _context.DatSan.Any(d => 
                d.MaSan == maSan && 
                d.NgaySd.Date == ngaySd.Date && 
                d.MaKhungGio == maKhungGio &&
                d.TrangThai != "Đã hủy");

            return Json(new { available = !daDat });
        }

        public IActionResult LichSuDatSan()
        {
            var maKh = HttpContext.Session.GetInt32("MaKH");
            if (maKh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var datSans = _context.DatSan
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .Where(d => d.MaKh == maKh)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(datSans);
        }
    }
}
