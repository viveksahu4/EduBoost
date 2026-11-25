using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class InnovativeMethod
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        [Display(Name = "Method Name")]
        public string Name { get; set; } = "";

        [Required, StringLength(500)]
        public string Description { get; set; } = "";

        [Required, StringLength(60)]
        public string Category { get; set; } = "";

        [Required, StringLength(20)]
        [Display(Name = "Suitable Learning Style")]
        public string SuitableLearningStyle { get; set; } = "Visual";
    }
}
