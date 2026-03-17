using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StrayCareAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User"; // Admin/User/Volunteer/Shelter

        // 核心状态字段：
        // 1. 对于普通 User: 注册后默认为 false (需邮箱验证 OTP)，验证通过后变为 true。
        // 2. 对于 Shelter/Volunteer: 注册后默认为 false (需 Admin 审批)，审批通过后变为 true。
        public bool IsApproved { get; set; } = false; 

        // 账号状态：true=正常，false=被封禁
        public bool IsActive { get; set; } = true;

        public decimal Balance { get; set; } = 0;

        // 密码重置令牌
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // General Profile
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? ProfileImageUrl { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        // 导航属性
        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public virtual ICollection<MedicalTask> MedicalTasks { get; set; } = new List<MedicalTask>();
        
        public virtual VolunteerProfile? VolunteerProfile { get; set; }
        public virtual ShelterProfile? ShelterProfile { get; set; }
        public virtual ICollection<AdoptionApplication> AdoptionApplications { get; set; } = new List<AdoptionApplication>();
        public virtual ICollection<Sighting> ReportedSightings { get; set; } = new List<Sighting>();
    }
}
