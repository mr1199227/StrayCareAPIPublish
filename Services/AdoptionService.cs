using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class AdoptionService : IAdoptionService
    {
        private readonly ApplicationDbContext _context;

        public AdoptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> SubmitApplicationAsync(int userId, AdoptionApplicationDto dto)
        {
            // Check if animal exists
            var animal = await _context.Animals.FindAsync(dto.AnimalId);
            if (animal == null)
            {
                return (false, "Animal not found.");
            }

            // ALLOW adoption of "Stray" animals. 
            // If it's "Adopted" or "Inactive" (Soft Deleted), we should block it.
            if (animal.Status == "Adopted" || animal.Status == "Inactive")
            {
                return (false, "This animal is not available for adoption.");
            }

            // Check if user already applied
            var existingApp = await _context.AdoptionApplications
                .FirstOrDefaultAsync(a => a.AnimalId == dto.AnimalId && a.ApplicantUserId == userId && a.Status == "Pending");
            
            if (existingApp != null)
            {
                return (false, "You already have a pending application for this animal.");
            }

            var application = new AdoptionApplication
            {
                AnimalId = dto.AnimalId,
                ApplicantUserId = userId,
                // If animal.ShelterId is null (Stray), application.ShelterId checks null -> Admin review
                // If animal.ShelterId is set (Sheltered), application.ShelterId checks set -> Shelter review
                ShelterId = animal.ShelterId,
                Message = dto.Message,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.AdoptionApplications.Add(application);
            await _context.SaveChangesAsync();

            return (true, "Adoption application submitted successfully.");
        }
        

        public async Task<List<object>> GetMyApplicationsAsync(int userId)
        {
            return await _context.AdoptionApplications
                .Where(a => a.ApplicantUserId == userId)
                .Include(a => a.Animal)
                .ThenInclude(an => an.Images)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    ApplicationId = a.Id,
                    AnimalId = a.AnimalId,
                    AnimalName = a.Animal.Name,
                    AnimalImage = a.Animal.Images.FirstOrDefault() != null ? a.Animal.Images.FirstOrDefault().ImageUrl : null,
                    Status = a.Status,
                    Message = a.Message,
                    CreatedAt = a.CreatedAt,
                    ShelterName = a.ShelterId.HasValue ? a.Shelter.Username : "Stray Care Admin"
                })
                .ToListAsync<object>();
        }
    }
}
