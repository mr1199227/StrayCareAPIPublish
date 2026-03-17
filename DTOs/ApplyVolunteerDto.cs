using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class ApplyVolunteerDto
    {
        public int? UserId { get; set; }

        [StringLength(500)]
        public string? Skills { get; set; }

        [StringLength(200)]
        public string? Availability { get; set; }

        public bool HasVehicle { get; set; }

        [StringLength(50)]
        public string? ExperienceLevel { get; set; }
    }
}
