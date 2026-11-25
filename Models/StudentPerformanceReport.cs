namespace EduBoost.Models
{
    /// <summary>
    /// Keyless entity mapped to the database view 'vw_StudentPerformance'.
    /// Demonstrates EF Core database view querying.
    /// </summary>
    public class StudentPerformanceReport
    {
        public int Id { get; set; }
        public string RollNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public double AttendancePercent { get; set; }
        public double RiskScore { get; set; }
        public bool IsSlowLearner { get; set; }
        public double AvgPercent { get; set; }
    }
}
