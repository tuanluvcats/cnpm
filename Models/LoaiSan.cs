using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class LoaiSan
{
    public int MaLoai { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<SanBong> SanBongs { get; set; } = new List<SanBong>();
}
