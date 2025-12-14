using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class NhanVien
{
    public int MaNv { get; set; }

    public string HoTen { get; set; } = null!;

    public DateTime? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? ChucVu { get; set; }

    public int? MaTk { get; set; }

    public virtual TaiKhoan? MaTkNavigation { get; set; }

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
}
