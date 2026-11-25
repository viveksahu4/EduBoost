using EduBoost.Data;
using EduBoost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalStudents = await _db.Students.CountAsync(),
                SlowLearners = await _db.Students.CountAsync(s => s.IsSlowLearner),
                ActivePlans = await _db.RemedialPlans.CountAsync(p => p.Status == "Active")
            };
            var data = await _db.Assessments.Include(a => a.Subject).ToListAsync();
            vm.SubjectAverages = data.GroupBy(a => a.Subject!.Name)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(a => a.Percentage), 1));

            vm.RecentStudents = await _db.Students
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .Take(6)
                .ToListAsync();

            return View(vm);
        }
    }
}