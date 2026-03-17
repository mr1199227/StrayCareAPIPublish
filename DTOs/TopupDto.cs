using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class TopupDto
    {
        [Required]
        [Range(0.01, 100000, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }
    }
}
