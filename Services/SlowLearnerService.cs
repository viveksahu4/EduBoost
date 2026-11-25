using EduBoost.Data;
using EduBoost.Models;
using EduBoost.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Services
{
    public class SlowLearnerService : ISlowLearnerService
    {
        private readonly ApplicationDbContext _db;

        public SlowLearnerService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> RunDetectionAsync()
        {
            var students = await _db.Students.Include(s => s.Assessments).ToListAsync();
            int flagged = 0;

            foreach (var student in students)
            {
                ApplyRiskCalculation(student);
                if (student.IsSlowLearner)
                {
                    flagged++;
                }
            }

            await _db.SaveChangesAsync();
            return flagged;
        }

        public async Task<bool> UpdateStudentRiskAsync(int studentId)
        {
            var student = await _db.Students
                .Include(s => s.Assessments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return false;

            ApplyRiskCalculation(student);
            await _db.SaveChangesAsync();
            return student.IsSlowLearner;
        }

        public async Task<List<SlowLearnerSpResult>> GetSlowLearnersFromSpAsync(double minRisk = 45)
        {
            // Execute the stored procedure sp_GetSlowLearners via EF Core raw SQL
            return await _db.Database
                .SqlQueryRaw<SlowLearnerSpResult>("EXEC sp_GetSlowLearners @MinRisk = {0}", minRisk)
                .ToListAsync();
        }

        /// <summary>
        /// Computes weighted risk:
        /// - Exam marks deficiency: 60% weight
        /// - Low attendance penalty: 25% weight
        /// - Declining performance trend: 15% weight
        /// Flagged as Slow Learner when average mark is below 40% OR total risk score is >= 45.
        /// </summary>
        public static void ApplyRiskCalculation(Student s)
        {
            var marks = s.Assessments
                .OrderBy(a => a.ExamDate)
                .Select(a => a.Percentage)
                .ToList();

            if (marks.Count == 0)
            {
                // Without assessments, base risk primarily on attendance
                double attendanceRisk = (100 - s.AttendancePercent) * 0.25;
                s.RiskScore = Math.Round(Math.Clamp(attendanceRisk, 0, 100), 1);
                s.IsSlowLearner = s.RiskScore >= 45;
                return;
            }

            double avg = marks.Average();
            double trend = marks.Count > 1 ? marks.Last() - marks.First() : 0;

            double risk = (100 - avg) * 0.60
                        + (100 - s.AttendancePercent) * 0.25
                        + Math.Max(0, -trend) * 0.15;

            s.RiskScore = Math.Round(Math.Clamp(risk, 0, 100), 1);
            s.IsSlowLearner = avg < 40 || s.RiskScore >= 45;
        }
    }
}