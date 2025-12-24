using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SanBong.Models;

namespace SanBong.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaiKhoan> TaiKhoan { get; set; }
    public DbSet<KhachHang> KhachHang { get; set; }
    public DbSet<NhanVien> NhanVien { get; set; }
    public DbSet<LoaiSan> LoaiSan { get; set; }
    public DbSet<Models.SanBong> SanBong { get; set; }
    public DbSet<KhungGio> KhungGio { get; set; }
    public DbSet<DatSan> DatSan { get; set; }
    public DbSet<DichVu> DichVu { get; set; }
    public DbSet<ChiTietDichVu> ChiTietDichVu { get; set; }
    public DbSet<ThanhToan> ThanhToan { get; set; }
    public DbSet<DanhGia> DanhGia { get; set; }
    public DbSet<LienHe> LienHe { get; set; }
    public DbSet<CaLam> CaLam { get; set; }
    public DbSet<PhanCa> PhanCa { get; set; }
    
    // Bảng mới: Hóa đơn và Chi tiết hóa đơn
    public DbSet<HoaDon> HoaDon { get; set; }
    public DbSet<ChiTietHoaDon> ChiTietHoaDon { get; set; }
    
    // Bảng mới: Đơn hàng và Chi tiết đơn hàng
    public DbSet<DonHang> DonHang { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
    
    // Bảng mới: Giao dịch thanh toán online
    public DbSet<GiaoDichThanhToan> GiaoDichThanhToan { get; set; }
    
    // Bảng mới: Khóa sân (lock khi đang thanh toán)
    public DbSet<KhoaSan> KhoaSan { get; set; }
    
    // Bảng mới: Ngày lễ Việt Nam (giảm giá 40%)
    public DbSet<NgayLe> NgayLe { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TaiKhoan
        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.MaTk);
            entity.ToTable("TaiKhoan");
            entity.HasIndex(e => e.TenDangNhap, "IX_TaiKhoan_TenDangNhap").IsUnique();
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.TrangThai).HasDefaultValue(1);
        });

        // KhachHang
        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh);
            entity.ToTable("KhachHang");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.Cccd).HasColumnName("CCCD");
            entity.Property(e => e.DiemTichLuy).HasDefaultValue(0);
            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.KhachHangs).HasForeignKey(d => d.MaTk);
        });

        // NhanVien
        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv);
            entity.ToTable("NhanVien");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.NhanViens).HasForeignKey(d => d.MaTk);
        });

        // LoaiSan
        modelBuilder.Entity<LoaiSan>(entity =>
        {
            entity.HasKey(e => e.MaLoai);
            entity.ToTable("LoaiSan");
        });

        // SanBong
        modelBuilder.Entity<Models.SanBong>(entity =>
        {
            entity.HasKey(e => e.MaSan);
            entity.ToTable("SanBong");
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            entity.HasOne(d => d.MaLoaiNavigation).WithMany(p => p.SanBongs).HasForeignKey(d => d.MaLoai);
        });

        // KhungGio
        modelBuilder.Entity<KhungGio>(entity =>
        {
            entity.HasKey(e => e.MaKhungGio);
            entity.ToTable("KhungGio");
            entity.Property(e => e.HeSoGia).HasDefaultValue(1.0m);
        });

        // DatSan
        modelBuilder.Entity<DatSan>(entity =>
        {
            entity.HasKey(e => e.MaDatSan);
            entity.ToTable("DatSan");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgaySd).HasColumnName("NgaySD");
            entity.Property(e => e.ThoiGianDat).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chờ xác nhận");
            entity.Property(e => e.GiaGoc).HasDefaultValue(0m);
            entity.Property(e => e.GiamGiaNgayLe).HasDefaultValue(0m);
            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DatSans).HasForeignKey(d => d.MaKh);
            entity.HasOne(d => d.MaSanNavigation).WithMany(p => p.DatSans).HasForeignKey(d => d.MaSan);
            entity.HasOne(d => d.MaKhungGioNavigation).WithMany(p => p.DatSans).HasForeignKey(d => d.MaKhungGio);
            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.DatSans).HasForeignKey(d => d.MaNv);
            entity.HasOne(d => d.MaNgayLeNavigation).WithMany(p => p.DatSans).HasForeignKey(d => d.MaNgayLe);
        });

        // DichVu
        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDv);
            entity.ToTable("DichVu");
            entity.Property(e => e.MaDv).HasColumnName("MaDV");
            entity.Property(e => e.TenDv).HasColumnName("TenDV");
            entity.Property(e => e.SoLuongTon).HasDefaultValue(0);
        });

        // ChiTietDichVu
        modelBuilder.Entity<ChiTietDichVu>(entity =>
        {
            entity.HasKey(e => e.MaCtdv);
            entity.ToTable("ChiTietDichVu");
            entity.Property(e => e.MaCtdv).HasColumnName("MaCTDV");
            entity.Property(e => e.MaDv).HasColumnName("MaDV");
            entity.HasOne(d => d.MaDatSanNavigation).WithMany(p => p.ChiTietDichVus).HasForeignKey(d => d.MaDatSan);
            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.ChiTietDichVus).HasForeignKey(d => d.MaDv);
        });

        // ThanhToan
        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaTt);
            entity.ToTable("ThanhToan");
            entity.Property(e => e.MaTt).HasColumnName("MaTT");
            entity.Property(e => e.NgayThanhToan).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đã thanh toán");
            entity.HasOne(d => d.MaDatSanNavigation).WithMany(p => p.ThanhToans).HasForeignKey(d => d.MaDatSan);
            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ThanhToans).HasForeignKey(d => d.MaHoaDon);
        });

        // HoaDon
        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHoaDon);
            entity.ToTable("HoaDon");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayLap).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chưa thanh toán");
            entity.Property(e => e.GiamGia).HasDefaultValue(0);
            entity.Property(e => e.ThueVat).HasDefaultValue(0);
            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.HoaDons).HasForeignKey(d => d.MaKh);
            entity.HasOne(d => d.MaDatSanNavigation).WithMany(p => p.HoaDons).HasForeignKey(d => d.MaDatSan);
            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.HoaDons).HasForeignKey(d => d.MaNv);
        });

        // ChiTietHoaDon
        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => e.MaChiTietHd);
            entity.ToTable("ChiTietHoaDon");
            entity.Property(e => e.MaChiTietHd).HasColumnName("MaChiTietHD");
            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ChiTietHoaDons).HasForeignKey(d => d.MaHoaDon);
        });

        // DonHang
        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.MaDonHang);
            entity.ToTable("DonHang");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayDat).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chờ xác nhận");
            entity.Property(e => e.GiamGia).HasDefaultValue(0);
            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DonHangs).HasForeignKey(d => d.MaKh);
            entity.HasOne(d => d.MaDatSanNavigation).WithMany(p => p.DonHangs).HasForeignKey(d => d.MaDatSan);
            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.DonHangs).HasForeignKey(d => d.MaNv);
        });

        // ChiTietDonHang
        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.MaChiTietDh);
            entity.ToTable("ChiTietDonHang");
            entity.Property(e => e.MaChiTietDh).HasColumnName("MaChiTietDH");
            entity.Property(e => e.MaDv).HasColumnName("MaDV");
            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ChiTietDonHangs).HasForeignKey(d => d.MaDonHang);
            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.ChiTietDonHangs).HasForeignKey(d => d.MaDv);
        });

        // GiaoDichThanhToan
        modelBuilder.Entity<GiaoDichThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaGiaoDich);
            entity.ToTable("GiaoDichThanhToan");
            entity.Property(e => e.ThoiGianTao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Pending");
            entity.HasOne(d => d.MaThanhToanNavigation).WithMany(p => p.GiaoDichThanhToans).HasForeignKey(d => d.MaThanhToan);
        });

        // DanhGia
        modelBuilder.Entity<DanhGia>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia);
            entity.ToTable("DanhGia");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.NgayDanhGia).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(d => d.MaDatSanNavigation).WithMany(p => p.DanhGias).HasForeignKey(d => d.MaDatSan);
            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DanhGias).HasForeignKey(d => d.MaKh);
        });

        // LienHe
        modelBuilder.Entity<LienHe>(entity =>
        {
            entity.HasKey(e => e.MaLienHe);
            entity.ToTable("LienHe");
            entity.Property(e => e.NgayGui).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chưa xử lý");
        });

        // CaLam
        modelBuilder.Entity<CaLam>(entity =>
        {
            entity.HasKey(e => e.MaCa);
            entity.ToTable("CaLam");
        });

        // PhanCa
        modelBuilder.Entity<PhanCa>(entity =>
        {
            entity.HasKey(e => e.MaPhanCa);
            entity.ToTable("PhanCa");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đang chờ");
            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.PhanCas).HasForeignKey(d => d.MaNv);
            entity.HasOne(d => d.MaCaNavigation).WithMany(p => p.PhanCas).HasForeignKey(d => d.MaCa);
        });

        // KhoaSan
        modelBuilder.Entity<KhoaSan>(entity =>
        {
            entity.HasKey(e => e.MaKhoaSan);
            entity.ToTable("KhoaSan");
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.ThoiGianKhoa).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TrangThai).HasDefaultValue("DangGiu");
            entity.HasIndex(e => new { e.MaSan, e.NgaySd, e.MaKhungGio, e.TrangThai });
            entity.HasOne(d => d.MaSanNavigation).WithMany().HasForeignKey(d => d.MaSan);
            entity.HasOne(d => d.MaKhungGioNavigation).WithMany().HasForeignKey(d => d.MaKhungGio);
            entity.HasOne(d => d.MaKhNavigation).WithMany().HasForeignKey(d => d.MaKh);
            entity.HasOne(d => d.MaDatSanNavigation).WithMany().HasForeignKey(d => d.MaDatSan);
        });

        // NgayLe - Bảng ngày lễ Việt Nam (giảm giá 40%)
        modelBuilder.Entity<NgayLe>(entity =>
        {
            entity.HasKey(e => e.MaNgayLe);
            entity.ToTable("NgayLe");
            entity.Property(e => e.HeSoGiamGia).HasDefaultValue(0.6m);
            entity.Property(e => e.LoaiLich).HasDefaultValue("DuongLich");
            entity.Property(e => e.TrangThai).HasDefaultValue(1);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
