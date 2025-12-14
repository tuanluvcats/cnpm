using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class KhungGio
{
    public int MaKhungGio { get; set; }

    public TimeSpan GioBatDau { get; set; }

    public TimeSpan GioKetThuc { get; set; }

    public decimal? HeSoGia { get; set; }

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
}
