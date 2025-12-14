using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class KhachHang
{
    public int MaKh { get; set; }

    public string HoTen { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string DienThoai { get; set; } = null!;

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public int? DiemTichLuy { get; set; }

    public int? MaTk { get; set; }

    public virtual TaiKhoan? MaTkNavigation { get; set; }

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();

    public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
}
