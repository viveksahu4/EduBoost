using EduBoost.Models;
using EduBoost.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EduBoost.Tests
{
    public class SlowLearnerRiskTests
    {
        [Fact]
        public void ApplyRiskCalculation_HighPerformer_ShouldNotBeSlowLearner()
        {
            // Arrange
            var student = new Student
            {
                Id = 1,
                RollNo = "T01",
                FullName = "Top Student",
                ClassName = "10-A",
                AttendancePercent = 95,
                Assessments = new List<Assessment>
                {
                    new() { MarksObtained = 88, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-30) },
                    new() { MarksObtained = 94, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-5) }
                }
            };

            // Act
            SlowLearnerService.ApplyRiskCalculation(student);

            // Assert
            Assert.False(student.IsSlowLearner);
            Assert.True(student.RiskScore < 20, $"Expected risk < 20, got {student.RiskScore}");
        }

        [Fact]
        public void ApplyRiskCalculation_FailingMarks_ShouldBeFlaggedSlowLearner()
        {
            // Arrange
            var student = new Student
            {
                Id = 2,
                RollNo = "T02",
                FullName = "Struggling Student",
                ClassName = "10-A",
                AttendancePercent = 70,
                Assessments = new List<Assessment>
                {
                    new() { MarksObtained = 30, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-30) },
                    new() { MarksObtained = 35, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-5) }
                }
            };

            // Act
            SlowLearnerService.ApplyRiskCalculation(student);

            // Assert: average is 32.5% (< 40%), so must be flagged
            Assert.True(student.IsSlowLearner);
            Assert.True(student.RiskScore >= 45, $"Expected risk >= 45, got {student.RiskScore}");
        }

        [Fact]
        public void ApplyRiskCalculation_DecliningTrendAndPoorAttendance_IncreasesRiskScore()
        {
            // Arrange: student drops from 60% down to 35% with 50% attendance
            var student = new Student
            {
                Id = 3,
                RollNo = "T03",
                FullName = "Declining Student",
                ClassName = "10-B",
                AttendancePercent = 50,
                Assessments = new List<Assessment>
                {
                    new() { MarksObtained = 60, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-60) },
                    new() { MarksObtained = 35, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-10) }
                }
            };

            // Act
            SlowLearnerService.ApplyRiskCalculation(student);

            // Assert
            // Trend penalty = (60 - 35) * 0.15 = 3.75
            // Attendance penalty = (100 - 50) * 0.25 = 12.5
            // Marks penalty = (100 - 47.5) * 0.60 = 31.5
            // Total risk approx = 47.75 -> rounded to 47.8
            Assert.True(student.IsSlowLearner);
            Assert.True(student.RiskScore >= 45);
        }

        [Fact]
        public void ApplyRiskCalculation_ClampsToZeroAndOneHundred()
        {
            // Worst case: 0% attendance, 0% marks
            var worstStudent = new Student
            {
                AttendancePercent = 0,
                Assessments = new List<Assessment>
                {
                    new() { MarksObtained = 0, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-20) },
                    new() { MarksObtained = 0, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-5) }
                }
            };

            SlowLearnerService.ApplyRiskCalculation(worstStudent);
            Assert.InRange(worstStudent.RiskScore, 0, 100);
            Assert.True(worstStudent.IsSlowLearner);

            // Best case: 100% attendance, 100% marks
            var bestStudent = new Student
            {
                AttendancePercent = 100,
                Assessments = new List<Assessment>
                {
                    new() { MarksObtained = 100, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-20) },
                    new() { MarksObtained = 100, MaxMarks = 100, ExamDate = DateTime.Today.AddDays(-5) }
                }
            };

            SlowLearnerService.ApplyRiskCalculation(bestStudent);
            Assert.Equal(0, bestStudent.RiskScore);
            Assert.False(bestStudent.IsSlowLearner);
        }

        [Fact]
        public async Task AiPlanService_ReturnsFallbackPlan_WhenApiKeyMissing()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Ai:ApiKey", "PUT-YOUR-KEY-HERE" },
                { "Ai:Model", "claude-sonnet-4-5" }
            };
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            using var httpClient = new HttpClient();
            var service = new AiPlanService(httpClient, config);

            // Act
            var plan = await service.GeneratePlanAsync("Aarav", "Mathematics", 35.0, "Visual", 60.0);

            // Assert
            Assert.NotNull(plan);
            Assert.Contains("Week 1", plan);
            Assert.Contains("Mathematics", plan);
            Assert.Contains("Visual", plan);
        }
    }
}
