using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class ShelterProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string ShelterName { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string LicenseImageUrl { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
