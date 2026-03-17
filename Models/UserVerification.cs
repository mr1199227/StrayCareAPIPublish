using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public enum VerificationType
    {
        Email = 0,
        PasswordReset = 1,
        Approved = 2,
        Rejected = 3
    }

    public class UserVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        // 验证码 (OTP)。对于 Email/PasswordReset 是必填的
        [StringLength(10)]
        public string? Code { get; set; }

        public DateTime? ExpiryTime { get; set; }

        public VerificationType Type { get; set; }

        public bool IsUsed { get; set; } = false;

        // 记录备注信息（如：验证失败原因、手动拒绝理由、审批意见等）
        [StringLength(200)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
