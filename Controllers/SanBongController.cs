using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Models;

namespace SanBong.Controllers
{
    public class SanBongController : Controller
    {
        private readonly AppDbContext _context;

        public SanBongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: SanBong
        public async Task<IActionResult> Index(int? maLoai)
        {
            var sanBongs = _context.SanBong.Include(s => s.MaLoaiNavigation).AsQueryable();

            if (maLoai.HasValue && maLoai.Value > 0)
            {
                sanBongs = sanBongs.Where(s => s.MaLoai == maLoai.Value);
            }

            ViewBag.LoaiSan = await _context.LoaiSan.ToListAsync();
            ViewBag.SelectedMaLoai = maLoai;
            return View(await sanBongs.ToListAsync());
        }

        // GET: SanBong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanBong = await _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaSan == id);
            
            if (sanBong == null)
            {
                return NotFound();
            }

            return View(sanBong);
        }

        // GET: SanBong/Create
        public IActionResult Create()
        {
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            return View();
        }

        // POST: SanBong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSan,TenSan,MaLoai,GiaTheoGio,TrangThai,ViTri,MoTa,HinhAnh")] Models.SanBong sanBong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(sanBong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            return View(sanBong);
        }

        // GET: SanBong/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanBong = await _context.SanBong.FindAsync(id);
            if (sanBong == null)
            {
                return NotFound();
            }
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            return View(sanBong);
        }

        // POST: SanBong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSan,TenSan,MaLoai,GiaTheoGio,TrangThai,ViTri,MoTa,HinhAnh")] Models.SanBong sanBong)
        {
            if (id != sanBong.MaSan)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sanBong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanBongExists(sanBong.MaSan))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.LoaiSan = _context.LoaiSan.ToList();
            return View(sanBong);
        }

        // GET: SanBong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanBong = await _context.SanBong
                .Include(s => s.MaLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaSan == id);
            
            if (sanBong == null)
            {
                return NotFound();
            }

            return View(sanBong);
        }

        // POST: SanBong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanBong = await _context.SanBong.FindAsync(id);
            if (sanBong != null)
            {
                _context.SanBong.Remove(sanBong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SanBongExists(int id)
        {
            return _context.SanBong.Any(e => e.MaSan == id);
        }
    }
}
