using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class ChiTietDonHang
{
    public int MaChiTietDh { get; set; }

    public int? MaDonHang { get; set; }

    public int? MaDv { get; set; } // Mã dịch vụ/sản phẩm

    public string TenSanPham { get; set; } = null!;

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal ThanhTien { get; set; }

    public string? GhiChu { get; set; }

    public virtual DonHang? MaDonHangNavigation { get; set; }

    public virtual DichVu? MaDvNavigation { get; set; }
}
