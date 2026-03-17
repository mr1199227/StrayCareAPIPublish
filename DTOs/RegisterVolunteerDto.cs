using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class RegisterVolunteerDto : RegisterDto
    {
        [StringLength(500)]
        public string Skills { get; set; } = string.Empty;

        [StringLength(200)]
        public string Availability { get; set; } = string.Empty;

        public bool HasVehicle { get; set; }

        [StringLength(50)]
        public string ExperienceLevel { get; set; } = "Beginner";
    }
}
