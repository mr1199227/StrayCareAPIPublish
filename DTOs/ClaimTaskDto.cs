using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class ClaimTaskDto
    {
        [Required]
        public int AnimalId { get; set; }

        [Required]
        public int VolunteerId { get; set; }

        [Required]
        [StringLength(50)]
        public string TaskType { get; set; } = string.Empty; // Vaccine/Neuter
    }
}
