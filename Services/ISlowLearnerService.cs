using EduBoost.Models.Dtos;

namespace EduBoost.Services
{
    public interface ISlowLearnerService
    {
        /// <summary>
        /// Evaluates all students and updates their RiskScore and IsSlowLearner flag.
        /// </summary>
        Task<int> RunDetectionAsync();

        /// <summary>
        /// Recalculates risk for a single student when assessments or attendance change.
        /// </summary>
        Task<bool> UpdateStudentRiskAsync(int studentId);

        /// <summary>
        /// Executes the SQL stored procedure 'sp_GetSlowLearners' using EF Core raw SQL.
        /// </summary>
        Task<List<SlowLearnerSpResult>> GetSlowLearnersFromSpAsync(double minRisk = 45);
    }
}