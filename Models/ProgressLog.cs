using System.ComponentModel.DataAnnotations;

namespace EduBoost.Models
{
    public class ProgressLog
    {
        public int Id { get; set; }

        public int RemedialPlanId { get; set; }
        public RemedialPlan? RemedialPlan { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime LogDate { get; set; } = DateTime.Today;

        [StringLength(1000)]
        public string Notes { get; set; } = "";

        [Range(0, 100)]
        [Display(Name = "Score After (%)")]
        public double ScoreAfter { get; set; }
    }
}
