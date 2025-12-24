using SanBong.Data;
using SanBong.Models;
using Microsoft.EntityFrameworkCore;

namespace SanBong.Services;

/// <summary>
/// Service xử lý logic giảm giá ngày lễ Việt Nam
/// </summary>
public class HolidayDiscountService
{
    private readonly AppDbContext _context;

    public HolidayDiscountService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra xem một ngày có phải ngày lễ không và trả về thông tin ngày lễ
    /// </summary>
    /// <param name="date">Ngày cần kiểm tra</param>
    /// <returns>NgayLe nếu là ngày lễ, null nếu không</returns>
    public async Task<NgayLe?> GetHolidayAsync(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var monthDayStr = date.ToString("MM-dd");

        // Kiểm tra ngày lễ có ngày cố định theo năm (dương lịch hoặc âm lịch đã chuyển đổi)
        var holiday = await _context.NgayLe
            .Where(n => n.TrangThai == 1)
            .Where(n => 
                // Ngày lễ có năm cụ thể (VD: Tết Nguyên Đán 2025)
                (n.NgayBatDau.Length == 10 && 
                 string.Compare(n.NgayBatDau, dateStr) <= 0 && 
                 (n.NgayKetThuc == null || string.Compare(n.NgayKetThuc, dateStr) >= 0))
                ||
                // Ngày lễ cố định hàng năm (VD: 01-01, 12-25)
                (n.NgayBatDau.Length == 5 && n.LoaiLich == "DuongLich" &&
                 string.Compare(n.NgayBatDau, monthDayStr) <= 0 && 
                 (n.NgayKetThuc == null || string.Compare(n.NgayKetThuc, monthDayStr) >= 0))
            )
            .FirstOrDefaultAsync();

        return holiday;
    }

    /// <summary>
    /// Kiểm tra xem một ngày có phải ngày lễ không
    /// </summary>
    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        return await GetHolidayAsync(date) != null;
    }

    /// <summary>
    /// Tính giá sau khi áp dụng giảm giá ngày lễ
    /// </summary>
    /// <param name="originalPrice">Giá gốc</param>
    /// <param name="date">Ngày sử dụng sân</param>
    /// <returns>Tuple (giá sau giảm, số tiền giảm, ngày lễ nếu có)</returns>
    public async Task<(decimal FinalPrice, decimal DiscountAmount, NgayLe? Holiday)> CalculateHolidayPriceAsync(decimal originalPrice, DateTime date)
    {
        var holiday = await GetHolidayAsync(date);
        
        if (holiday == null)
        {
            return (originalPrice, 0, null);
        }

        var discountedPrice = originalPrice * holiday.HeSoGiamGia;
        var discountAmount = originalPrice - discountedPrice;

        return (discountedPrice, discountAmount, holiday);
    }

    /// <summary>
    /// Lấy danh sách tất cả ngày lễ đang hoạt động
    /// </summary>
    public async Task<List<NgayLe>> GetActiveHolidaysAsync()
    {
        return await _context.NgayLe
            .Where(n => n.TrangThai == 1)
            .OrderBy(n => n.NgayBatDau)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách ngày lễ trong khoảng thời gian
    /// </summary>
    public async Task<List<NgayLe>> GetHolidaysInRangeAsync(DateTime startDate, DateTime endDate)
    {
        var holidays = new List<NgayLe>();
        var allHolidays = await GetActiveHolidaysAsync();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var holiday = await GetHolidayAsync(date);
            if (holiday != null && !holidays.Any(h => h.MaNgayLe == holiday.MaNgayLe))
            {
                holidays.Add(holiday);
            }
        }

        return holidays;
    }

    /// <summary>
    /// Lấy thông tin giảm giá cho hiển thị trên UI
    /// </summary>
    public async Task<HolidayDiscountInfo?> GetDiscountInfoAsync(DateTime date)
    {
        var holiday = await GetHolidayAsync(date);
        if (holiday == null) return null;

        return new HolidayDiscountInfo
        {
            HolidayName = holiday.TenNgayLe,
            DiscountPercent = (int)((1 - holiday.HeSoGiamGia) * 100),
            Description = holiday.MoTa ?? $"Giảm giá {(int)((1 - holiday.HeSoGiamGia) * 100)}% nhân dịp {holiday.TenNgayLe}"
        };
    }
}

/// <summary>
/// DTO chứa thông tin giảm giá ngày lễ để hiển thị
/// </summary>
public class HolidayDiscountInfo
{
    public string HolidayName { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public string Description { get; set; } = string.Empty;
}
