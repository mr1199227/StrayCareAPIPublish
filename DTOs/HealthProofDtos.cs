using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class SubmitHealthProofDto
    {
        [Required]
        public int AnimalId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProofType { get; set; } = "Vaccine"; // Vaccine or Neuter

        [Required]
        public IFormFile ProofImage { get; set; } = null!;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    public class HealthProofDetailDto
    {
        public int Id { get; set; }
        public int AnimalId { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public string ProofType { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ApproveProofDto
    {
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
