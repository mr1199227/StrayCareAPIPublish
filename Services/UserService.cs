using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;

namespace StrayCareAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserDashboardStatsDto> GetDashboardStatsAsync(int userId)
        {
            var animalCount = await _context.Animals
                .AsNoTracking()
                .CountAsync(a => a.OwnerId == userId);

            var adoptionCount = await _context.AdoptionApplications
                .AsNoTracking()
                .CountAsync(a => a.ApplicantUserId == userId);

            var feedingCount = await _context.FeedingLogs
                .AsNoTracking()
                .CountAsync(f => f.UserId == userId);

            var totalDonated = await _context.Donations
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .SumAsync(d => (decimal?)d.Amount);

            return new UserDashboardStatsDto
            {
                AnimalCount = animalCount,
                AdoptionCount = adoptionCount,
                FeedingCount = feedingCount,
                TotalDonated = totalDonated ?? 0
            };
        }

        public async Task<List<object>> GetMyAnimalsAsync(int userId)
        {
            return await _context.Animals
                .AsNoTracking()
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
                    Status = a.Status,
                    IsVaccinated = a.IsVaccinated,
                    IsNeutered = a.IsNeutered,
                    LocationName = a.LocationName,
                    PrimaryImage = a.Images.FirstOrDefault(img => img.IsPrimary) != null
                        ? a.Images.FirstOrDefault(img => img.IsPrimary)!.ImageUrl
                        : a.Images.FirstOrDefault() != null
                            ? a.Images.FirstOrDefault()!.ImageUrl
                            : null
                })
                .ToListAsync<object>();
        }

        public async Task<List<object>> GetMyAdoptionsAsync(int userId)
        {
            return await _context.AdoptionApplications
                .AsNoTracking()
                .Where(a => a.ApplicantUserId == userId)
                .Include(a => a.Animal)
                .ThenInclude(an => an.Images)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    ApplicationId = a.Id,
                    AnimalId = a.AnimalId,
                    AnimalName = a.Animal.Name,
                    AnimalImage = a.Animal.Images.FirstOrDefault(img => img.IsPrimary) != null
                        ? a.Animal.Images.FirstOrDefault(img => img.IsPrimary)!.ImageUrl
                        : a.Animal.Images.FirstOrDefault() != null
                            ? a.Animal.Images.FirstOrDefault()!.ImageUrl
                            : null,
                    Status = a.Status,
                    Message = a.Message,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync<object>();
        }

        public async Task<List<object>> GetMyFeedingsAsync(int userId)
        {
            return await _context.FeedingLogs
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Include(f => f.Animal)
                .OrderByDescending(f => f.FedAt)
                .Select(f => new
                {
                    FeedingId = f.Id,
                    AnimalId = f.AnimalId,
                    AnimalName = f.Animal.Name,
                    FedAt = f.FedAt,
                    f.Description,
                    f.LocationName,
                    f.Latitude,
                    f.Longitude,
                    f.ProofImageUrl
                })
                .ToListAsync<object>();
        }

        public async Task<UserFinancialsDto> GetMyFinancialsAsync(int userId)
        {
            var donations = await _context.Donations
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Include(d => d.Campaign)
                .ThenInclude(c => c.Animal)
                .OrderByDescending(d => d.Timestamp)
                .Select(d => new UserDonationItemDto
                {
                    Id = d.Id,
                    Amount = d.Amount,
                    Timestamp = d.Timestamp,
                    CampaignId = d.CampaignId,
                    CampaignTitle = d.Campaign.Title,
                    AnimalId = d.Campaign.AnimalId,
                    AnimalName = d.Campaign.Animal != null ? d.Campaign.Animal.Name : null
                })
                .ToListAsync();

            var topups = await _context.WalletTransactions
                .AsNoTracking()
                .Where(w => w.UserId == userId && w.TransactionType == "Topup")
                .OrderByDescending(w => w.Timestamp)
                .Select(w => new UserTopupItemDto
                {
                    Id = w.Id,
                    Amount = w.Amount,
                    Timestamp = w.Timestamp,
                    TransactionType = w.TransactionType,
                    Description = w.Description
                })
                .ToListAsync();

            return new UserFinancialsDto
            {
                Donations = donations,
                Topups = topups
            };
        }

        public async Task<List<UserRecentActivityDto>> GetRecentActivitiesAsync(int userId, int perTypeLimit = 10, int finalLimit = 10)
        {
            var adoptions = await _context.AdoptionApplications
                .AsNoTracking()
                .Where(a => a.ApplicantUserId == userId)
                .Include(a => a.Animal)
                .OrderByDescending(a => a.CreatedAt)
                .Take(perTypeLimit)
                .ToListAsync();

            var feedings = await _context.FeedingLogs
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Include(f => f.Animal)
                .OrderByDescending(f => f.FedAt)
                .Take(perTypeLimit)
                .ToListAsync();

            var donations = await _context.Donations
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Include(d => d.Campaign)
                .OrderByDescending(d => d.Timestamp)
                .Take(perTypeLimit)
                .ToListAsync();

            var adoptionActivities = adoptions.Select(a => new UserRecentActivityDto
            {
                Type = "Adoption",
                Description = $"Applied to adopt {a.Animal.Name} (status: {a.Status})",
                Date = a.CreatedAt,
                Amount = null,
                SourceId = a.Id
            });

            var feedingActivities = feedings.Select(f => new UserRecentActivityDto
            {
                Type = "Feeding",
                Description = $"Fed {f.Animal.Name}",
                Date = f.FedAt,
                Amount = null,
                SourceId = f.Id
            });

            var donationActivities = donations.Select(d => new UserRecentActivityDto
            {
                Type = "Donation",
                Description = $"Donated to {d.Campaign.Title}",
                Date = d.Timestamp,
                Amount = d.Amount,
                SourceId = d.Id
            });

            return adoptionActivities
                .Concat(feedingActivities)
                .Concat(donationActivities)
                .OrderByDescending(x => x.Date)
                .Take(finalLimit)
                .ToList();
        }
    }
}
