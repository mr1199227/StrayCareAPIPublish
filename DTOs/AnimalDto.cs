namespace StrayCareAPI.DTOs
{
    /// <summary>
    /// 动物响应 DTO，包含品种名称
    /// </summary>
    public class AnimalDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Demographics
        public string Species { get; set; } = string.Empty;
        public int? BreedId { get; set; }
        public string? BreedName { get; set; } // 来自导航属性
        public string Gender { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string EstAge { get; set; } = string.Empty;

        public List<string> ImageUrls { get; set; } = new List<string>();
        public string Status { get; set; } = string.Empty;

        // Medical
        public bool IsVaccinated { get; set; }
        public bool IsNeutered { get; set; }

        // [REF REMOVED]: Old Goal fields
        // public string CurrentGoalType { get; set; }
        // public decimal GoalAmount { get; set; }
        // public decimal RaisedAmount { get; set; }
        // public string GoalStatus { get; set; }

        public string LocationName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // [REF NEW]: Multi-Campaigns
        public List<CampaignDto> Campaigns { get; set; } = new List<CampaignDto>();
        
        // Optional: Expose proofs if needed
        public List<HealthProofDetailDto> PendingProofs { get; set; } = new List<HealthProofDetailDto>();

        // Calculated field for "Nearby" feature
        public double DistanceInKm { get; set; }
    }
}
