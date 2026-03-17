using System;

namespace StrayCareAPI.DTOs
{
    public class ActivityHistoryDto
    {
        public string ActivityType { get; set; } // "Sighting" or "Feeding"
        public int Id { get; set; } // Original ID
        public DateTime Timestamp { get; set; }
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string Username { get; set; }
    }
}
