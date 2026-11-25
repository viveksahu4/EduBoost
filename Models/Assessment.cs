using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class Assessment
    {
        public int Id { get; set; }

        [Display(Name = "Student")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a student.")]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Display(Name = "Subject")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Exam Type")]
        public string ExamType { get; set; } = "Unit Test";

        [Range(0, 1000)]
        [Display(Name = "Marks Obtained")]
        public double MarksObtained { get; set; }

        [Range(1, 1000)]
        [Display(Name = "Maximum Marks")]
        public double MaxMarks { get; set; } = 100;

        [DataType(DataType.Date)]
        [Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; } = DateTime.Today;

        /// <summary>Percentage is calculated, never stored in the database.</summary>
        public double Percentage => MaxMarks == 0 ? 0 : MarksObtained * 100 / MaxMarks;
    }
}
