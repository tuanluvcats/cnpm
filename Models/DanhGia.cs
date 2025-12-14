using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class DanhGia
{
    public int MaDanhGia { get; set; }

    public int? MaDatSan { get; set; }

    public int? MaKh { get; set; }

    public int? DiemDanhGia { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }

    public virtual KhachHang? MaKhNavigation { get; set; }
}
