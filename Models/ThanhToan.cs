using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class ThanhToan
{
    public int MaTt { get; set; }

    public int? MaDatSan { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public decimal SoTien { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public string? TrangThai { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }
}
