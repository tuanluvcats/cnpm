using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class LienHe
{
    public int MaLienHe { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? TieuDe { get; set; }

    public string NoiDung { get; set; } = null!;

    public string? NgayGui { get; set; }

    public string? TrangThai { get; set; }
}
