using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace StrayCareAPI.Services
{
    public class SightingService : ISightingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public SightingService(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<(bool Success, string Message, object? Data)> CreateSightingAsync(CreateSightingDto dto)
        {
            Animal animal;
            Sighting sighting;
            string sightingImageUrl;

            // Handle Image Upload (Optional)
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                // Verify Image
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                var extension = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return (false, "Only JPG and PNG format images are supported.", null);
                }

                if (dto.ImageFile.Length > maxFileSize)
                {
                    return (false, "Image size cannot exceed 5MB", null);
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ImageFile, "straycare/sightings");
                if (!uploadResult.Success)
                {
                    return (false, uploadResult.Message, null);
                }

                sightingImageUrl = uploadResult.SecureUrl!;
            }
            else
            {
                // Use default image
                sightingImageUrl = "/uploads/defaults/default-animal.png";
            }

            // Determine Scene: Update Existing vs Create New
            bool isNewAnimal = !dto.AnimalId.HasValue || dto.AnimalId.Value <= 0;

            if (!isNewAnimal)
            {
                // ========== Scene A: Update Existing Animal ==========
                animal = await _context.Animals.FindAsync(dto.AnimalId!.Value);
                if (animal == null)
                {
                    return (false, "Animal not found", null);
                }

                // Update Location
                animal.LocationName = dto.LocationName;
                animal.Latitude = dto.Latitude;
                animal.Longitude = dto.Longitude;

                // Create Sighting Record
                sighting = new Sighting
                {
                    AnimalId = animal.Id,
                    ReporterUserId = dto.ReporterUserId!.Value,
                    Timestamp = DateTime.UtcNow,
                    Description = dto.Description,
                    SightingImageUrl = sightingImageUrl,
                    LocationName = dto.LocationName,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude
                };

                _context.Sightings.Add(sighting);
                await _context.SaveChangesAsync();
            }
            else
            {
                // ========== Scene B: Create New Animal ==========
                
                // Validate Name
                if (string.IsNullOrWhiteSpace(dto.NewAnimalName) || 
                    dto.NewAnimalName.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Please provide a valid name for the new animal.", null);
                }

                // Validate BreedId
                if (!dto.BreedId.HasValue)
                {
                    return (false, "Please select a breed (BreedId) when creating a new animal.", null);
                }

                var breed = await _context.Breeds.FindAsync(dto.BreedId.Value);
                if (breed == null)
                {
                    return (false, "Selected breed does not exist.", null);
                }

                // Create New Animal
                animal = new Animal
                {
                    Name = dto.NewAnimalName,
                    Species = breed.Species,
                    BreedId = dto.BreedId,
                    Gender = dto.Gender,
                    Size = dto.Size,
                    EstAge = "Unknown",
                    LocationName = dto.LocationName,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Status = "Stray",
                    IsVaccinated = false,
                    IsNeutered = false,
                    
                    // [REF UPDATED]: Auto-create Campaigns here too
                    Campaigns = new List<DonationCampaign>
                    {
                        new DonationCampaign
                        {
                            Title = "Daily Food Fund",
                            Type = CampaignType.Food.ToString(),
                            TargetAmount = 200m,
                            Status = CampaignStatus.Active.ToString()
                        },
                        new DonationCampaign
                        {
                            Title = "Vaccination Fund",
                            Type = CampaignType.Vaccine.ToString(),
                            TargetAmount = 100m,
                            Status = CampaignStatus.Active.ToString()
                        },
                        new DonationCampaign
                        {
                            Title = "Sterilization Fund",
                            Type = CampaignType.Sterilization.ToString(),
                            TargetAmount = 200m,
                            Status = CampaignStatus.Active.ToString()
                        }
                    }
                };

                // Add Image to Gallery
                var animalImage = new AnimalImage
                {
                    ImageUrl = sightingImageUrl,
                    Animal = animal
                };
                animal.Images.Add(animalImage);

                _context.Animals.Add(animal);
                await _context.SaveChangesAsync();

                // Create Sighting Record
                sighting = new Sighting
                {
                    AnimalId = animal.Id,
                    ReporterUserId = dto.ReporterUserId!.Value,
                    Timestamp = DateTime.UtcNow,
                    Description = dto.Description,
                    SightingImageUrl = sightingImageUrl,
                    LocationName = dto.LocationName,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude
                };

                _context.Sightings.Add(sighting);
                await _context.SaveChangesAsync();
            }

            var resultData = new
            {
                message = "Sighting record created successfully",
                sightingId = sighting.Id,
                animalId = animal.Id,
                animalName = animal.Name,
                isNewAnimal = isNewAnimal
            };

            return (true, "Sighting record created successfully", resultData);
        }

        public async Task<List<object>> GetAnimalSightingsAsync(int animalId)
        {
            var sightings = await _context.Sightings
                .Where(s => s.AnimalId == animalId)
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new
                {
                    s.Id,
                    s.Timestamp,
                    s.Description,
                    s.SightingImageUrl,
                    s.LocationName,
                    s.Latitude,
                    s.Longitude,
                    ReporterName = s.Reporter.Username
                })
                .ToListAsync<object>();
            
            return sightings;
        }
    }
}
