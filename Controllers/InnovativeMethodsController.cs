using EduBoost.Data;
using EduBoost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    public class InnovativeMethodsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InnovativeMethodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: InnovativeMethods
        public async Task<IActionResult> Index()
        {
            return View(await _context.InnovativeMethods
                .OrderBy(m => m.SuitableLearningStyle)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync());
        }

        // GET: InnovativeMethods/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new InnovativeMethod());
        }

        // POST: InnovativeMethods/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,Category,SuitableLearningStyle")] InnovativeMethod method)
        {
            if (!ModelState.IsValid) return View(method);

            _context.Add(method);
            await _context.SaveChangesAsync();
            TempData["Msg"] = $"Teaching method {method.Name} was added to the library.";
            return RedirectToAction(nameof(Index));
        }

        // GET: InnovativeMethods/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var method = await _context.InnovativeMethods.FindAsync(id);
            if (method == null) return NotFound();
            return View(method);
        }

        // POST: InnovativeMethods/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Name,Description,Category,SuitableLearningStyle")] InnovativeMethod method)
        {
            if (id != method.Id) return NotFound();
            if (!ModelState.IsValid) return View(method);

            try
            {
                _context.Update(method);
                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Teaching method {method.Name} was updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MethodExists(method.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: InnovativeMethods/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var method = await _context.InnovativeMethods.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (method == null) return NotFound();
            return View(method);
        }

        // POST: InnovativeMethods/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var method = await _context.InnovativeMethods.FindAsync(id);
            if (method != null)
            {
                _context.InnovativeMethods.Remove(method);
                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Teaching method {method.Name} was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MethodExists(int id)
        {
            return _context.InnovativeMethods.Any(e => e.Id == id);
        }
    }
}
