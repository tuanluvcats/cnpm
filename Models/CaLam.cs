using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class CaLam
{
    public int MaCa { get; set; }

    public string TenCa { get; set; } = null!;

    public TimeSpan GioBatDau { get; set; }

    public TimeSpan GioKetThuc { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<PhanCa> PhanCas { get; set; } = new List<PhanCa>();
}
