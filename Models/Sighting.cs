using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class Sighting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Required]
        public int ReporterUserId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? SightingImageUrl { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // 导航属性
        [ForeignKey("AnimalId")]
        public virtual Animal Animal { get; set; } = null!;

        [ForeignKey("ReporterUserId")]
        public virtual User Reporter { get; set; } = null!;
    }
}
