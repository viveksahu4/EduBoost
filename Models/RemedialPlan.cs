using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class RemedialPlan
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

        [Required, StringLength(150)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Write the plan strategy or generate one with AI.")]
        [StringLength(4000)]
        public string Strategy { get; set; } = "";

        [Display(Name = "AI Generated")]
        public bool IsAiGenerated { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active"; // Active / Completed

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public List<ProgressLog> Logs { get; set; } = new();
    }
}
