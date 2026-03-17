using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class FeedingService : IFeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public FeedingService(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<(bool Success, string Message, object? Data)> CreateFeedingLogAsync(CreateFeedingLogDto dto)
        {
            // 1. Validate Animal
            var animal = await _context.Animals.FindAsync(dto.AnimalId);
            if (animal == null)
            {
                return (false, "Animal not found", null);
            }

            // 2. Handle Image Upload (Optional)
            string proofImageUrl;

            if (dto.ProofImage != null && dto.ProofImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                var extension = Path.GetExtension(dto.ProofImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return (false, "Only JPG and PNG are allowed.", null);
                }

                if (dto.ProofImage.Length > maxFileSize)
                {
                    return (false, "Image size cannot exceed 5MB.", null);
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ProofImage, "straycare/feedings");
                if (!uploadResult.Success)
                {
                    return (false, uploadResult.Message, null);
                }

                proofImageUrl = uploadResult.SecureUrl!;
            }
            else
            {
                // Use default image
                proofImageUrl = "/uploads/defaults/default-animal.png";
            }

            // 3. Create FeedingLog
            var feedingLog = new FeedingLog
            {
                AnimalId = dto.AnimalId,
                UserId = dto.UserId!.Value,
                FedAt = DateTime.UtcNow,
                ProofImageUrl = proofImageUrl,
                Description = dto.Description,
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.FeedingLogs.Add(feedingLog);

            // 4. Update Animal Location (Location Sync)
            animal.LocationName = dto.LocationName;
            animal.Latitude = dto.Latitude;
            animal.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();

            // Return DTO to avoid Circular Reference
            var resultDto = new FeedingLogDto
            {
                Id = feedingLog.Id,
                AnimalId = feedingLog.AnimalId,
                AnimalName = animal.Name,
                UserId = feedingLog.UserId,
                // UserName would require fetching User entity, simplified or fetch if needed. 
                // Keeping blank as per original controller logic.
                UserName = "", 
                FedAt = feedingLog.FedAt,
                ProofImageUrl = feedingLog.ProofImageUrl,
                Description = feedingLog.Description,
                LocationName = feedingLog.LocationName,
                Latitude = feedingLog.Latitude,
                Longitude = feedingLog.Longitude
            };

            return (true, "Feeding log created and location updated.", resultDto);
        }
    }
}
