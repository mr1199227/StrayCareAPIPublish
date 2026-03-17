namespace StrayCareAPI.DTOs
{
    public class ShelterDto
    {
        public int Id { get; set; } // ShelterProfile Id
        public int UserId { get; set; } // User Id
        public string ShelterName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; } // From User
    }

    public class ShelterDetailDto : ShelterDto
    {
        public string Description { get; set; } = string.Empty;
        public string? LicenseImageUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Email { get; set; } = string.Empty; // Contact info
        public string? PhoneNumber { get; set; }
    }

    public class NearbyShelterDto : ShelterDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceInKm { get; set; }
    }
}
