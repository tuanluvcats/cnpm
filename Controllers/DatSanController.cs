using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Models;
using SanBong.Services;

namespace SanBong.Controllers
{
    public class DatSanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly HolidayDiscountService _holidayService;

        public DatSanController(AppDbContext context, HolidayDiscountService holidayService)
        {
            _context = context;
            _holidayService = holidayService;
        }

        // GET: DatSan
        public async Task<IActionResult> Index()
        {
            var datSans = await _context.DatSan
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaSanNavigation)
                .Include(d => d.MaKhungGioNavigation)
                .Include(d => d.MaNvNavigation)
                .Include(d => d.MaNgayLeNavigation)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            
            return View(datSans);
        }

        // GET: DatSan/Create
        public async Task<IActionResult> Create(int? maSan, DateTime? ngaySd)
        {
            var maKh = HttpContext.Session.GetInt32("MaKH");
            if (maKh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SanBongs = _context.SanBong.Where(s => s.TrangThai == "Hoạt động").ToList();
            ViewBag.KhungGios = _context.KhungGio.AsEnumerable().OrderBy(k => k.GioBatDau).ToList();
            ViewBag.DichVus = _context.DichVu.Where(d => d.SoLuongTon > 0).ToList();
            
            // Kiểm tra giảm giá ngày lễ cho ngày được chọn
            if (ngaySd.HasValue)
            {
                var holidayInfo = await _holidayService.GetDiscountInfoAsync(ngaySd.Value);
                ViewBag.HolidayDiscount = holidayInfo;
            }
            
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

        // API: Kiểm tra giảm giá ngày lễ
        [HttpPost]
        public async Task<IActionResult> CheckHolidayDiscount(DateTime ngaySd)
        {
            var holidayInfo = await _holidayService.GetDiscountInfoAsync(ngaySd);
            if (holidayInfo != null)
            {
                return Json(new { 
                    isHoliday = true, 
                    holidayName = holidayInfo.HolidayName,
                    discountPercent = holidayInfo.DiscountPercent,
                    description = holidayInfo.Description
                });
            }
            return Json(new { isHoliday = false });
        }

        // API: Tính giá với giảm giá ngày lễ
        [HttpPost]
        public async Task<IActionResult> CalculatePriceWithHoliday(int maSan, int maKhungGio, DateTime ngaySd)
        {
            var san = await _context.SanBong.FindAsync(maSan);
            var khungGio = await _context.KhungGio.FindAsync(maKhungGio);
            
            if (san == null || khungGio == null)
                return Json(new { success = false, message = "Không tìm thấy sân hoặc khung giờ" });

            decimal giaGoc = san.GiaTheoGio * (khungGio.HeSoGia ?? 1.0m);
            var (finalPrice, discountAmount, holiday) = await _holidayService.CalculateHolidayPriceAsync(giaGoc, ngaySd);

            return Json(new { 
                success = true,
                giaGoc = giaGoc,
                giaSauGiam = finalPrice,
                soTienGiam = discountAmount,
                isHoliday = holiday != null,
                holidayName = holiday?.TenNgayLe,
                discountPercent = holiday != null ? (int)((1 - holiday.HeSoGiamGia) * 100) : 0
            });
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

            // Tính tiền sân (giá gốc = giá theo giờ * số giờ)
            var san = await _context.SanBong.FindAsync(maSan);
            decimal giaGoc = san!.GiaTheoGio * soGio;

            // Kiểm tra và áp dụng giảm giá ngày lễ
            var (finalPrice, discountAmount, holiday) = await _holidayService.CalculateHolidayPriceAsync(giaGoc, ngaySd);
            
            string customNote = $" (Đặt tùy chọn: {soGio} giờ từ {gioCustom:hh\\:mm})";
            string? ghiChuFinal = ghiChu + customNote;
            if (holiday != null)
            {
                ghiChuFinal = $"🎉 Giảm giá {(int)((1 - holiday.HeSoGiamGia) * 100)}% nhân dịp {holiday.TenNgayLe} | " + ghiChuFinal;
            }

            // Tạo đơn đặt sân (không có MaKhungGio vì là custom)
            var datSan = new DatSan
            {
                MaKh = maKh.Value,
                MaSan = maSan,
                MaKhungGio = null, // Custom time doesn't use predefined slots
                NgayDat = DateTime.Now,
                NgaySd = ngayGioSd,
                ThoiGianDat = DateTime.Now,
                GiaGoc = giaGoc,
                GiamGiaNgayLe = discountAmount,
                TongTien = finalPrice,
                TrangThai = "Chờ xác nhận",
                GhiChu = ghiChuFinal,
                MaNgayLe = holiday?.MaNgayLe
            };

            _context.DatSan.Add(datSan);
            await _context.SaveChangesAsync();

            // Thêm dịch vụ nếu có
            decimal tongTien = finalPrice;
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

            TempData["Success"] = "Đặt sân thành công! Vui lòng tiến hành thanh toán.";
            // Chuyển đến trang thanh toán ngay sau khi đặt sân
            return RedirectToAction("Index", "Payment", new { datSanId = datSan.MaDatSan });
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

            // Tính tiền sân (giá gốc)
            var san = await _context.SanBong.FindAsync(maSan);
            var khungGio = await _context.KhungGio.FindAsync(maKhungGio);
            decimal giaGoc = san!.GiaTheoGio * (khungGio?.HeSoGia ?? 1.0m);

            // Kiểm tra và áp dụng giảm giá ngày lễ
            var (finalPrice, discountAmount, holiday) = await _holidayService.CalculateHolidayPriceAsync(giaGoc, ngaySd);
            
            string? ghiChuFinal = ghiChu;
            if (holiday != null)
            {
                ghiChuFinal = $"🎉 Giảm giá {(int)((1 - holiday.HeSoGiamGia) * 100)}% nhân dịp {holiday.TenNgayLe}" + 
                              (string.IsNullOrEmpty(ghiChu) ? "" : $" | {ghiChu}");
            }

            // Tạo đơn đặt sân
            var datSan = new DatSan
            {
                MaKh = maKh.Value,
                MaSan = maSan,
                MaKhungGio = maKhungGio,
                NgayDat = DateTime.Now,
                NgaySd = ngaySd,
                ThoiGianDat = DateTime.Now,
                GiaGoc = giaGoc,
                GiamGiaNgayLe = discountAmount,
                TongTien = finalPrice,
                TrangThai = "Chờ xác nhận",
                GhiChu = ghiChuFinal,
                MaNgayLe = holiday?.MaNgayLe
            };

            _context.DatSan.Add(datSan);
            await _context.SaveChangesAsync();

            // Thêm dịch vụ nếu có
            decimal tongTien = finalPrice;
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

            TempData["Success"] = "Đặt sân thành công! Vui lòng tiến hành thanh toán.";
            // Chuyển đến trang thanh toán ngay sau khi đặt sân
            return RedirectToAction("Index", "Payment", new { datSanId = datSan.MaDatSan });
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
                .Include(d => d.MaNgayLeNavigation) // Include thông tin ngày lễ
                .Include(d => d.ChiTietDichVus)
                    .ThenInclude(ct => ct.MaDvNavigation)
                .Include(d => d.ThanhToans) // Include thông tin thanh toán
                .FirstOrDefaultAsync(m => m.MaDatSan == id);

            if (datSan == null)
            {
                return NotFound();
            }

            // Tính tổng số tiền đã thanh toán
            var tongDaThanhToan = datSan.ThanhToans?
                .Where(t => t.TrangThai == "DaThanhToan")
                .Sum(t => t.SoTien) ?? 0;
            
            ViewBag.TongDaThanhToan = tongDaThanhToan;
            ViewBag.ConLai = (datSan.TongTien ?? 0) - tongDaThanhToan;

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
