using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Positive for Topup, Negative for Expense

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty; // "Topup", "Donation"

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
