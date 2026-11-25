using System.Text;
using EduBoost.Data;
using EduBoost.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    public class SlowLearnersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISlowLearnerService _service;

        public SlowLearnersController(ApplicationDbContext db, ISlowLearnerService service)
        {
            _db = db;
            _service = service;
        }

        // GET: /SlowLearners
        public async Task<IActionResult> Index()
        {
            // Primary view model: load slow learners
            var list = await _db.Students
                .Where(s => s.IsSlowLearner)
                .OrderByDescending(s => s.RiskScore)
                .ToListAsync();

            // Demonstrate querying the database view 'vw_StudentPerformance'
            ViewBag.ReportingViewCount = await _db.StudentPerformanceReports
                .CountAsync(r => r.IsSlowLearner);

            return View(list);
        }

        // POST: /SlowLearners/RunDetection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunDetection()
        {
            int count = await _service.RunDetectionAsync();
            TempData["Msg"] = $"Detection complete. {count} students flagged for remedial support.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /SlowLearners/ExportCsv
        public async Task<IActionResult> ExportCsv()
        {
            // Uses the database reporting view vw_StudentPerformance for data export
            var records = await _db.StudentPerformanceReports
                .Where(s => s.IsSlowLearner || s.RiskScore >= 45)
                .OrderByDescending(s => s.RiskScore)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("RollNo,FullName,Class,AttendancePercent,AveragePercent,RiskScore,Status");

            foreach (var r in records)
            {
                var status = r.IsSlowLearner ? "Needs Support" : "Monitor";
                sb.AppendLine($"\"{r.RollNo}\",\"{r.FullName}\",\"{r.ClassName}\",{r.AttendancePercent:F1},{r.AvgPercent:F1},{r.RiskScore:F1},\"{status}\"");
            }

            var fileName = $"EduBoost_SlowLearners_{DateTime.Today:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }

        // GET: /SlowLearners/FromStoredProcedure?minRisk=45
        public async Task<IActionResult> FromStoredProcedure(double minRisk = 45)
        {
            // Executes the SQL stored procedure sp_GetSlowLearners
            var results = await _service.GetSlowLearnersFromSpAsync(minRisk);
            return Json(new
            {
                source = "Stored Procedure: sp_GetSlowLearners",
                threshold = minRisk,
                count = results.Count,
                data = results
            });
        }
    }
}