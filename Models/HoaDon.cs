using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class HoaDon
{
    public int MaHoaDon { get; set; }

    public int? MaKh { get; set; }

    public int? MaDatSan { get; set; }

    public string MaHoaDonCode { get; set; } = null!; // Mã hóa đơn hiển thị (VD: HD20231223001)

    public DateTime NgayLap { get; set; }

    public decimal TongTienSan { get; set; } // Tổng tiền thuê sân

    public decimal TongTienDichVu { get; set; } // Tổng tiền dịch vụ

    public decimal GiamGia { get; set; } // Số tiền giảm giá

    public decimal ThueVat { get; set; } // Thuế VAT (nếu có)

    public decimal TongCong { get; set; } // Tổng cộng phải thanh toán

    public string? TrangThai { get; set; } // Chưa thanh toán, Đã thanh toán, Đã hủy

    public string? GhiChu { get; set; }

    public int? MaNv { get; set; } // Nhân viên lập hóa đơn

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
