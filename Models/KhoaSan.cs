using System;

namespace SanBong.Models;

/// <summary>
/// Bảng khóa sân - Ngăn chặn đặt trùng khi đang thanh toán
/// </summary>
public class KhoaSan
{
    public int MaKhoaSan { get; set; }
    
    public int MaSan { get; set; }
    
    public DateTime NgaySd { get; set; }
    
    public int MaKhungGio { get; set; }
    
    public int? MaKh { get; set; } // Khách hàng đang giữ khóa
    
    public string? SessionId { get; set; } // Session ID để xác thực
    
    public DateTime ThoiGianKhoa { get; set; }
    
    public DateTime ThoiGianHetHan { get; set; } // Tự động mở khóa sau thời gian này
    
    public string? TrangThai { get; set; } // DangGiu, DaThanhToan, DaHuy
    
    public int? MaDatSan { get; set; } // Liên kết với đơn đặt sân (nếu đã tạo)
    
    public virtual Models.SanBong? MaSanNavigation { get; set; }
    
    public virtual KhungGio? MaKhungGioNavigation { get; set; }
    
    public virtual KhachHang? MaKhNavigation { get; set; }
    
    public virtual DatSan? MaDatSanNavigation { get; set; }
}
