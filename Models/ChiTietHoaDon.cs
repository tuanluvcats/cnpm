using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class ChiTietHoaDon
{
    public int MaChiTietHd { get; set; }

    public int? MaHoaDon { get; set; }

    public string LoaiMuc { get; set; } = null!; // "ThueSan", "DichVu"

    public string TenMuc { get; set; } = null!; // Tên sân hoặc tên dịch vụ

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal ThanhTien { get; set; }

    public string? GhiChu { get; set; }

    public virtual HoaDon? MaHoaDonNavigation { get; set; }
}
