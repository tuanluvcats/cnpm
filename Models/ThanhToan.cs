using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class ThanhToan
{
    public int MaTt { get; set; }

    public int? MaDatSan { get; set; }

    public int? MaHoaDon { get; set; } // Liên kết với hóa đơn

    public string PhuongThuc { get; set; } = null!; // TienMat, ChuyenKhoan, MoMo, ZaloPay, VNPay

    public decimal SoTien { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public string? TrangThai { get; set; } // ChoDuyet, DaThanhToan, ThatBai, DaHuy, DaHoanTien

    public string? MaGiaoDich { get; set; } // Mã giao dịch thanh toán online

    public string? GhiChu { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }

    public virtual HoaDon? MaHoaDonNavigation { get; set; }

    public virtual ICollection<GiaoDichThanhToan> GiaoDichThanhToans { get; set; } = new List<GiaoDichThanhToan>();
}
