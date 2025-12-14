using System;
using System.Collections.Generic;

namespace SanBong.Models;

public partial class DichVu
{
    public int MaDv { get; set; }

    public string TenDv { get; set; } = null!;

    public decimal DonGia { get; set; }

    public string? DonVi { get; set; }

    public string? MoTa { get; set; }

    public string? HinhAnh { get; set; }

    public int? SoLuongTon { get; set; }

    public virtual ICollection<ChiTietDichVu> ChiTietDichVus { get; set; } = new List<ChiTietDichVu>();
}
