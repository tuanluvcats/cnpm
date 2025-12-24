using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Models;

namespace SanBong.Services;

/// <summary>
/// Service quản lý khóa sân
/// Ngăn chặn đặt trùng khi đang trong quá trình thanh toán
/// </summary>
public class BookingLockService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingLockService> _logger;
    
    // Thời gian giữ khóa mặc định (phút)
    public const int DEFAULT_LOCK_DURATION_MINUTES = 10;
    
    public BookingLockService(AppDbContext context, ILogger<BookingLockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Thử khóa sân cho việc đặt
    /// </summary>
    /// <returns>Lock ID nếu thành công, null nếu sân đã bị khóa</returns>
    public async Task<BookingLockResult> TryLockAsync(int maSan, DateTime ngaySd, int maKhungGio, int? maKh, string sessionId)
    {
        // Dọn dẹp các khóa hết hạn
        await CleanupExpiredLocksAsync();

        // Kiểm tra xem sân đã được đặt chưa
        var existingBooking = await _context.DatSan
            .AnyAsync(d => d.MaSan == maSan 
                && d.NgaySd.Date == ngaySd.Date 
                && d.MaKhungGio == maKhungGio 
                && d.TrangThai != "Đã hủy");

        if (existingBooking)
        {
            return new BookingLockResult
            {
                Success = false,
                Message = "Sân đã được đặt trong khung giờ này"
            };
        }

        // Kiểm tra xem có ai đang giữ khóa không
        var existingLock = await _context.KhoaSan
            .FirstOrDefaultAsync(k => k.MaSan == maSan 
                && k.NgaySd.Date == ngaySd.Date 
                && k.MaKhungGio == maKhungGio 
                && k.TrangThai == "DangGiu"
                && k.ThoiGianHetHan > DateTime.Now);

        if (existingLock != null)
        {
            // Nếu là cùng session thì cho phép
            if (existingLock.SessionId == sessionId)
            {
                // Gia hạn thời gian khóa
                existingLock.ThoiGianHetHan = DateTime.Now.AddMinutes(DEFAULT_LOCK_DURATION_MINUTES);
                await _context.SaveChangesAsync();

                return new BookingLockResult
                {
                    Success = true,
                    LockId = existingLock.MaKhoaSan,
                    ExpiresAt = existingLock.ThoiGianHetHan,
                    Message = "Đã gia hạn khóa sân"
                };
            }

            // Có người khác đang giữ
            var remainingSeconds = (existingLock.ThoiGianHetHan - DateTime.Now).TotalSeconds;
            return new BookingLockResult
            {
                Success = false,
                Message = $"Sân đang được người khác đặt. Vui lòng thử lại sau {remainingSeconds:0} giây",
                RemainingSeconds = (int)remainingSeconds
            };
        }

        // Tạo khóa mới
        var newLock = new KhoaSan
        {
            MaSan = maSan,
            NgaySd = ngaySd.Date,
            MaKhungGio = maKhungGio,
            MaKh = maKh,
            SessionId = sessionId,
            ThoiGianKhoa = DateTime.Now,
            ThoiGianHetHan = DateTime.Now.AddMinutes(DEFAULT_LOCK_DURATION_MINUTES),
            TrangThai = "DangGiu"
        };

        _context.KhoaSan.Add(newLock);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Locked field {MaSan} for date {NgaySd}, slot {MaKhungGio}, session {SessionId}", 
            maSan, ngaySd.Date, maKhungGio, sessionId);

        return new BookingLockResult
        {
            Success = true,
            LockId = newLock.MaKhoaSan,
            ExpiresAt = newLock.ThoiGianHetHan,
            Message = "Đã giữ sân thành công"
        };
    }

    /// <summary>
    /// Gia hạn khóa
    /// </summary>
    public async Task<bool> ExtendLockAsync(int lockId, string sessionId, int additionalMinutes = 5)
    {
        var lockItem = await _context.KhoaSan
            .FirstOrDefaultAsync(k => k.MaKhoaSan == lockId && k.SessionId == sessionId);

        if (lockItem == null || lockItem.TrangThai != "DangGiu")
        {
            return false;
        }

        lockItem.ThoiGianHetHan = DateTime.Now.AddMinutes(additionalMinutes);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Giải phóng khóa
    /// </summary>
    public async Task<bool> ReleaseLockAsync(int lockId, string sessionId)
    {
        var lockItem = await _context.KhoaSan
            .FirstOrDefaultAsync(k => k.MaKhoaSan == lockId && k.SessionId == sessionId);

        if (lockItem == null)
        {
            return false;
        }

        lockItem.TrangThai = "DaHuy";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Released lock {LockId}", lockId);

        return true;
    }

    /// <summary>
    /// Giải phóng khóa theo thông tin slot
    /// </summary>
    public async Task<bool> ReleaseLockBySlotAsync(int maSan, DateTime ngaySd, int maKhungGio, string sessionId)
    {
        var lockItem = await _context.KhoaSan
            .FirstOrDefaultAsync(k => k.MaSan == maSan 
                && k.NgaySd.Date == ngaySd.Date 
                && k.MaKhungGio == maKhungGio
                && k.SessionId == sessionId
                && k.TrangThai == "DangGiu");

        if (lockItem == null)
        {
            return false;
        }

        lockItem.TrangThai = "DaHuy";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Released lock for field {MaSan}, date {NgaySd}, slot {MaKhungGio}", 
            maSan, ngaySd.Date, maKhungGio);

        return true;
    }

    /// <summary>
    /// Đánh dấu khóa đã thanh toán thành công
    /// </summary>
    public async Task<bool> CompleteLockAsync(int lockId, int maDatSan)
    {
        var lockItem = await _context.KhoaSan.FindAsync(lockId);

        if (lockItem == null)
        {
            return false;
        }

        lockItem.TrangThai = "DaThanhToan";
        lockItem.MaDatSan = maDatSan;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Kiểm tra sân có khả dụng không
    /// </summary>
    public async Task<bool> IsSlotAvailableAsync(int maSan, DateTime ngaySd, int maKhungGio, string? excludeSessionId = null)
    {
        // Dọn dẹp khóa hết hạn
        await CleanupExpiredLocksAsync();

        // Kiểm tra đã đặt chưa
        var booked = await _context.DatSan
            .AnyAsync(d => d.MaSan == maSan 
                && d.NgaySd.Date == ngaySd.Date 
                && d.MaKhungGio == maKhungGio 
                && d.TrangThai != "Đã hủy");

        if (booked) return false;

        // Kiểm tra có khóa không
        var lockedQuery = _context.KhoaSan
            .Where(k => k.MaSan == maSan 
                && k.NgaySd.Date == ngaySd.Date 
                && k.MaKhungGio == maKhungGio 
                && k.TrangThai == "DangGiu"
                && k.ThoiGianHetHan > DateTime.Now);

        if (!string.IsNullOrEmpty(excludeSessionId))
        {
            lockedQuery = lockedQuery.Where(k => k.SessionId != excludeSessionId);
        }

        var locked = await lockedQuery.AnyAsync();

        return !locked;
    }

    /// <summary>
    /// Lấy thông tin các slot đang bị khóa
    /// </summary>
    public async Task<List<LockedSlotInfo>> GetLockedSlotsAsync(int maSan, DateTime ngaySd)
    {
        await CleanupExpiredLocksAsync();

        return await _context.KhoaSan
            .Where(k => k.MaSan == maSan 
                && k.NgaySd.Date == ngaySd.Date 
                && k.TrangThai == "DangGiu"
                && k.ThoiGianHetHan > DateTime.Now)
            .Select(k => new LockedSlotInfo
            {
                MaKhungGio = k.MaKhungGio,
                ThoiGianHetHan = k.ThoiGianHetHan,
                RemainingSeconds = (int)(k.ThoiGianHetHan - DateTime.Now).TotalSeconds
            })
            .ToListAsync();
    }

    /// <summary>
    /// Dọn dẹp các khóa hết hạn
    /// </summary>
    private async Task CleanupExpiredLocksAsync()
    {
        var expiredLocks = await _context.KhoaSan
            .Where(k => k.TrangThai == "DangGiu" && k.ThoiGianHetHan <= DateTime.Now)
            .ToListAsync();

        foreach (var lockItem in expiredLocks)
        {
            lockItem.TrangThai = "DaHuy";
        }

        if (expiredLocks.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired locks", expiredLocks.Count);
        }
    }
}

/// <summary>
/// Kết quả khóa sân
/// </summary>
public class BookingLockResult
{
    public bool Success { get; set; }
    public int LockId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Message { get; set; } = null!;
    public int RemainingSeconds { get; set; }
}

/// <summary>
/// Thông tin slot đang bị khóa
/// </summary>
public class LockedSlotInfo
{
    public int MaKhungGio { get; set; }
    public DateTime ThoiGianHetHan { get; set; }
    public int RemainingSeconds { get; set; }
}
