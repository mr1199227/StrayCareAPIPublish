namespace StrayCareAPI.DTOs
{
    public class FeedingLogDto
    {
        public int Id { get; set; }
        public int AnimalId { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime FedAt { get; set; }
        public string? ProofImageUrl { get; set; }
        public string? Description { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
