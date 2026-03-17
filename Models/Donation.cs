using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class Donation
    {
        [Key]
        public int Id { get; set; }

        // [REF CHANGED]: Managed by CampaignId now
        // public int AnimalId { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // 导航属性
        // [ForeignKey("AnimalId")]
        // public virtual Animal Animal { get; set; } = null!;

        [ForeignKey("CampaignId")]
        public virtual DonationCampaign Campaign { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
