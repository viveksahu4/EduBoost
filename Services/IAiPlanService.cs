namespace EduBoost.Services
{
    public interface IAiPlanService
    {
        Task<string> GeneratePlanAsync(string studentName, string subject,
                                       double avgPercent, string learningStyle,
                                       double attendance);
    }
}