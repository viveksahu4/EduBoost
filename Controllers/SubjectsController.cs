using EduBoost.Data;
using EduBoost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Assessments)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();
            return View(subjects);
        }

        // GET: Subjects/Create
        public IActionResult Create()
        {
            return View(new Subject());
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Subject subject)
        {
            if (await _context.Subjects.AnyAsync(s => s.Name == subject.Name))
            {
                ModelState.AddModelError(nameof(Subject.Name), "This subject already exists.");
            }

            if (!ModelState.IsValid) return View(subject);

            _context.Add(subject);
            await _context.SaveChangesAsync();
            TempData["Msg"] = $"Subject {subject.Name} was added.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Subjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Subject subject)
        {
            if (id != subject.Id) return NotFound();

            if (await _context.Subjects.AnyAsync(s => s.Name == subject.Name && s.Id != subject.Id))
            {
                ModelState.AddModelError(nameof(Subject.Name), "This subject already exists.");
            }

            if (!ModelState.IsValid) return View(subject);

            try
            {
                _context.Update(subject);
                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Subject {subject.Name} was updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(subject.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects
                .Include(s => s.Assessments)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();
            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                if (await _context.Assessments.AnyAsync(a => a.SubjectId == id))
                {
                    TempData["Error"] = "This subject has assessments. Delete those assessments first.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Subject {subject.Name} was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }
    }
}
