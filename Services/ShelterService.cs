using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class ShelterService : IShelterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;

        public ShelterService(ApplicationDbContext context, IEmailService emailService, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<(bool Success, string Message, object? Data)> IntakeAnimalAsync(int animalId, int userId, ShelterIntakeDto dto)
        {
            // 1. Fetch Profile
            var shelterProfile = await _context.ShelterProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId);
            if (shelterProfile == null)
            {
                 return (false, "Shelter profile not found.", null);
            }

            // 2. Update Animal
            var animal = await _context.Animals.FindAsync(animalId);
            if (animal == null)
            {
                 return (false, "Animal not found.", null);
            }

            // Check for pending adoption applications
            // Use ToLower() to be safe against casing issues
            var hasPendingApplications = await _context.AdoptionApplications
                .AnyAsync(a => a.AnimalId == animalId && a.Status.ToLower() == "pending");

            if (hasPendingApplications)
            {
                return (false, "This animal has pending adoption applications. Please wait for them to be processed.", null);
            }

            animal.Status = "Sheltered";
            animal.ShelterId = shelterProfile.UserId;
            animal.Latitude = shelterProfile.Latitude;
            animal.Longitude = shelterProfile.Longitude;
            animal.LocationName = shelterProfile.Address;

            // 3. Update Image
            if (dto.NewProfileImage != null && dto.NewProfileImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(dto.NewProfileImage.FileName).ToLowerInvariant();
                
                if (allowedExtensions.Contains(extension))
                {
                    var uploadResult = await _cloudinaryService.UploadImageAsync(dto.NewProfileImage, "straycare/animals");
                    if (!uploadResult.Success)
                    {
                        return (false, uploadResult.Message, null);
                    }
                    var imageUrl = uploadResult.SecureUrl;
                    
                    var animalImage = new AnimalImage
                    {
                        AnimalId = animal.Id,
                        ImageUrl = imageUrl,
                        IsPrimary = true // Set as primary
                    };

                    // Unset other primaries
                    var existingImages = await _context.AnimalImages.Where(ai => ai.AnimalId == animal.Id).ToListAsync();
                    foreach (var img in existingImages) img.IsPrimary = false;

                    _context.AnimalImages.Add(animalImage);
                }
            }

            await _context.SaveChangesAsync();

            return (true, "Animal intake successful.", new { animalId = animal.Id });
        }

        public async Task<List<object>> GetApplicationsAsync(int userId)
        {
            var applications = await _context.AdoptionApplications
                .Include(a => a.Animal)
                .Include(a => a.ApplicantUser)
                .Where(a => a.ShelterId == userId || a.Animal.ShelterId == userId) // Fallback
                .Select(a => new 
                {
                    ApplicationId = a.Id,
                    AnimalName = a.Animal.Name,
                    ApplicantName = a.ApplicantUser.Username,
                    Status = a.Status,
                    Message = a.Message,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync<object>();

            return applications;
        }

        public async Task<(bool Success, string Message)> ApproveApplicationAsync(int appId, int userId)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.Animal)
                .Include(a => a.ApplicantUser)
                .FirstOrDefaultAsync(a => a.Id == appId);

            if (application == null) return (false, "Application not found.");

            // Verify ownership
            if (application.Animal.ShelterId != userId)
            {
                return (false, "You do not have permission to approve this application.");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Change App Status
                    application.Status = "Approved";

                    // 2. Change Animal Status
                    application.Animal.Status = "Adopted";
                    application.Animal.OwnerId = application.ApplicantUserId; // [NEW] Set Owner

                    // ==========================================
                    // [NEW] 嫁妆逻辑：转移捐款与清理遗留医疗任务
                    // ==========================================

                    // 1. 转移筹款余额到领养人钱包并关闭筹款
                    var animalCampaigns = await _context.DonationCampaigns
                        .Where(c => c.AnimalId == application.AnimalId && c.CurrentAmount > 0)
                        .ToListAsync();

                    decimal totalDowry = 0;
                    foreach (var campaign in animalCampaigns)
                    {
                        totalDowry += campaign.CurrentAmount;
                        campaign.CurrentAmount = 0; // 资金已转移，清零防复用
                        campaign.Status = "Closed"; // 动物已领养，关闭筹款
                    }

                    if (totalDowry > 0)
                    {
                        application.ApplicantUser.Balance += totalDowry; // 直接打入领养人钱包
                    }

                    // 2. 取消还没人接单的医疗任务 (修复前端志愿者大厅的幽灵任务 Bug)
                    var pendingTasks = await _context.MedicalTasks
                        .Where(t => t.AnimalId == application.AnimalId && (t.Status == "Open" || t.Status == "ReadyForVolunteer"))
                        .ToListAsync();
            
                    foreach (var task in pendingTasks)
                    {
                        task.Status = "Cancelled";
                    }
                    // ==========================================

                    // 3. Reject others
                    var otherApps = await _context.AdoptionApplications
                        .Include(a => a.ApplicantUser)
                        .Where(a => a.AnimalId == application.AnimalId && a.Id != appId && a.Status == "Pending")
                        .ToListAsync();
                    
                    foreach(var app in otherApps)
                    {
                        app.Status = "Rejected";
                        app.RejectionReason = "Animal adopted by another applicant.";
                        
                        // Notify rejected
                        await _emailService.SendStatusUpdateAsync(
                            app.ApplicantUser.Email,
                            "Adoption Application Update",
                            $"Your application for {application.Animal?.Name ?? "the animal"} has been closed because the animal was adopted by another applicant.",
                            app.ApplicantUser.Username
                        );
                    }
                    
                    // Log Verification for winner
                     var verification = new UserVerification
                    {
                        UserId = application.ApplicantUserId,
                        Code = null, 
                        ExpiryTime = null,
                        Type = VerificationType.Approved,
                        IsUsed = true,
                        Remarks = $"Adoption Application {application.Id} for {application.Animal?.Name} Approved by Shelter",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserVerifications.Add(verification);

                    await _context.SaveChangesAsync();
                    
                    // 动态构建邮件内容
                    string emailMessage = $"Congratulations! Your application to adopt {application.Animal?.Name} has been APPROVED. Please contact us to arrange pickup.";

                    // 如果有嫁妆，追加惊喜提示
                    if (totalDowry > 0)
                    {
                        emailMessage += $" As a special gift, the community's raised fund of RM {totalDowry:F2} for {application.Animal?.Name} has been credited to your wallet to help with their future care!";
                    }

                    // 发送邮件
                    await _emailService.SendStatusUpdateAsync(
                        application.ApplicantUser.Email,
                        "Adoption Application Approved",
                        emailMessage,
                        application.ApplicantUser.Username
                    );

                    await transaction.CommitAsync();

                    return (true, "Application approved. Animal marked as Adopted.");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return (false, "Error processing approval.");
                }
            }
        }

        public async Task<(bool Success, string Message)> RejectApplicationAsync(int appId, int userId, RejectApplicationDto dto)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.Animal)
                .Include(a => a.ApplicantUser)
                .FirstOrDefaultAsync(a => a.Id == appId);

            if (application == null) return (false, "Application not found.");

             // Verify ownership
            if (application.Animal.ShelterId != userId)
            {
                return (false, "You do not have permission to reject this application.");
            }

            application.Status = "Rejected";
            application.RejectionReason = dto.Reason;
            
            // Log Verification
            var verification = new UserVerification
            {
                UserId = application.ApplicantUserId,
                Code = null, 
                ExpiryTime = null,
                Type = VerificationType.Rejected,
                IsUsed = true,
                Remarks = $"Adoption Application {application.Id} for {application.Animal?.Name} Rejected by Shelter: {dto.Reason}",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);

            await _context.SaveChangesAsync();
            
            // Notify User
            await _emailService.SendStatusUpdateAsync(
                application.ApplicantUser.Email,
                "Adoption Application Update",
                $"Your application for {application.Animal?.Name} has been rejected. Reason: {dto.Reason}",
                application.ApplicantUser.Username
            );

            return (true, "Application rejected.");
        }

        public async Task<List<object>> ViewOwnAnimalsAsync(int userId)
        {
            var animals = await _context.Animals
                .Include(a => a.Breed)
                .Include(a => a.Images)
                .Where(a => a.ShelterId == userId && a.Status != "Adopted" && a.OwnerId == null)
                .Select(a => new
                {
                    AnimalId = a.Id,
                    Name = a.Name,
                    Species = a.Species,
                    Breed = a.Breed != null ? a.Breed.Name : "Unknown",
                    Gender = a.Gender,
                    Size = a.Size,
                    EstAge = a.EstAge,
                    Status = a.Status,
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

            return animals;
        }

        public async Task<(bool Success, string Message, object? Data)> GetNearbyStraysAsync(int userId, double radiusInKm)
        {
            var shelterProfile = await _context.ShelterProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId);
            if (shelterProfile == null)
            {
                return (false, "Shelter profile or location not found. Please update your profile.", null);
            }

            double sLat = shelterProfile.Latitude;
            double sLon = shelterProfile.Longitude;

            var strays = await _context.Animals
                .Include(a => a.Images)
                .Include(a => a.Breed)
                .Include(a => a.Sightings)
                .Where(a => a.Status.ToLower() == "stray" && a.Latitude.HasValue && a.Longitude.HasValue)
                .ToListAsync();

            var nearbyStrays = strays
                .Select(a => new
                {
                    Animal = a,
                    Distance = CalculateDistance(sLat, sLon, a.Latitude!.Value, a.Longitude!.Value)
                })
                .Where(x => x.Distance <= radiusInKm)
                .OrderBy(x => x.Distance)
                .Select(x => new
                {
                    Id = x.Animal.Id,
                    Name = string.IsNullOrWhiteSpace(x.Animal.Name) ? "Unknown" : x.Animal.Name,
                    Species = x.Animal.Species,
                    Breed = x.Animal.Breed != null ? x.Animal.Breed.Name : "Unknown",
                    Gender = x.Animal.Gender,
                    LocationName = x.Animal.LocationName,
                    ImageUrl = x.Animal.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                        ?? x.Animal.Images.FirstOrDefault()?.ImageUrl,
                    DistanceKm = Math.Round(x.Distance, 2),
                    ReportedAt = x.Animal.Sightings
                        .OrderByDescending(s => s.Timestamp)
                        .Select(s => (DateTime?)s.Timestamp)
                        .FirstOrDefault()
                })
                .ToList();

            return (true, "Successfully retrieved nearby strays.", nearbyStrays);
        }

        public async Task<List<ShelterDto>> GetAllSheltersAsync()
        {
            var shelters = await _context.ShelterProfiles
                .Include(sp => sp.User)
                .Where(sp => sp.User.IsApproved && sp.User.IsActive) // Public only sees approved active shelters
                .Select(sp => new ShelterDto
                {
                    Id = sp.Id,
                    UserId = sp.UserId,
                    ShelterName = sp.ShelterName,
                    Address = sp.Address,
                    ProfileImageUrl = sp.User.ProfileImageUrl
                })
                .ToListAsync();

            return shelters;
        }

        public async Task<ShelterDetailDto?> GetShelterByIdAsync(int id)
        {
            var shelter = await _context.ShelterProfiles
                .Include(sp => sp.User)
                .Where(sp => sp.Id == id && sp.User.IsApproved && sp.User.IsActive)
                .Select(sp => new ShelterDetailDto
                {
                    Id = sp.Id,
                    UserId = sp.UserId,
                    ShelterName = sp.ShelterName,
                    Address = sp.Address,
                    ProfileImageUrl = sp.User.ProfileImageUrl,
                    
                    Description = sp.Description,
                    LicenseImageUrl = sp.LicenseImageUrl,
                    Latitude = sp.Latitude,
                    Longitude = sp.Longitude,
                    Email = sp.User.Email,
                    PhoneNumber = sp.User.PhoneNumber
                })
                .FirstOrDefaultAsync();

            return shelter;
        }

        public async Task<List<NearbyShelterDto>> GetNearbySheltersAsync(double latitude, double longitude, double radius = 15.0, int pageNumber = 1, int pageSize = 10)
        {
            var shelters = await _context.ShelterProfiles
                .Include(sp => sp.User)
                .Where(sp => sp.User.IsApproved && sp.User.IsActive)
                .ToListAsync();

            var sortedShelters = shelters
                .Select(sp => new
                {
                    Shelter = sp,
                    Distance = CalculateDistance(latitude, longitude, sp.Latitude, sp.Longitude)
                })
                .Where(x => x.Distance <= radius)
                .OrderBy(x => x.Distance);

            if (pageNumber > 0 && pageSize > 0)
            {
                sortedShelters = sortedShelters
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .OrderBy(x => x.Distance);
            }

            return sortedShelters.Select(x => new NearbyShelterDto
            {
                Id = x.Shelter.Id,
                UserId = x.Shelter.UserId,
                ShelterName = x.Shelter.ShelterName,
                Address = x.Shelter.Address,
                ProfileImageUrl = x.Shelter.User?.ProfileImageUrl,
                Latitude = x.Shelter.Latitude,
                Longitude = x.Shelter.Longitude,
                DistanceInKm = Math.Round(x.Distance, 2)
            }).ToList();
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371d; // 地球半径 (公里)
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
