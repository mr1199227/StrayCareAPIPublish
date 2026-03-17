using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.Services
{
    public interface ICloudinaryService
    {
        Task<(bool Success, string Message, string? SecureUrl)> UploadImageAsync(IFormFile file, string folderName);
    }
}
