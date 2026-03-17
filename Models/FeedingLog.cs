using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class FeedingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime FedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? ProofImageUrl { get; set; }

        // --- New Fields ---
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
        // ------------------

        // Navigation Properties
        [ForeignKey("AnimalId")]
        public virtual Animal Animal { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
