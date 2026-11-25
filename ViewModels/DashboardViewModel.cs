using EduBoost.Models;

namespace EduBoost.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int SlowLearners { get; set; }
        public int ActivePlans { get; set; }
        public Dictionary<string, double> SubjectAverages { get; set; } = new();
        public List<Student> RecentStudents { get; set; } = new();

        public double AverageScore => SubjectAverages.Count == 0
            ? 0
            : Math.Round(SubjectAverages.Values.Average(), 1);

        public int SupportRate => TotalStudents == 0
            ? 0
            : (int)Math.Round(SlowLearners * 100.0 / TotalStudents);
    }
}
