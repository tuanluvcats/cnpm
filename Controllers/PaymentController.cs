using Microsoft.AspNetCore.Mvc;
using SanBong.Data;
using SanBong.Models;
using SanBong.Services;
using Microsoft.EntityFrameworkCore;

namespace SanBong.Controllers;

/// <summary>
/// Controller xử lý thanh toán online
/// </summary>
public class PaymentController : Controller
{
    private readonly AppDbContext _context;
    private readonly SandboxPaymentService _sandboxPayment;
    private readonly BankTransferService _bankTransfer;
    private readonly BookingLockService _lockService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        AppDbContext context, 
        SandboxPaymentService sandboxPayment,
        BankTransferService bankTransfer,
        BookingLockService lockService,
        ILogger<PaymentController> logger)
    {
        _context = context;
        _sandboxPayment = sandboxPayment;
        _bankTransfer = bankTransfer;
        _lockService = lockService;
        _logger = logger;
    }

    /// <summary>
    /// Trang chọn phương thức thanh toán
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int datSanId)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaKhNavigation)
            .Include(d => d.MaSanNavigation)
            .Include(d => d.MaKhungGioNavigation)
            .Include(d => d.ChiTietDichVus)
                .ThenInclude(c => c.MaDvNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        // Kiểm tra đã thanh toán chưa
        var existingPayment = await _context.ThanhToan
            .FirstOrDefaultAsync(t => t.MaDatSan == datSanId && t.TrangThai == "DaThanhToan");

        if (existingPayment != null)
        {
            TempData["Message"] = "Đơn đặt sân này đã được thanh toán!";
            return RedirectToAction("Details", "DatSan", new { id = datSanId });
        }

        return View(datSan);
    }

    /// <summary>
    /// Thanh toán tiền mặt - Tạo QR chuyển khoản tiền cọc 30%
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayWithCash(int datSanId)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaSanNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        var totalAmount = datSan.TongTien ?? 0;
        var description = $"Dat san {datSan.MaSanNavigation?.TenSan}";

        // Tạo QR chuyển khoản tiền cọc 30%
        var transferInfo = _bankTransfer.CreateDepositTransferInfo(
            datSanId.ToString(), 
            totalAmount, 
            description
        );

        // Lưu thông tin thanh toán
        var thanhToan = new ThanhToan
        {
            MaDatSan = datSanId,
            PhuongThuc = "TienMat_Coc",
            SoTien = transferInfo.Amount,
            TrangThai = "ChoCoc",
            MaGiaoDich = transferInfo.TransferCode,
            GhiChu = $"Cọc 30% - Còn lại thanh toán tiền mặt: {transferInfo.RemainingAmount:N0}đ"
        };

        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        ViewBag.TransferInfo = transferInfo;
        ViewBag.DatSan = datSan;
        ViewBag.ThanhToanId = thanhToan.MaTt;

        return View("DepositPayment", transferInfo);
    }

    /// <summary>
    /// Thanh toán chuyển khoản 100%
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayWithTransfer(int datSanId)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaSanNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        var totalAmount = datSan.TongTien ?? 0;
        var description = $"Dat san {datSan.MaSanNavigation?.TenSan}";

        // Tạo QR chuyển khoản 100%
        var transferInfo = _bankTransfer.CreateTransferInfo(
            datSanId.ToString(), 
            totalAmount, 
            description
        );

        // Lưu thông tin thanh toán
        var thanhToan = new ThanhToan
        {
            MaDatSan = datSanId,
            PhuongThuc = "ChuyenKhoan",
            SoTien = totalAmount,
            TrangThai = "ChoDuyet",
            MaGiaoDich = transferInfo.TransferCode
        };

        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        ViewBag.DatSan = datSan;
        ViewBag.ThanhToanId = thanhToan.MaTt;

        return View("TransferPayment", transferInfo);
    }

    /// <summary>
    /// Thanh toán phần còn lại (sau khi đã cọc)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PayRemaining(int datSanId)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaKhNavigation)
            .Include(d => d.MaSanNavigation)
            .Include(d => d.MaKhungGioNavigation)
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        // Tính số tiền đã thanh toán và còn lại
        var daThanhToan = datSan.ThanhToans?
            .Where(t => t.TrangThai == "DaThanhToan")
            .Sum(t => t.SoTien) ?? 0;
        var conLai = (datSan.TongTien ?? 0) - daThanhToan;

        if (conLai <= 0)
        {
            TempData["Message"] = "Đơn đặt sân này đã được thanh toán đầy đủ!";
            return RedirectToAction("Details", "DatSan", new { id = datSanId });
        }

        ViewBag.DaThanhToan = daThanhToan;
        ViewBag.ConLai = conLai;

        return View(datSan);
    }

    /// <summary>
    /// Xử lý thanh toán phần còn lại qua chuyển khoản
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayRemainingTransfer(int datSanId)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaSanNavigation)
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        // Tính số tiền còn lại
        var daThanhToan = datSan.ThanhToans?
            .Where(t => t.TrangThai == "DaThanhToan")
            .Sum(t => t.SoTien) ?? 0;
        var conLai = (datSan.TongTien ?? 0) - daThanhToan;

        if (conLai <= 0)
        {
            TempData["Message"] = "Đơn đặt sân này đã được thanh toán đầy đủ!";
            return RedirectToAction("Details", "DatSan", new { id = datSanId });
        }

        var description = $"TT con lai san {datSan.MaSanNavigation?.TenSan}";

        // Tạo QR chuyển khoản phần còn lại
        var transferInfo = _bankTransfer.CreateTransferInfo(
            $"{datSanId}_CONLAI", 
            conLai, 
            description
        );

        // Lưu thông tin thanh toán
        var thanhToan = new ThanhToan
        {
            MaDatSan = datSanId,
            PhuongThuc = "ChuyenKhoan_ConLai",
            SoTien = conLai,
            TrangThai = "ChoDuyet",
            MaGiaoDich = transferInfo.TransferCode,
            GhiChu = "Thanh toán phần còn lại sau cọc"
        };

        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        ViewBag.DatSan = datSan;
        ViewBag.ThanhToanId = thanhToan.MaTt;
        ViewBag.IsRemaining = true;

        return View("TransferPayment", transferInfo);
    }

    /// <summary>
    /// Thanh toán online (MoMo/ZaloPay) - Sandbox Demo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayOnline(int datSanId, string provider)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaKhNavigation)
            .Include(d => d.MaSanNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        // Đối với sandbox/demo, chuyển trực tiếp đến trang SandboxPayment
        ViewBag.DatSan = datSan;
        ViewBag.Provider = provider;
        ViewBag.Amount = datSan.TongTien ?? 0;
        ViewBag.OrderId = $"DS{datSanId}_{DateTime.Now:yyyyMMddHHmmss}";

        // Lưu thông tin thanh toán (chờ xác nhận)
        var thanhToan = new ThanhToan
        {
            MaDatSan = datSanId,
            PhuongThuc = provider,
            SoTien = datSan.TongTien ?? 0,
            TrangThai = "ChoDuyet",
            MaGiaoDich = ViewBag.OrderId
        };
        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        return View("SandboxPayment");

        /* 
        // Code dưới đây sử dụng cho production với MoMo/ZaloPay thật
        var returnUrl = Url.Action("PaymentReturn", "Payment", null, Request.Scheme);
        var notifyUrl = Url.Action("PaymentCallback", "Payment", null, Request.Scheme);

        var request = new PaymentRequest
        {
            OrderId = $"DS{datSanId}_{DateTime.Now:yyyyMMddHHmmss}",
            Amount = datSan.TongTien ?? 0,
            Description = $"Thanh toán đặt sân {datSan.MaSanNavigation?.TenSan}",
            ReturnUrl = returnUrl!,
            NotifyUrl = notifyUrl!,
            CustomerName = datSan.MaKhNavigation?.HoTen,
            CustomerPhone = datSan.MaKhNavigation?.DienThoai,
            ExtraData = new Dictionary<string, string>
            {
                { "datSanId", datSanId.ToString() },
                { "provider", provider }
            }
        };

        var result = await _sandboxPayment.CreatePaymentAsync(request);

        if (result.Success)
        {
            // Lưu thông tin thanh toán
            var thanhToan2 = new ThanhToan
            {
                MaDatSan = datSanId,
                PhuongThuc = provider,
                SoTien = datSan.TongTien ?? 0,
                TrangThai = "ChoDuyet",
                MaGiaoDich = result.TransactionId
            };

            _context.ThanhToan.Add(thanhToan2);
            await _context.SaveChangesAsync();

            // Lưu giao dịch
            var giaoDich = new GiaoDichThanhToan
            {
                MaThanhToan = thanhToan2.MaTt,
                MaGiaoDichCode = result.TransactionId!,
                NhaCungCap = provider,
                SoTien = datSan.TongTien ?? 0,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Pending"
            };

            _context.GiaoDichThanhToan.Add(giaoDich);
            await _context.SaveChangesAsync();

            return Redirect(result.PaymentUrl!);
        }

        TempData["Error"] = result.Message;
        return RedirectToAction("Index", new { datSanId });
        */
    }

    /// <summary>
    /// Trang thanh toán Sandbox (demo)
    /// </summary>
    [HttpGet]
    public IActionResult SandboxPayment(string transactionId)
    {
        var transaction = SandboxPaymentService.GetTransaction(transactionId);
        if (transaction == null)
        {
            return NotFound("Không tìm thấy giao dịch");
        }

        return View(transaction);
    }

    /// <summary>
    /// Xử lý thanh toán Sandbox
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcessSandboxPayment(string transactionId, string action)
    {
        var transaction = SandboxPaymentService.GetTransaction(transactionId);
        if (transaction == null)
        {
            return NotFound();
        }

        var callbackData = new Dictionary<string, string>
        {
            { "transactionId", transactionId },
            { "status", action } // success, failed, cancelled
        };

        var result = await _sandboxPayment.VerifyPaymentAsync(transactionId, callbackData);

        // Cập nhật database
        var giaoDich = await _context.GiaoDichThanhToan
            .Include(g => g.MaThanhToanNavigation)
            .FirstOrDefaultAsync(g => g.MaGiaoDichCode == transactionId);

        if (giaoDich != null)
        {
            giaoDich.TrangThai = result.Status.ToString();
            giaoDich.ThoiGianCapNhat = DateTime.Now;

            if (giaoDich.MaThanhToanNavigation != null)
            {
                if (result.Success)
                {
                    giaoDich.MaThanhToanNavigation.TrangThai = "DaThanhToan";
                    giaoDich.MaThanhToanNavigation.NgayThanhToan = DateTime.Now;

                    // Cập nhật trạng thái đặt sân
                    var datSan = await _context.DatSan.FindAsync(giaoDich.MaThanhToanNavigation.MaDatSan);
                    if (datSan != null)
                    {
                        datSan.TrangThai = "Đã xác nhận";
                    }
                }
                else
                {
                    giaoDich.MaThanhToanNavigation.TrangThai = action == "cancelled" ? "DaHuy" : "ThatBai";
                }
            }

            await _context.SaveChangesAsync();
        }

        if (result.Success)
        {
            TempData["Success"] = "Thanh toán thành công!";
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        // Redirect về trang chi tiết đặt sân
        if (giaoDich?.MaThanhToanNavigation?.MaDatSan != null)
        {
            return RedirectToAction("Details", "DatSan", new { id = giaoDich.MaThanhToanNavigation.MaDatSan });
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Xác nhận đã chuyển khoản (tiền cọc hoặc 100%)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmTransfer(int thanhToanId)
    {
        var thanhToan = await _context.ThanhToan
            .Include(t => t.MaDatSanNavigation)
            .FirstOrDefaultAsync(t => t.MaTt == thanhToanId);

        if (thanhToan == null)
        {
            return NotFound();
        }

        // Giả lập xác nhận (trong thực tế cần kiểm tra với ngân hàng)
        thanhToan.TrangThai = "DaThanhToan";
        thanhToan.NgayThanhToan = DateTime.Now;

        if (thanhToan.MaDatSanNavigation != null)
        {
            thanhToan.MaDatSanNavigation.TrangThai = thanhToan.PhuongThuc == "TienMat_Coc" 
                ? "Đã cọc" 
                : "Đã xác nhận";
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = thanhToan.PhuongThuc == "TienMat_Coc"
            ? $"Đã xác nhận cọc {thanhToan.SoTien:N0}đ. Vui lòng thanh toán phần còn lại khi đến sân."
            : "Thanh toán thành công!";

        return RedirectToAction("Details", "DatSan", new { id = thanhToan.MaDatSan });
    }

    /// <summary>
    /// API kiểm tra slot có khả dụng không
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckSlotAvailability(int maSan, DateTime ngaySd, int maKhungGio)
    {
        var sessionId = HttpContext.Session.Id;
        var available = await _lockService.IsSlotAvailableAsync(maSan, ngaySd, maKhungGio, sessionId);

        return Json(new { available });
    }

    /// <summary>
    /// API khóa slot trước khi thanh toán
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> LockSlot(int maSan, DateTime ngaySd, int maKhungGio)
    {
        var sessionId = HttpContext.Session.Id;
        int? maKh = null;

        // Lấy mã khách hàng nếu đã đăng nhập
        var username = HttpContext.Session.GetString("Username");
        if (!string.IsNullOrEmpty(username))
        {
            var khachHang = await _context.KhachHang
                .Include(k => k.MaTkNavigation)
                .FirstOrDefaultAsync(k => k.MaTkNavigation != null && k.MaTkNavigation.TenDangNhap == username);
            maKh = khachHang?.MaKh;
        }

        var result = await _lockService.TryLockAsync(maSan, ngaySd, maKhungGio, maKh, sessionId);

        return Json(result);
    }

    /// <summary>
    /// API giải phóng khóa slot
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReleaseSlot(int lockId)
    {
        var sessionId = HttpContext.Session.Id;
        var success = await _lockService.ReleaseLockAsync(lockId, sessionId);

        return Json(new { success });
    }

    /// <summary>
    /// API gia hạn khóa slot
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExtendLock(int lockId)
    {
        var sessionId = HttpContext.Session.Id;
        var success = await _lockService.ExtendLockAsync(lockId, sessionId);

        return Json(new { success });
    }

    /// <summary>
    /// Lấy danh sách slot đang bị khóa
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLockedSlots(int maSan, DateTime ngaySd)
    {
        var lockedSlots = await _lockService.GetLockedSlotsAsync(maSan, ngaySd);
        return Json(lockedSlots);
    }

    /// <summary>
    /// Callback từ trang Sandbox Payment (demo)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SandboxCallback(int datSanId, string provider, string status)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaSanNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return NotFound();
        }

        // Giải phóng khóa
        var sessionId = HttpContext.Session.Id;
        if (datSan.MaSan.HasValue && datSan.MaKhungGio.HasValue)
        {
            await _lockService.ReleaseLockBySlotAsync(datSan.MaSan.Value, datSan.NgaySd, datSan.MaKhungGio.Value, sessionId);
        }

        if (status == "success")
        {
            // Tìm và cập nhật thanh toán
            var thanhToan = await _context.ThanhToan
                .FirstOrDefaultAsync(t => t.MaDatSan == datSanId && t.TrangThai == "ChoDuyet");

            if (thanhToan != null)
            {
                thanhToan.TrangThai = "DaThanhToan";
                thanhToan.NgayThanhToan = DateTime.Now;
            }
            else
            {
                // Tạo mới thanh toán nếu chưa có
                thanhToan = new ThanhToan
                {
                    MaDatSan = datSanId,
                    PhuongThuc = provider,
                    SoTien = datSan.TongTien ?? 0,
                    TrangThai = "DaThanhToan",
                    NgayThanhToan = DateTime.Now,
                    MaGiaoDich = $"SB{datSanId}_{DateTime.Now:yyyyMMddHHmmss}"
                };
                _context.ThanhToan.Add(thanhToan);
            }

            datSan.TrangThai = "Đã xác nhận";
            await _context.SaveChangesAsync();

            ViewBag.DatSan = datSan;
            return View("Success");
        }
        else
        {
            ViewBag.DatSan = datSan;
            ViewBag.ErrorMessage = status == "cancelled" 
                ? "Bạn đã hủy giao dịch thanh toán." 
                : "Thanh toán không thành công. Vui lòng thử lại.";
            return View("Failed");
        }
    }

    /// <summary>
    /// Return URL sau khi thanh toán
    /// </summary>
    [HttpGet]
    public IActionResult PaymentReturn()
    {
        // Xử lý redirect từ cổng thanh toán
        TempData["Message"] = "Đang xử lý kết quả thanh toán...";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Callback từ cổng thanh toán (IPN)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PaymentCallback()
    {
        // Xử lý callback từ cổng thanh toán
        return Ok();
    }

    /// <summary>
    /// Lịch sử thanh toán
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Account");
        }

        var khachHang = await _context.KhachHang
            .Include(k => k.MaTkNavigation)
            .FirstOrDefaultAsync(k => k.MaTkNavigation != null && k.MaTkNavigation.TenDangNhap == username);

        if (khachHang == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var thanhToans = await _context.ThanhToan
            .Include(t => t.MaDatSanNavigation)
                .ThenInclude(d => d!.MaSanNavigation)
            .Include(t => t.GiaoDichThanhToans)
            .Where(t => t.MaDatSanNavigation != null && t.MaDatSanNavigation.MaKh == khachHang.MaKh)
            .OrderByDescending(t => t.NgayThanhToan)
            .ToListAsync();

        return View(thanhToans);
    }
}
