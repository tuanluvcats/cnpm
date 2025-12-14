using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class ChiTietDichVu
{
    public int MaCtdv { get; set; }

    public int? MaDatSan { get; set; }

    public int? MaDv { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual DatSan? MaDatSanNavigation { get; set; }

    public virtual DichVu? MaDvNavigation { get; set; }
}
