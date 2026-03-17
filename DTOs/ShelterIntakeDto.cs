using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class ShelterIntakeDto
    {
        public IFormFile? NewProfileImage { get; set; }
    }
}
