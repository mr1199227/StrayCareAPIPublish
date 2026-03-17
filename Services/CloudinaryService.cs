using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace StrayCareAPI.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<(bool Success, string Message, string? SecureUrl)> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty or null.", null);
            }

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folderName 
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return (false, $"Failed to upload image to cloud: {uploadResult.Error.Message}", null);
                }

                return (true, "Image uploaded successfully.", uploadResult.SecureUrl.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"Exception during image upload: {ex.Message}", null);
            }
        }
    }
}
