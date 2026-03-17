using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class MedicalTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        public int? VolunteerId { get; set; }

        [Required]
        [StringLength(50)]
        public string TaskType { get; set; } = string.Empty; // Vaccine/Neuter

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "InProgress"; // InProgress/PendingApproval/Approved

        [StringLength(500)]
        public string? ProofImage { get; set; }

        [StringLength(500)]
        public string? ReceiptImageUrl { get; set; }

        public decimal ExpenseAmount { get; set; } = 0;

        // 导航属性
        [ForeignKey("AnimalId")]
        public virtual Animal Animal { get; set; } = null!;

        [ForeignKey("VolunteerId")]
        public virtual User? Volunteer { get; set; }
    }
}
