namespace StrayCareAPI.DTOs
{
    public class UserDashboardStatsDto
    {
        public int AnimalCount { get; set; }
        public int AdoptionCount { get; set; }
        public int FeedingCount { get; set; }
        public decimal TotalDonated { get; set; }
    }

    public class UserDonationItemDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public int CampaignId { get; set; }
        public string CampaignTitle { get; set; } = string.Empty;
        public int? AnimalId { get; set; }
        public string? AnimalName { get; set; }
    }

    public class UserTopupItemDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserFinancialsDto
    {
        public List<UserDonationItemDto> Donations { get; set; } = new();
        public List<UserTopupItemDto> Topups { get; set; } = new();
    }

    public class UserRecentActivityDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal? Amount { get; set; }
        public int SourceId { get; set; }
    }
}
