using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        [Display(Name = "Subject Name")]
        public string Name { get; set; } = "";

        public List<Assessment> Assessments { get; set; } = new();
    }
}
