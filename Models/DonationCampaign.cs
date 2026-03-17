using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StrayCareAPI.Models
{
    public enum CampaignType
    {
        Food,
        Vaccine,
        Sterilization,
        Emergency
    }

    public enum CampaignStatus
    {
        Active,       // Fundraising in progress
        FullyFunded,  // Goal reached, task ready/generated
        Completed,    // Task done (Vaccinated/Neutered)
        Closed        // Manually closed or cancelled (e.g. proof submitted externally)
    }

    public class DonationCampaign
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Food"; // Stored as string for DB compatibility, used via helper or Enum parsing

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active/FullyFunded/Completed/Closed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("AnimalId")]
        [JsonIgnore] // Prevent cycles if necessary, though we handle via options
        public virtual Animal? Animal { get; set; }

        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
