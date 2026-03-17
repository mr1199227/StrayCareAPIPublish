using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class EditAnimalDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(20)]
        public string? Species { get; set; }

        public int? BreedId { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? EstAge { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public bool? IsVaccinated { get; set; }
        public bool? IsNeutered { get; set; }

        // [REF REMOVED]: Old Goal fields
        // public string? CurrentGoalType { get; set; }
        // public decimal? GoalAmount { get; set; }
        // public string? GoalStatus { get; set; }

        [StringLength(200)]
        public string? LocationName { get; set; }
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Optional: New Images to add?
        public List<IFormFile>? NewImages { get; set; }
    }
}
