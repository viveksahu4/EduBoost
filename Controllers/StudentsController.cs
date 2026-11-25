using EduBoost.Data;
using EduBoost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Students?search=text
        public async Task<IActionResult> Index(string? search)
        {
            var students = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                students = students.Where(s =>
                    s.FullName.Contains(search) ||
                    s.RollNo.Contains(search) ||
                    s.ClassName.Contains(search));
            }

            ViewData["Search"] = search;
            return View(await students
                .OrderBy(s => s.ClassName)
                .ThenBy(s => s.RollNo)
                .ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Assessments).ThenInclude(a => a.Subject)
                .Include(s => s.RemedialPlans).ThenInclude(p => p.Subject)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View(new Student());
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("RollNo,FullName,ClassName,ParentEmail,AttendancePercent,LearningStyle")] Student student)
        {
            if (await _context.Students.AnyAsync(s => s.RollNo == student.RollNo))
            {
                ModelState.AddModelError(nameof(Student.RollNo), "This roll number is already registered.");
            }

            if (!ModelState.IsValid) return View(student);

            _context.Add(student);
            await _context.SaveChangesAsync();
            TempData["Msg"] = $"Student {student.FullName} was added.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,RollNo,FullName,ClassName,ParentEmail,AttendancePercent,LearningStyle")] Student student)
        {
            if (id != student.Id) return NotFound();

            if (await _context.Students.AnyAsync(s => s.RollNo == student.RollNo && s.Id != student.Id))
            {
                ModelState.AddModelError(nameof(Student.RollNo), "This roll number is already registered.");
            }

            if (!ModelState.IsValid) return View(student);

            try
            {
                // Load the tracked entity so calculated risk values are preserved.
                var existing = await _context.Students.FindAsync(id);
                if (existing == null) return NotFound();

                existing.RollNo = student.RollNo;
                existing.FullName = student.FullName;
                existing.ClassName = student.ClassName;
                existing.ParentEmail = student.ParentEmail;
                existing.AttendancePercent = student.AttendancePercent;
                existing.LearningStyle = student.LearningStyle;

                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Student {student.FullName} was updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(student.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["Msg"] = $"Student {student.FullName} was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Students/StyleQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StyleQuiz(
            int studentId, int visual, int auditory, int reading, int kinesthetic)
        {
            var scores = new Dictionary<string, int>
            {
                { "Visual", visual },
                { "Auditory", auditory },
                { "Reading", reading },
                { "Kinesthetic", kinesthetic }
            };

            var style = scores.OrderByDescending(x => x.Value).First().Key;

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            student.LearningStyle = style;
            await _context.SaveChangesAsync();

            // Session usage (syllabus requirement).
            HttpContext.Session.SetString("LastStyle", style);

            TempData["Msg"] = $"Learning style detected for {student.FullName}: {style}";
            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
