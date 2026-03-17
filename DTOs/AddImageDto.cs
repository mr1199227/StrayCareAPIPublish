using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class AddImageDto
    {
        public IFormFile ImageFile { get; set; } = null!;
    }
}
