using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class SanBong
{
    public int MaSan { get; set; }

    public string TenSan { get; set; } = null!;

    public int? MaLoai { get; set; }

    public decimal GiaTheoGio { get; set; }

    public string? TrangThai { get; set; }

    public string? ViTri { get; set; }

    public string? MoTa { get; set; }

    public string? HinhAnh { get; set; }

    public virtual LoaiSan? MaLoaiNavigation { get; set; }

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();
}
