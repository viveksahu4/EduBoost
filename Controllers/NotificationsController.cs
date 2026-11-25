using EduBoost.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NotificationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public class NotificationItemDto
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = "info"; // danger, warning, primary, success, info
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Time { get; set; } = "Just now";
            public string Url { get; set; } = "/";
            public string Category { get; set; } = "General"; // Alert, Plan, Assessment, System
            public bool IsUrgent { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = new List<NotificationItemDto>();

            // 1. High risk slow learners
            var slowLearners = await _db.Students
                .AsNoTracking()
                .Where(s => s.IsSlowLearner || s.RiskScore >= 50)
                .OrderByDescending(s => s.RiskScore)
                .Take(3)
                .ToListAsync();

            int slowIdx = 1;
            foreach (var student in slowLearners)
            {
                notifications.Add(new NotificationItemDto
                {
                    Id = $"slow-{student.Id}",
                    Type = student.RiskScore >= 75 ? "danger" : "warning",
                    Title = $"High Risk Alert: {student.FullName}",
                    Message = $"Class {student.ClassName} (Roll #{student.RollNo}) has a risk score of {student.RiskScore:F0}% with {student.AttendancePercent:F0}% attendance.",
                    Time = slowIdx == 1 ? "12m ago" : $"{slowIdx * 35}m ago",
                    Url = "/SlowLearners",
                    Category = "Alert",
                    IsUrgent = student.RiskScore >= 75
                });
                slowIdx++;
            }

            // 2. Active remedial plans needing attention
            var activePlans = await _db.RemedialPlans
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.StartDate)
                .Take(2)
                .ToListAsync();

            int planIdx = 1;
            foreach (var plan in activePlans)
            {
                notifications.Add(new NotificationItemDto
                {
                    Id = $"plan-{plan.Id}",
                    Type = "primary",
                    Title = $"Active Remedial Plan: {plan.Subject?.Name ?? "Subject"}",
                    Message = $"{plan.Student?.FullName ?? "Student"} is enrolled in: {plan.Title}.",
                    Time = $"{planIdx * 2}h ago",
                    Url = "/RemedialPlans",
                    Category = "Plan",
                    IsUrgent = false
                });
                planIdx++;
            }

            // 3. Low assessments score alert
            var lowAssessments = await _db.Assessments
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .Where(a => a.MaxMarks > 0 && ((double)a.MarksObtained / a.MaxMarks) < 0.45)
                .OrderByDescending(a => a.ExamDate)
                .Take(1)
                .ToListAsync();

            foreach (var a in lowAssessments)
            {
                var percent = Math.Round(((double)a.MarksObtained / a.MaxMarks) * 100, 1);
                notifications.Add(new NotificationItemDto
                {
                    Id = $"assess-{a.Id}",
                    Type = "warning",
                    Title = $"Assessment Review: {a.Subject?.Name ?? "Assessment"}",
                    Message = $"{a.Student?.FullName ?? "Student"} scored {a.MarksObtained}/{a.MaxMarks} ({percent}%) in {a.ExamType}.",
                    Time = "Yesterday",
                    Url = "/Assessments",
                    Category = "Assessment",
                    IsUrgent = false
                });
            }

            // 4. System tip or AI intervention notification
            notifications.Add(new NotificationItemDto
            {
                Id = "sys-ai-methods",
                Type = "success",
                Title = "AI Method Recommendations",
                Message = "New gamified & collaborative teaching methods are available for kinesthetic slow learners.",
                Time = "1d ago",
                Url = "/InnovativeMethods",
                Category = "System",
                IsUrgent = false
            });

            return Ok(new
            {
                totalCount = notifications.Count,
                notifications
            });
        }
    }
}
