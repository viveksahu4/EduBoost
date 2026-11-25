using EduBoost.Data;
using EduBoost.Models;
using EduBoost.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    public class AssessmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlowLearnerService _slowLearnerService;

        public AssessmentsController(ApplicationDbContext context, ISlowLearnerService slowLearnerService)
        {
            _context = context;
            _slowLearnerService = slowLearnerService;
        }

        // GET: Assessments?studentId=5
        public async Task<IActionResult> Index(int? studentId)
        {
            var assessments = _context.Assessments
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .AsQueryable();

            if (studentId.HasValue)
            {
                assessments = assessments.Where(a => a.StudentId == studentId.Value);
                ViewData["StudentId"] = studentId.Value;
            }

            ViewBag.Students = new SelectList(
                await _context.Students.OrderBy(s => s.FullName).ToListAsync(), "Id", "FullName", studentId);

            return View(await assessments
                .OrderByDescending(a => a.ExamDate)
                .ThenBy(a => a.Student!.FullName)
                .ToListAsync());
        }

        // GET: Assessments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assessment = await _context.Assessments
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();
            return View(assessment);
        }

        // GET: Assessments/Create
        public async Task<IActionResult> Create(int? studentId)
        {
            await FillDropDownsAsync(studentId);
            return View(new Assessment { StudentId = studentId ?? 0 });
        }

        // POST: Assessments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("StudentId,SubjectId,ExamType,MarksObtained,MaxMarks,ExamDate")] Assessment assessment)
        {
            if (assessment.MarksObtained > assessment.MaxMarks)
            {
                ModelState.AddModelError(nameof(Assessment.MarksObtained),
                    "Marks obtained cannot be greater than maximum marks.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropDownsAsync(assessment.StudentId);
                return View(assessment);
            }

            _context.Add(assessment);
            await _context.SaveChangesAsync();
            await _slowLearnerService.UpdateStudentRiskAsync(assessment.StudentId);
            TempData["Msg"] = "Assessment saved and student risk recalculated automatically.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Assessments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment == null) return NotFound();

            await FillDropDownsAsync(assessment.StudentId, assessment.SubjectId);
            return View(assessment);
        }

        // POST: Assessments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,StudentId,SubjectId,ExamType,MarksObtained,MaxMarks,ExamDate")] Assessment assessment)
        {
            if (id != assessment.Id) return NotFound();

            if (assessment.MarksObtained > assessment.MaxMarks)
            {
                ModelState.AddModelError(nameof(Assessment.MarksObtained),
                    "Marks obtained cannot be greater than maximum marks.");
            }

            if (!ModelState.IsValid)
            {
                await FillDropDownsAsync(assessment.StudentId, assessment.SubjectId);
                return View(assessment);
            }

            try
            {
                _context.Update(assessment);
                await _context.SaveChangesAsync();
                await _slowLearnerService.UpdateStudentRiskAsync(assessment.StudentId);
                TempData["Msg"] = "Assessment updated and student risk recalculated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssessmentExists(assessment.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Assessments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var assessment = await _context.Assessments
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();
            return View(assessment);
        }

        // POST: Assessments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment != null)
            {
                int studentId = assessment.StudentId;
                _context.Assessments.Remove(assessment);
                await _context.SaveChangesAsync();
                await _slowLearnerService.UpdateStudentRiskAsync(studentId);
                TempData["Msg"] = "Assessment deleted and student risk updated.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task FillDropDownsAsync(int? studentId = null, int? subjectId = null)
        {
            ViewBag.StudentId = new SelectList(
                await _context.Students.OrderBy(s => s.FullName).ToListAsync(),
                "Id", "FullName", studentId);
            ViewBag.SubjectId = new SelectList(
                await _context.Subjects.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name", subjectId);
        }

        private bool AssessmentExists(int id)
        {
            return _context.Assessments.Any(e => e.Id == id);
        }
    }
}
