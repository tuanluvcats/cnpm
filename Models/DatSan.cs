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

    public decimal? TongTien { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public int? MaNv { get; set; }

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual SanBong? MaSanNavigation { get; set; }

    public virtual KhungGio? MaKhungGioNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual ICollection<ChiTietDichVu> ChiTietDichVus { get; set; } = new List<ChiTietDichVu>();

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();

    public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
}
