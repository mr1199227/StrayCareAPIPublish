using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class VolunteerProfile
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [StringLength(500)]
        public string Skills { get; set; } = string.Empty;

        [StringLength(200)]
        public string Availability { get; set; } = string.Empty;

        public bool HasVehicle { get; set; }

        [StringLength(50)]
        public string ExperienceLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Expert

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
