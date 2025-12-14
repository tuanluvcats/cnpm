using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Models;

namespace SanBong.Controllers
{
    public class DatSanController : Controller
    {
        private readonly AppDbContext _context;

        public DatSanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DatSan
        public async Task<IActionResult> Index()
        {
            var datSans = await _context.DatSan
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .Include(d => d.MaNvNavigation)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            
            return View(datSans);
        }

        // GET: DatSan/Create
        public IActionResult Create(int? maSan, DateTime? ngaySd)
        {
            var maKh = HttpContext.Session.GetInt32("MaKH");
            if (maKh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SanBongs = _context.SanBong.Where(s => s.TrangThai == "Hoạt động").ToList();
            ViewBag.KhungGios = _context.KhungGio.AsEnumerable().OrderBy(k => k.GioBatDau).ToList();
            ViewBag.DichVus = _context.DichVu.Where(d => d.SoLuongTon > 0).ToList();
            
            // Pass pre-selected values
            ViewBag.PreSelectedMaSan = maSan;
            ViewBag.PreSelectedNgaySd = ngaySd?.ToString("yyyy-MM-dd");
            
            return View();
        }

        // API: Check if time slot is booked
        [HttpPost]
        public IActionResult CheckTimeSlotAvailability(int maSan, DateTime ngaySd, int maKhungGio)
        {
            var daDat = _context.DatSan.Any(d => 
                d.MaSan == maSan && 
                d.NgaySd.Date == ngaySd.Date && 
                d.MaKhungGio == maKhungGio &&
                d.TrangThai != "Đã hủy");

            return Json(new { isBooked = daDat });
        }

        // POST: Custom time booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomTime(int maSan, DateTime ngaySd, TimeSpan gioCustom, int soGio, string? ghiChu, List<int>? dichVuIds, List<int>? soLuongs)
        {
            var maKh = HttpContext.Session.GetInt32("MaKH");
            if (maKh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra trùng lặp với custom time
            var ngayGioSd = ngaySd.Date + gioCustom;
            var ngayGioKetThuc = ngayGioSd.AddHours(soGio);

            // Check if overlaps with existing bookings
            var trungLap = await _context.DatSan
                .Where(d => d.MaSan == maSan && 
                            d.NgaySd.Date == ngaySd.Date && 
                            d.TrangThai != "Đã hủy")
                .ToListAsync();

            foreach (var booking in trungLap)
            {               
                // Simple overlap check - can be enhanced
                TempData["Error"] = "Sân đã được đặt vào thời gian này hoặc gần thời gian này!";
                return RedirectToAction("Create", new { maSan = maSan });
            }

            // Tính tiền sân (giá theo giờ * số giờ)
            var san = await _context.SanBong.FindAsync(maSan);
            decimal tongTien = san!.GiaTheoGio * soGio;

            // Tạo đơn đặt sân (không có MaKhungGio vì là custom)
            var datSan = new DatSan
            {
                MaKh = maKh.Value,
                MaSan = maSan,
                MaKhungGio = null, // Custom time doesn't use predefined slots
                NgayDat = DateTime.Now,
                NgaySd = ngayGioSd,
                ThoiGianDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = "Chờ xác nhận",
                GhiChu = ghiChu + $" (Đặt tùy chọn: {soGio} giờ từ {gioCustom:hh\\:mm})"
            };

            _context.DatSan.Add(datSan);
            await _context.SaveChangesAsync();

            // Thêm dịch vụ nếu có
            if (dichVuIds != null && soLuongs != null)
            {
                for (int i = 0; i < dichVuIds.Count; i++)
                {
                    if (soLuongs[i] > 0)
                    {
                        var dichVu = await _context.DichVu.FindAsync(dichVuIds[i]);
                        if (dichVu != null)
                        {
                            var chiTiet = new ChiTietDichVu
                            {
                                MaDatSan = datSan.MaDatSan,
                                MaDv = dichVuIds[i],
                                SoLuong = soLuongs[i],
                                DonGia = dichVu.DonGia,
                                ThanhTien = dichVu.DonGia * soLuongs[i]
                            };
                            _context.ChiTietDichVu.Add(chiTiet);
                            tongTien += chiTiet.ThanhTien ?? 0;
                        }
                    }
                }
                
                datSan.TongTien = tongTien;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đặt sân thành công! Vui lòng chờ xác nhận.";
            return RedirectToAction("Index");
        }

        // POST: DatSan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int maSan, int maKhungGio, DateTime ngaySd, string? ghiChu, List<int>? dichVuIds, List<int>? soLuongs)
        {
            var maKh = HttpContext.Session.GetInt32("MaKH");
            if (maKh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra sân có trống không
            var daDat = await _context.DatSan.AnyAsync(d => 
                d.MaSan == maSan && 
                d.NgaySd.Date == ngaySd.Date && 
                d.MaKhungGio == maKhungGio &&
                d.TrangThai != "Đã hủy");

            if (daDat)
            {
                TempData["Error"] = "Sân đã được đặt trong khung giờ này!";
                return RedirectToAction("Create", new { maSan = maSan, ngaySd = ngaySd });
            }

            // Tính tiền sân
            var san = await _context.SanBong.FindAsync(maSan);
            var khungGio = await _context.KhungGio.FindAsync(maKhungGio);
            decimal tongTien = san!.GiaTheoGio * (khungGio?.HeSoGia ?? 1.0m);

            // Tạo đơn đặt sân
            var datSan = new DatSan
            {
                MaKh = maKh.Value,
                MaSan = maSan,
                MaKhungGio = maKhungGio,
                NgayDat = DateTime.Now,
                NgaySd = ngaySd,
                ThoiGianDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = "Chờ xác nhận",
                GhiChu = ghiChu
            };

            _context.DatSan.Add(datSan);
            await _context.SaveChangesAsync();

            // Thêm dịch vụ nếu có
            if (dichVuIds != null && soLuongs != null)
            {
                for (int i = 0; i < dichVuIds.Count; i++)
                {
                    if (soLuongs[i] > 0)
                    {
                        var dichVu = await _context.DichVu.FindAsync(dichVuIds[i]);
                        if (dichVu != null)
                        {
                            var chiTiet = new ChiTietDichVu
                            {
                                MaDatSan = datSan.MaDatSan,
                                MaDv = dichVuIds[i],
                                SoLuong = soLuongs[i],
                                DonGia = dichVu.DonGia,
                                ThanhTien = dichVu.DonGia * soLuongs[i]
                            };
                            _context.ChiTietDichVu.Add(chiTiet);
                            tongTien += chiTiet.ThanhTien ?? 0;
                        }
                    }
                }
                
                datSan.TongTien = tongTien;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đặt sân thành công! Vui lòng chờ xác nhận.";
            return RedirectToAction("Details", new { id = datSan.MaDatSan });
        }

        // GET: DatSan/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var datSan = await _context.DatSan
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .Include(d => d.MaNvNavigation)
                .Include(d => d.ChiTietDichVus)
                    .ThenInclude(ct => ct.MaDvNavigation)
                .FirstOrDefaultAsync(m => m.MaDatSan == id);

            if (datSan == null)
            {
                return NotFound();
            }

            return View(datSan);
        }

        // POST: DatSan/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var datSan = await _context.DatSan.FindAsync(id);
                if (datSan != null)
                {
                    datSan.TrangThai = "Đã hủy";
                    await _context.SaveChangesAsync();
                    
                    // Check if it's an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Hủy đặt sân thành công!" });
                    }
                    
                    TempData["Success"] = "Hủy đặt sân thành công!";
                }
                else
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Không tìm thấy đơn đặt sân" });
                    }
                    TempData["Error"] = "Không tìm thấy đơn đặt sân";
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: DatSan/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, int? maNv)
        {
            try
            {
                var sessionMaNv = HttpContext.Session.GetInt32("MaNV");
                var finalMaNv = maNv ?? sessionMaNv;
                
                if (finalMaNv == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Không có quyền xác nhận" });
                    }
                    return Unauthorized();
                }

                var datSan = await _context.DatSan.FindAsync(id);
                if (datSan != null)
                {
                    datSan.TrangThai = "Đã xác nhận";
                    datSan.MaNv = finalMaNv.Value;
                    await _context.SaveChangesAsync();
                    
                    // Check if it's an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Xác nhận đặt sân thành công!" });
                    }
                    
                    TempData["Success"] = "Xác nhận đặt sân thành công!";
                }
                else
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Không tìm thấy đơn đặt sân" });
                    }
                    TempData["Error"] = "Không tìm thấy đơn đặt sân";
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
