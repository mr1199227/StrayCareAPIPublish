using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class AdoptionApplicationDto
    {
        [Required]
        public int AnimalId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        // Only for intake
        public int ApplicantUserId { get; set; }
    }
}
