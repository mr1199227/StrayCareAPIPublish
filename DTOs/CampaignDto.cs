namespace StrayCareAPI.DTOs
{
    public class CampaignDto
    {
        public int Id { get; set; }
        public int AnimalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Food, Vaccine, Sterilization, Emergency
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Active, FullyFunded, Completed, Closed
        public decimal Percentage => TargetAmount > 0 ? Math.Round((CurrentAmount / TargetAmount) * 100, 2) : 0;
    }

    public class CreateEmergencyCampaignDto
    {
        public int AnimalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
    }
}
