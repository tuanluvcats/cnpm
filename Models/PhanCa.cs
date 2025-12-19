using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class PhanCa
{
    public int MaPhanCa { get; set; }

    public int MaNv { get; set; }

    public int MaCa { get; set; }

    public DateTime NgayLam { get; set; }

    public string TrangThai { get; set; } = "Đang chờ";

    public string? GhiChu { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual CaLam? MaCaNavigation { get; set; }
}
