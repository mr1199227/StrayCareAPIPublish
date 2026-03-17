using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class CreateFeedingLogDto
    {
        [Required]
        public int AnimalId { get; set; }

        // User ID of the person feeding (if not taken from token)
        public int? UserId { get; set; }

        public IFormFile? ProofImage { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}
