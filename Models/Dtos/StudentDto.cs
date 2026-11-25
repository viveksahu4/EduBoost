using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models.Dtos
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string RollNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string? ParentEmail { get; set; }
        public double AttendancePercent { get; set; }
        public string LearningStyle { get; set; } = "";
        public double RiskScore { get; set; }
        public bool IsSlowLearner { get; set; }
        public int AssessmentCount { get; set; }
        public int ActivePlansCount { get; set; }
    }

    public class CreateStudentDto
    {
        [Required, StringLength(20)]
        public string RollNo { get; set; } = "";

        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Required, StringLength(30)]
        public string ClassName { get; set; } = "";

        [EmailAddress, StringLength(150)]
        public string? ParentEmail { get; set; }

        [Range(0, 100)]
        public double AttendancePercent { get; set; } = 100;

        [StringLength(20)]
        public string LearningStyle { get; set; } = "Visual";
    }

    public class UpdateStudentDto : CreateStudentDto
    {
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// Result structure returned from stored procedure 'sp_GetSlowLearners'.
    /// </summary>
    public class SlowLearnerSpResult
    {
        public int Id { get; set; }
        public string RollNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public double RiskScore { get; set; }
        public double AttendancePercent { get; set; }
    }
}
