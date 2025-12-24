using System;
using System.Collections.Generic;

namespace SanBong.Models;

/// <summary>
/// Bảng lưu trữ giao dịch thanh toán online (MoMo, ZaloPay, VNPay, etc.)
/// </summary>
public partial class GiaoDichThanhToan
{
    public int MaGiaoDich { get; set; }

    public int? MaThanhToan { get; set; } // Liên kết với bảng ThanhToan

    public string MaGiaoDichCode { get; set; } = null!; // Mã giao dịch nội bộ

    public string? MaGiaoDichDoiTac { get; set; } // Mã giao dịch từ đối tác (MoMo, ZaloPay)

    public string NhaCungCap { get; set; } = null!; // "MoMo", "ZaloPay", "VNPay", "Banking"

    public decimal SoTien { get; set; }

    public string? MoTa { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianCapNhat { get; set; }

    public string TrangThai { get; set; } = null!; // "Pending", "Success", "Failed", "Cancelled", "Refunded"

    public string? RequestData { get; set; } // JSON data gửi đi

    public string? ResponseData { get; set; } // JSON data nhận về

    public string? ErrorMessage { get; set; } // Thông báo lỗi (nếu có)

    public string? CallbackUrl { get; set; } // URL callback

    public string? ReturnUrl { get; set; } // URL redirect sau khi thanh toán

    public string? IpAddress { get; set; } // IP của khách hàng

    public virtual ThanhToan? MaThanhToanNavigation { get; set; }
}
