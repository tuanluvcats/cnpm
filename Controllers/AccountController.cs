using Microsoft.AspNetCore.Mvc;
using SanBong.Models; 
using SanBong.Data;   
using System.Linq;

namespace SanBong.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string tendangnhap, string matkhau, string returnUrl = null)
        {
            // Tìm tài khoản (Lưu ý: SQLite phân biệt hoa thường, nên dùng ToLower nếu cần)
            var tk = _context.TaiKhoan.FirstOrDefault(t => t.TenDangNhap == tendangnhap && t.MatKhau == matkhau);
            
            if (tk != null)
            {
                // Lưu Session
                HttpContext.Session.SetString("MaTK", tk.MaTk.ToString());
                HttpContext.Session.SetString("Role", tk.VaiTro);
                HttpContext.Session.SetString("UserName", tk.TenDangNhap);

                if (tk.VaiTro == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (tk.VaiTro == "NhanVien")
                {
                    // Lấy thông tin nhân viên
                    var nv = _context.NhanVien.FirstOrDefault(n => n.MaTk == tk.MaTk);
                    if (nv != null)
                    {
                        HttpContext.Session.SetInt32("MaNV", nv.MaNv);
                        HttpContext.Session.SetString("UserName", nv.HoTen);
                    }
                    return RedirectToAction("Index", "DatSan");
                }
                else
                {
                    // Lấy tên khách hàng để hiển thị cho đẹp
                    var kh = _context.KhachHang.FirstOrDefault(k => k.MaTk == tk.MaTk);
                    if (kh != null)
                    {
                        HttpContext.Session.SetInt32("MaKH", kh.MaKh);
                        HttpContext.Session.SetString("UserName", kh.HoTen);
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa sạch session
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string hoten, string diachi, string dienthoai, string email, string cccd, string tendangnhap, string matkhau)
        {
            if (_context.TaiKhoan.Any(t => t.TenDangNhap == tendangnhap))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }

            // 1. Tạo Tài khoản
            var tk = new TaiKhoan
            {
                TenDangNhap = tendangnhap,
                MatKhau = matkhau,
                VaiTro = "KhachHang",
                TrangThai = 1 // 1 là true trong SQLite
            };
            _context.TaiKhoan.Add(tk);
            _context.SaveChanges();

            // 2. Tạo Khách hàng
            var kh = new KhachHang
            {
                HoTen = hoten,
                DiaChi = diachi,
                DienThoai = dienthoai,
                Email = email,
                Cccd = cccd,
                DiemTichLuy = 0,
                MaTk = tk.MaTk
            };
            _context.KhachHang.Add(kh);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }
    }
}