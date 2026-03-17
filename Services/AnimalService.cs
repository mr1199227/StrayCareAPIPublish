using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class AnimalService : IAnimalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public AnimalService(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<Animal>> GetAllAnimalsAsync()
        {
            return await _context.Animals
                .Where(a => a.Status != "Inactive")
                .Include(a => a.Campaigns)
                .Include(a => a.Images)
                .ToListAsync();
        }

        public async Task<Animal?> GetAnimalByIdAsync(int id)
        {
            return await _context.Animals
                .Include(a => a.Campaigns)
                .Include(a => a.Images)
                .Include(a => a.Breed)
                .Include(a => a.MedicalTasks)
                .Include(a => a.Shelter).ThenInclude(u => u.ShelterProfile)
                .Include(a => a.Owner)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Animal> CreateAnimalAsync(CreateAnimalDto dto)
        {
             // 1. Validate Breed to derive Species
            var breed = await _context.Breeds.FindAsync(dto.BreedId);
            if (breed == null)
            {
               throw new ArgumentException("Invalid Breed ID");
            }

            var animal = new Animal
            {
                Name = dto.Name,
                BreedId = dto.BreedId,
                Species = breed.Species,
                Gender = dto.Gender ?? "Unknown",
                Size = dto.Size ?? "Unknown",
                EstAge = dto.EstAge ?? "Unknown",
                Status = dto.Status ?? "Stray",
                IsVaccinated = false,
                IsNeutered = dto.IsNeutered, // Allowed to set, but still creates campaign
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                
                // [REF REMOVED]: Old Goal fields
                // CurrentGoalType = "None",
                // GoalAmount = 0,
                // RaisedAmount = 0,
                // GoalStatus = "Completed",
                
                Images = new List<AnimalImage>()
            };

            // 2. Auto-Create 3 Default Campaigns
            var campaigns = new List<DonationCampaign>
            {
                new DonationCampaign
                {
                    Title = "Daily Food Fund",
                    Type = CampaignType.Food.ToString(),
                    TargetAmount = 200m, // Plan C: 200
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
            };

            // If animal is already marked as neutered/vaccinated, we might auto-close them?
            // Plan C says: "Users will not input health status... assume animal needs everything".
            // But CreateAnimalDto *does* have IsNeutered (I checked).
            // Let's assume strict Plan C: Always create campaigns. If it's already done, they use Proof workflow to close it.
            // OR we can be smart: if IsNeutered=true, we close the Sterilization campaign immediately?
            // "System must assume the animal needs everything by default." -> I will stick to this.
            
            // However, I should properly link them.
            // Since Animal is new, we add them to the collection.
            animal.Campaigns = campaigns;

            // 3. Handle Image Uploads
            if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
            {
                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var uploadResult = await _cloudinaryService.UploadImageAsync(file, "straycare/animals");
                        if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.SecureUrl))
                        {
                            bool isFirstImage = animal.Images.Count == 0;
                            animal.Images.Add(new AnimalImage
                            {
                                ImageUrl = uploadResult.SecureUrl,
                                IsPrimary = isFirstImage
                            });
                        }
                    }
                }
            }

            _context.Animals.Add(animal);
            await _context.SaveChangesAsync();
            return animal;
        }

        public async Task<(bool Success, string Message, int? AnimalId)> EditAnimalAsync(int id, EditAnimalDto dto, int userId, string userRole)
        {
            var animal = await _context.Animals.Include(a => a.Images).FirstOrDefaultAsync(a => a.Id == id);
            if (animal == null) return (false, "Animal not found", null);

            // Permission Logic:
            // 1. User/Volunteer: Can edit Basic Info (Breed, Gender, Size, EstAge) + Images
            // 2. Shelter: Can edit Basic Info + Name + Images
            // 3. Admin: Can edit ALL fields

            // --- Common Editable Fields ---
            if (dto.BreedId.HasValue) animal.BreedId = dto.BreedId.Value;
            if (dto.Gender != null) animal.Gender = dto.Gender;
            if (dto.Size != null) animal.Size = dto.Size;
            if (dto.EstAge != null) animal.EstAge = dto.EstAge;

            // --- Shelter & Admin Fields ---
            if (userRole == "Shelter" || userRole == "Admin")
            {
                if (dto.Name != null) animal.Name = dto.Name;
                // [修复] 允许 Shelter 和 Admin 修改医疗状态
                if (dto.IsVaccinated.HasValue) animal.IsVaccinated = dto.IsVaccinated.Value;
                if (dto.IsNeutered.HasValue) animal.IsNeutered = dto.IsNeutered.Value;
            }

            // --- Admin Only Fields ---
            if (userRole == "Admin")
            {
                if (dto.Species != null) animal.Species = dto.Species;
                if (dto.Status != null) animal.Status = dto.Status;

                // [REF REMOVED]: Goal updates removed from here. 
                // Campaigns are managed via separate endpoints.
                
                if (dto.LocationName != null) animal.LocationName = dto.LocationName;
                if (dto.Latitude.HasValue) animal.Latitude = dto.Latitude.Value;
                if (dto.Longitude.HasValue) animal.Longitude = dto.Longitude.Value;
            }

            // Handle New Images
            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                 foreach (var file in dto.NewImages)
                 {
                     if (file.Length > 0)
                     {
                        var uploadResult = await _cloudinaryService.UploadImageAsync(file, "straycare/animals");
                        if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.SecureUrl))
                        {
                            animal.Images.Add(new AnimalImage
                            {
                                ImageUrl = uploadResult.SecureUrl,
                                IsPrimary = false 
                            });
                        }
                     }
                 }
            }

            await _context.SaveChangesAsync();
            return (true, "Animal updated successfully", animal.Id);
        }

        public async Task<List<ActivityHistoryDto>> GetActivityHistoryAsync(int id)
        {
             // 1. Fetch Sightings
            var sightings = await _context.Sightings
                .Where(s => s.AnimalId == id)
                .Include(s => s.Reporter)
                .Select(s => new ActivityHistoryDto
                {
                    ActivityType = "Sighting",
                    Id = s.Id,
                    Timestamp = s.Timestamp,
                    LocationName = s.LocationName,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    ImageUrl = s.SightingImageUrl,
                    Description = s.Description,
                    Username = s.Reporter.Username
                })
                .ToListAsync();

            // 2. Fetch FeedingLogs
            var feedings = await _context.FeedingLogs
                .Where(f => f.AnimalId == id)
                .Include(f => f.User)
                .Select(f => new ActivityHistoryDto
                {
                    ActivityType = "Feeding",
                    Id = f.Id,
                    Timestamp = f.FedAt,
                    LocationName = f.LocationName,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude,
                    ImageUrl = f.ProofImageUrl,
                    Description = f.Description,
                    Username = f.User.Username
                })
                .ToListAsync();

            // 3. Merge & Sort
            return sightings.Concat(feedings)
                .OrderByDescending(a => a.Timestamp)
                .ToList();
        }

        public async Task<List<object>> GetAnimalSightingsAsync(int animalId)
        {
            return await _context.Sightings
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
        }

        public async Task<List<AnimalDto>> GetNearbyAnimalsAsync(double latitude, double longitude, double radius, int? pageNumber = null, int? pageSize = null)
        {
            // 1. Fetch animals with valid location data
            var animals = await _context.Animals
                .Include(a => a.Breed)
                .Include(a => a.Images)
                .Include(a => a.Campaigns) // Include Campaigns
                .Where(a => a.Latitude.HasValue && 
                            a.Longitude.HasValue && 
                            a.Status != "Inactive" && 
                            a.Status != "Adopted" && 
                            a.Status != "Sheltered")
                .ToListAsync();

            // 2. Calculate distance and filter in memory
            var sortedAnimals = animals
                .Select(a => new 
                { 
                    Animal = a, 
                    Distance = CalculateDistance(latitude, longitude, a.Latitude.Value, a.Longitude.Value) 
                })
                .Where(x => x.Distance <= radius)
                .OrderBy(x => x.Distance);

            // 动态应用分页 (如果传入了分页参数)
            if (pageNumber.HasValue && pageSize.HasValue && pageNumber.Value > 0 && pageSize.Value > 0)
            {
                sortedAnimals = sortedAnimals
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    // 需要重新指定排序类型避免 IOrderedEnumerable 隐式转换错误
                    .OrderBy(x => x.Distance); 
            }

            return sortedAnimals.Select(x => new AnimalDto
                {
                    Id = x.Animal.Id,
                    Name = x.Animal.Name,
                    Species = x.Animal.Species,
                    BreedId = x.Animal.BreedId,
                    BreedName = x.Animal.Breed?.Name,
                    Gender = x.Animal.Gender,
                    Size = x.Animal.Size,
                    EstAge = x.Animal.EstAge,
                    ImageUrls = x.Animal.Images.Select(i => i.ImageUrl).ToList(),
                    Status = x.Animal.Status,
                    IsVaccinated = x.Animal.IsVaccinated,
                    IsNeutered = x.Animal.IsNeutered,
                    
                    // [REF UPDATED]: Map Campaigns
                    Campaigns = x.Animal.Campaigns.Select(c => new CampaignDto
                    {
                        Id = c.Id,
                        AnimalId = c.AnimalId,
                        Title = c.Title,
                        Type = c.Type,
                        TargetAmount = c.TargetAmount,
                        CurrentAmount = c.CurrentAmount,
                        Status = c.Status
                    }).ToList(),

                    LocationName = x.Animal.LocationName,
                    Latitude = x.Animal.Latitude,
                    Longitude = x.Animal.Longitude,
                    DistanceInKm = Math.Round(x.Distance, 2)
                })
                .ToList();
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; 
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1); 
            var a = 
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * 
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2); 
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)); 
            return R * c; 
        }

        private static double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
        

        public async Task<(bool Success, string Message)> DeleteAnimalAsync(int id, int userId, string userRole)
        {
            var animal = await _context.Animals
                .Include(a => a.Campaigns)
                .Include(a => a.Images)
                .Include(a => a.Sightings)
                .Include(a => a.FeedingLogs)
                .Include(a => a.MedicalTasks)
                .Include(a => a.HealthUpdateProofs)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (animal == null) return (false, "Animal not found");

            // Permission Check
            if (userRole != "Admin")
            {
                // Shelter can only delete their own animals
                if (userRole == "Shelter" && animal.ShelterId != userId)
                {
                    return (false, "Forbidden: You can only delete animals you created.");
                }
                // Others cannot delete
                if (userRole != "Shelter")
                {
                    return (false, "Forbidden: Only Admin or the owner Shelter can delete animals.");
                }
            }

            // Financial Safety Check
            // If any campaign has received ANY funds, block deletion to preserve financial records.
            if (animal.Campaigns != null && animal.Campaigns.Any(c => c.CurrentAmount > 0))
            {
                return (false, "Cannot delete animal: Active donations exist. Financial records must be preserved.");
            }

            // Soft Delete: Mark as Inactive
            animal.Status = "Inactive";

            // Cancel/Close all active campaigns
            if (animal.Campaigns != null)
            {
                foreach (var campaign in animal.Campaigns)
                {
                    if (campaign.Status == "Active")
                    {
                        campaign.Status = "Closed";
                    }
                }
            }

            // [MODIFIED] Do NOT delete images from disk. Keep them for record.
            /*
            if (animal.Images != null)
            {
                foreach (var img in animal.Images)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var relativePath = img.ImageUrl.TrimStart('/');
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                        if (File.Exists(fullPath))
                        {
                            try { File.Delete(fullPath); } catch {  }
                        }
                    }
                }
            }
            */
            
            // Update record instead of removing
            _context.Animals.Update(animal);
            await _context.SaveChangesAsync();

            return (true, "Animal marked as Inactive. Data preserved.");
        }

        public async Task<List<object>> GetMyAnimalsAsync(int userId)
        {
            return await _context.Animals
                .Where(a => a.OwnerId == userId)
                .Include(a => a.Breed)
                .Include(a => a.Images)
                .Select(a => new
                {
                    AnimalId = a.Id,
                    Name = a.Name,
                    Species = a.Species,
                    Breed = a.Breed != null ? a.Breed.Name : "Unknown",
                    Gender = a.Gender,
                    Size = a.Size,
                    EstAge = a.EstAge,
                    Status = a.Status, // Should be 'Adopted'
                    IsVaccinated = a.IsVaccinated,
                    IsNeutered = a.IsNeutered,
                    LocationName = a.LocationName,
                    PrimaryImage = a.Images.FirstOrDefault(img => img.IsPrimary) != null 
                        ? a.Images.FirstOrDefault(img => img.IsPrimary).ImageUrl 
                        : a.Images.FirstOrDefault() != null 
                            ? a.Images.FirstOrDefault().ImageUrl 
                            : null
                })
                .ToListAsync<object>();
        }

        public async Task<List<AnimalDto>> GetAnimalsByShelterIdAsync(int shelterProfileId, int pageNumber = 1, int pageSize = 10)
        {
            // 1. 先通过传入的 shelterProfileId (如 2006) 找到其对应的 UserId (如 2015)
            var actualUserId = await _context.ShelterProfiles
                .Where(sp => sp.Id == shelterProfileId)
                .Select(sp => sp.UserId)
                .FirstOrDefaultAsync();

            // 如果找不到对应的收容所配置，直接返回空列表
            if (actualUserId == 0)
            {
                return new List<AnimalDto>();
            }

            // 2. 用真正的 UserId 去 Animals 表里匹配 (因为 Animals.ShelterId 存的是 UserId)
            var query = _context.Animals
                .Include(a => a.Breed)
                .Include(a => a.Images)
                .Include(a => a.Campaigns)
                .Where(a => a.ShelterId == actualUserId && a.Status != "Inactive" && a.Status != "Adopted");

            var pagedAnimals = await query
                .OrderByDescending(a => a.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return pagedAnimals.Select(a => new AnimalDto
            {
                Id = a.Id,
                Name = a.Name,
                Species = a.Species,
                BreedId = a.BreedId,
                BreedName = a.Breed?.Name,
                Gender = a.Gender,
                Size = a.Size,
                EstAge = a.EstAge,
                ImageUrls = a.Images.Select(i => i.ImageUrl).ToList(),
                Status = a.Status,
                IsVaccinated = a.IsVaccinated,
                IsNeutered = a.IsNeutered,
                Campaigns = a.Campaigns?.Select(c => new CampaignDto
                {
                    Id = c.Id,
                    AnimalId = c.AnimalId,
                    Title = c.Title,
                    Type = c.Type,
                    TargetAmount = c.TargetAmount,
                    CurrentAmount = c.CurrentAmount,
                    Status = c.Status
                }).ToList() ?? new List<CampaignDto>(),
                LocationName = a.LocationName,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                DistanceInKm = 0
            }).ToList();
        }
    }
    
}
