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
    public class RemedialPlansController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiPlanService _ai;

        public RemedialPlansController(ApplicationDbContext db, IAiPlanService ai)
        {
            _db = db;
            _ai = ai;
        }

        // GET: RemedialPlans
        public async Task<IActionResult> Index()
        {
            var plans = await _db.RemedialPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.Logs)
                .OrderByDescending(p => p.StartDate)
                .AsNoTracking()
                .ToListAsync();
            return View(plans);
        }

        // GET: RemedialPlans/Create?studentId=5
        public async Task<IActionResult> Create(int studentId)
        {
            var student = await _db.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            ViewBag.Student = student;
            ViewBag.Subjects = new SelectList(_db.Subjects.OrderBy(s => s.Name), "Id", "Name");
            ViewBag.Methods = await _db.InnovativeMethods
                .Where(m => m.SuitableLearningStyle == student.LearningStyle)
                .ToListAsync();

            return View(new RemedialPlan { StudentId = studentId });
        }

        // POST: RemedialPlans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("StudentId,SubjectId,Title,Strategy,Status,StartDate")] RemedialPlan plan)
        {
            if (string.IsNullOrWhiteSpace(plan.Title))
            {
                ModelState.AddModelError(nameof(RemedialPlan.Title), "Give the plan a short title.");
            }

            if (!ModelState.IsValid)
            {
                var student = await _db.Students.FindAsync(plan.StudentId);
                if (student == null) return NotFound();

                ViewBag.Student = student;
                ViewBag.Subjects = new SelectList(_db.Subjects.OrderBy(s => s.Name), "Id", "Name", plan.SubjectId);
                ViewBag.Methods = await _db.InnovativeMethods
                    .Where(m => m.SuitableLearningStyle == student.LearningStyle)
                    .ToListAsync();
                return View(plan);
            }

            plan.IsAiGenerated = false;
            _db.RemedialPlans.Add(plan);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Remedial plan created.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        // POST: RemedialPlans/GenerateAi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAi(int studentId, int subjectId)
        {
            var student = await _db.Students
                .Include(s => s.Assessments)
                .FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return NotFound();

            var subject = await _db.Subjects.FindAsync(subjectId);
            if (subject == null) return NotFound();

            var scores = student.Assessments
                .Where(a => a.SubjectId == subjectId)
                .Select(a => a.Percentage)
                .ToList();
            double avg = scores.Count == 0 ? 0 : scores.Average();

            var text = await _ai.GeneratePlanAsync(
                student.FullName, subject.Name, avg, student.LearningStyle, student.AttendancePercent);

            var plan = new RemedialPlan
            {
                StudentId = studentId,
                SubjectId = subjectId,
                Title = $"AI Plan - {subject.Name}",
                Strategy = text,
                IsAiGenerated = true,
                Status = "Active",
                StartDate = DateTime.Today
            };

            _db.RemedialPlans.Add(plan);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "AI remedial plan generated.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        // GET: RemedialPlans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var plan = await _db.RemedialPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.Logs.OrderByDescending(l => l.LogDate))
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return plan == null ? NotFound() : View(plan);
        }

        // POST: RemedialPlans/AddLog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLog(int id, DateTime logDate, string notes, double scoreAfter)
        {
            var plan = await _db.RemedialPlans.FindAsync(id);
            if (plan == null) return NotFound();

            if (scoreAfter < 0 || scoreAfter > 100)
            {
                TempData["Error"] = "Score after must be between 0 and 100.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _db.ProgressLogs.Add(new ProgressLog
            {
                RemedialPlanId = id,
                LogDate = logDate == default ? DateTime.Today : logDate,
                Notes = notes ?? "",
                ScoreAfter = scoreAfter
            });
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Progress log added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: RemedialPlans/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var plan = await _db.RemedialPlans.FindAsync(id);
            if (plan == null) return NotFound();

            plan.Status = "Completed";
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Plan marked as completed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: RemedialPlans/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _db.RemedialPlans.FindAsync(id);
            if (plan != null)
            {
                _db.RemedialPlans.Remove(plan);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Remedial plan deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
