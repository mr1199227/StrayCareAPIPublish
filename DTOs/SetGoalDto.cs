using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class SetGoalDto
    {
        [Required]
        public int AnimalId { get; set; }

        [Required]
        [StringLength(50)]
        public string GoalType { get; set; } = string.Empty; // Vaccine/Neuter

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "目标金额必须大于 0")]
        public decimal GoalAmount { get; set; }
    }
}
