using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class DonateDto
    {
        // [REF CHANGED]: Targeted Donation to specific Campaign
        // public int? AnimalId { get; set; }
        
        [Required]
        public int CampaignId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "捐赠金额必须大于 0")]
        public decimal Amount { get; set; }
    }
}
