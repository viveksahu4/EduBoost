using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Roll No")]
        public string RollNo { get; set; } = "";

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, StringLength(30)]
        [Display(Name = "Class")]
        public string ClassName { get; set; } = "";

        [EmailAddress, StringLength(150)]
        [Display(Name = "Parent Email")]
        public string? ParentEmail { get; set; }

        [Range(0, 100, ErrorMessage = "Attendance must be between 0 and 100.")]
        [Display(Name = "Attendance (%)")]
        public double AttendancePercent { get; set; }

        // Visual / Auditory / Reading / Kinesthetic
        [Required, StringLength(20)]
        [Display(Name = "Learning Style")]
        public string LearningStyle { get; set; } = "Visual";

        [Display(Name = "Risk Score")]
        public double RiskScore { get; set; }

        [Display(Name = "Slow Learner")]
        public bool IsSlowLearner { get; set; }

        public List<Assessment> Assessments { get; set; } = new();
        public List<RemedialPlan> RemedialPlans { get; set; } = new();
    }
}
