using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class DatSan
{
    public int MaDatSan { get; set; }

    public int? MaKh { get; set; }

    public int? MaSan { get; set; }

    public int? MaKhungGio { get; set; }

    public DateTime NgayDat { get; set; }

    public DateTime NgaySd { get; set; }

    public DateTime? ThoiGianDat { get; set; }

    /// <summary>
    /// Giá gốc trước khi áp dụng giảm giá ngày lễ
    /// </summary>
    public decimal? GiaGoc { get; set; }

    /// <summary>
    /// Số tiền được giảm do ngày lễ
    /// </summary>
    public decimal? GiamGiaNgayLe { get; set; }

    public decimal? TongTien { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public int? MaNv { get; set; }

    /// <summary>
    /// Mã ngày lễ được áp dụng (nếu có)
    /// </summary>
    public int? MaNgayLe { get; set; }

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual SanBong? MaSanNavigation { get; set; }

    public virtual KhungGio? MaKhungGioNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual NgayLe? MaNgayLeNavigation { get; set; }

    public virtual ICollection<ChiTietDichVu> ChiTietDichVus { get; set; } = new List<ChiTietDichVu>();

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();

    public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
