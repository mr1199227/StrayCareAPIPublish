using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Models;

namespace StrayCareAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Animal> Animals { get; set; }
        public DbSet<AnimalImage> AnimalImages { get; set; }
        public DbSet<Breed> Breeds { get; set; }
        public DbSet<Sighting> Sightings { get; set; }
        public DbSet<FeedingLog> FeedingLogs { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<UserVerification> UserVerifications { get; set; }
        public DbSet<DonationCampaign> DonationCampaigns { get; set; }
        public DbSet<HealthUpdateProof> HealthUpdateProofs { get; set; }
        public DbSet<MedicalTask> MedicalTasks { get; set; }
        public DbSet<ShelterProfile> ShelterProfiles { get; set; }
        public DbSet<VolunteerProfile> VolunteerProfiles { get; set; }
        public DbSet<AdoptionApplication> AdoptionApplications { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 Donation 关系
            // [REF CHANGED]: Donation links to Campaign, not Animal directly
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Campaign)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 DonationCampaign 关系
            modelBuilder.Entity<DonationCampaign>()
                .HasOne(dc => dc.Animal)
                .WithMany(a => a.Campaigns)
                .HasForeignKey(dc => dc.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置 HealthUpdateProof 关系
            modelBuilder.Entity<HealthUpdateProof>()
                .HasOne(hp => hp.Animal)
                .WithMany(a => a.HealthUpdateProofs)
                .HasForeignKey(hp => hp.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 配置 MedicalTask 关系
            modelBuilder.Entity<MedicalTask>()
                .HasOne(m => m.Animal)
                .WithMany(a => a.MedicalTasks)
                .HasForeignKey(m => m.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalTask>()
                .HasOne(m => m.Volunteer)
                .WithMany(u => u.MedicalTasks)
                .HasForeignKey(m => m.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 Animal-Breed 关系
            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Breed)
                .WithMany(b => b.Animals)
                .HasForeignKey(a => a.BreedId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 AnimalImage 关系
            modelBuilder.Entity<AnimalImage>()
                .HasOne(ai => ai.Animal)
                .WithMany(a => a.Images)
                .HasForeignKey(ai => ai.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置 Sighting 关系
            modelBuilder.Entity<Sighting>()
                .HasOne(s => s.Animal)
                .WithMany(a => a.Sightings)
                .HasForeignKey(s => s.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sighting>()
                .HasOne(s => s.Reporter)
                .WithMany()
                .HasForeignKey(s => s.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 FeedingLog 关系
            modelBuilder.Entity<FeedingLog>()
                .HasOne(f => f.Animal)
                .WithMany(a => a.FeedingLogs)
                .HasForeignKey(f => f.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedingLog>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 AdoptionApplication 关系
            modelBuilder.Entity<AdoptionApplication>()
                .HasOne(a => a.Animal)
                .WithMany()
                .HasForeignKey(a => a.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdoptionApplication>()
                .HasOne(a => a.ApplicantUser)
                .WithMany()
                .HasForeignKey(a => a.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdoptionApplication>()
                .HasOne(a => a.Shelter)
                .WithMany()
                .HasForeignKey(a => a.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 Animal-Shelter 关系
            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Shelter)
                .WithMany()
                .HasForeignKey(a => a.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置 Decimal 精度
            modelBuilder.Entity<User>()
                .Property(u => u.Balance)
                .HasPrecision(18, 2);

            // 配置 Email 唯一索引
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // [REF REMOVED]: Old Animal Goal precision config
            // modelBuilder.Entity<Animal>().Property(a => a.GoalAmount).HasPrecision(18, 2);
            // modelBuilder.Entity<Animal>().Property(a => a.RaisedAmount).HasPrecision(18, 2);

            modelBuilder.Entity<DonationCampaign>()
                .Property(dc => dc.TargetAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DonationCampaign>()
                .Property(dc => dc.CurrentAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Donation>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .Property(w => w.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(w => w.User)
                .WithMany() // We can add collection to User if needed, but not required
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== 数据种子：品种 ==========
            modelBuilder.Entity<Breed>().HasData(
                // Dogs
                new Breed { Id = 1, Name = "Golden Retriever", Species = "Dog" },
                new Breed { Id = 2, Name = "German Shepherd", Species = "Dog" },
                new Breed { Id = 3, Name = "Labrador", Species = "Dog" },
                new Breed { Id = 4, Name = "Husky", Species = "Dog" },
                new Breed { Id = 5, Name = "Poodle", Species = "Dog" },
                new Breed { Id = 6, Name = "Bulldog", Species = "Dog" },
                new Breed { Id = 7, Name = "Beagle", Species = "Dog" },
                new Breed { Id = 8, Name = "Rottweiler", Species = "Dog" },
                new Breed { Id = 9, Name = "Chihuahua", Species = "Dog" },
                new Breed { Id = 10, Name = "Mixed Breed (Dog)", Species = "Dog" },

                // Cats
                new Breed { Id = 11, Name = "Persian", Species = "Cat" },
                new Breed { Id = 12, Name = "Maine Coon", Species = "Cat" },
                new Breed { Id = 13, Name = "Siamese", Species = "Cat" },
                new Breed { Id = 14, Name = "Ragdoll", Species = "Cat" },
                new Breed { Id = 15, Name = "British Shorthair", Species = "Cat" },
                new Breed { Id = 16, Name = "Sphynx", Species = "Cat" },
                new Breed { Id = 17, Name = "Bengal", Species = "Cat" },
                new Breed { Id = 18, Name = "Domestic Short Hair (Orange)", Species = "Cat" },
                new Breed { Id = 19, Name = "Mixed Breed (Cat)", Species = "Cat" }
            );
        }
    }
}
