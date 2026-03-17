using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class EditUserProfileDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public IFormFile? ProfileImage { get; set; }

        // Expanded Profile Data
        public VolunteerUpdateDto? VolunteerData { get; set; }
        public ShelterUpdateDto? ShelterData { get; set; }
    }

    public class VolunteerUpdateDto
    {
        [StringLength(500)]
        public string? Skills { get; set; }

        [StringLength(200)]
        public string? Availability { get; set; }

        public bool? HasVehicle { get; set; }

        [StringLength(50)]
        public string? ExperienceLevel { get; set; }
    }

    public class ShelterUpdateDto
    {
        [StringLength(200)]
        public string? ShelterName { get; set; }

        [StringLength(300)]
        public string? Address { get; set; } // Shelter also has Address in Profile

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
