using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class RegisterShelterDto
    {
        // User Info
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // Shelter Profile Info
        [Required]
        [StringLength(200)]
        public string ShelterName { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // License file is now optional
        public IFormFile? LicenseFile { get; set; }

        // Shelter profile image/logo (optional)
        public IFormFile? ProfileImage { get; set; }
    }
}
