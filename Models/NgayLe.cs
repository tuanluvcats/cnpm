using System;
using System.Collections.Generic;

namespace SanBong.Models;

/// <summary>
/// Bảng Ngày Lễ Việt Nam - Dùng để áp dụng giảm giá 40% vào các dịp lễ
/// </summary>
public partial class NgayLe
{
    public int MaNgayLe { get; set; }

    public string TenNgayLe { get; set; } = null!;

    /// <summary>
    /// Ngày bắt đầu - Format: MM-DD cho dương lịch cố định, YYYY-MM-DD cho âm lịch đã chuyển đổi
    /// </summary>
    public string NgayBatDau { get; set; } = null!;

    /// <summary>
    /// Ngày kết thúc - Null nếu chỉ 1 ngày
    /// </summary>
    public string? NgayKetThuc { get; set; }

    /// <summary>
    /// Loại lịch: DuongLich hoặc AmLich
    /// </summary>
    public string LoaiLich { get; set; } = "DuongLich";

    /// <summary>
    /// Hệ số giảm giá: 0.6 = giá chỉ còn 60% (giảm 40%)
    /// </summary>
    public decimal HeSoGiamGia { get; set; } = 0.6m;

    public string? MoTa { get; set; }

    /// <summary>
    /// Trạng thái: 1 = Hoạt động, 0 = Không hoạt động
    /// </summary>
    public int TrangThai { get; set; } = 1;

    // Navigation property
    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
}
