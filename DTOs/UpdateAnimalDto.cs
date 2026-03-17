using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class UpdateAnimalDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        public int? BreedId { get; set; }

        [StringLength(20)]
        public string? Species { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? EstAge { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public bool? IsNeutered { get; set; }

        public bool? IsVaccinated { get; set; }
    }
}

