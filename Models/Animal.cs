using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class Animal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // --- NEW DEMOGRAPHICS FIELDS ---
        [Required]
        [StringLength(20)]
        public string Species { get; set; } = "Dog"; // "Dog" or "Cat"

        // Foreign Key for Breed (Nullable, in case it's a mixed breed not in list)
        public int? BreedId { get; set; }
        
        [ForeignKey("BreedId")]
        public virtual Breed? Breed { get; set; }

        [StringLength(20)]
        public string Gender { get; set; } = "Unknown"; // Male/Female/Unknown

        [StringLength(20)]
        public string Size { get; set; } = "Medium"; // Small/Medium/Large

        [StringLength(50)]
        public string EstAge { get; set; } = string.Empty; // e.g., "2 years", "3 months"
        // -------------------------------

        // 当前位置信息（来自最新的目击记录）
        [StringLength(200)]
        public string? LocationName { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Stray"; // Stray/Adopted

        // 医疗标记
        public bool IsVaccinated { get; set; } = false;
        public bool IsNeutered { get; set; } = false;

        // 众筹逻辑 (已重构为多活动模式，旧字段移除)
        // [REF REMOVED]: CurrentGoalType, GoalAmount, RaisedAmount, GoalStatus

        public int? ShelterId { get; set; }

        [ForeignKey("ShelterId")]
        public virtual User? Shelter { get; set; }

        // [NEW] Owner ID (Adopter)
        public int? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual User? Owner { get; set; }

        // 导航属性
        public virtual ICollection<AnimalImage> Images { get; set; } = new List<AnimalImage>();
        public virtual ICollection<Sighting> Sightings { get; set; } = new List<Sighting>();
        public virtual ICollection<FeedingLog> FeedingLogs { get; set; } = new List<FeedingLog>();
        
        // [REF CHANGED]: Donation is now linked to Campaign, not Animal directly
        // public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
        
        // [REF NEW]: Multi-Campaign Architecture
        public virtual ICollection<DonationCampaign> Campaigns { get; set; } = new List<DonationCampaign>();
        public virtual ICollection<HealthUpdateProof> HealthUpdateProofs { get; set; } = new List<HealthUpdateProof>();

        public virtual ICollection<MedicalTask> MedicalTasks { get; set; } = new List<MedicalTask>();
    }
}
