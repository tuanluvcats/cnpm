using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class DonHang
{
    public int MaDonHang { get; set; }

    public string MaDonHangCode { get; set; } = null!; // Mã đơn hàng hiển thị (VD: DH20231223001)

    public int? MaKh { get; set; }

    public int? MaDatSan { get; set; } // Liên kết với đặt sân (nếu có)

    public DateTime NgayDat { get; set; }

    public decimal TongTien { get; set; }

    public decimal GiamGia { get; set; }

    public decimal ThanhToan { get; set; } // Số tiền phải thanh toán

    public string? TrangThai { get; set; } // Chờ xác nhận, Đã xác nhận, Đang xử lý, Hoàn thành, Đã hủy

    public string? GhiChu { get; set; }

    public int? MaNv { get; set; } // Nhân viên xử lý

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
}
