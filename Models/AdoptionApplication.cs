using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class AdoptionApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Required]
        public int ApplicantUserId { get; set; }

        public int? ShelterId { get; set; } // Derived from Animal for optimization

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("AnimalId")]
        public virtual Animal Animal { get; set; } = null!;

        [ForeignKey("ApplicantUserId")]
        public virtual User ApplicantUser { get; set; } = null!;

        [ForeignKey("ShelterId")]
        public virtual User? Shelter { get; set; }
    }
}
