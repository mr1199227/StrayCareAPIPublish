using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class CreateSightingDto
    {
        // 场景 A: 如果提供 AnimalId，表示更新现有动物
        public int? AnimalId { get; set; }

        // 场景 B: 如果 AnimalId 为 null，使用此字段创建新动物
        [StringLength(100)]
        public string? NewAnimalName { get; set; }

        // 品种 ID（仅在创建新动物时必填，用于自动推导 Species）
        public int? BreedId { get; set; }

        // 图片文件（用于目击记录，如果是新动物也用作主图）- Optional
        public IFormFile? ImageFile { get; set; }

        // 位置信息
        [Required]
        [StringLength(200)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string Gender { get; set; }

        public string Size { get; set; }

        // 描述
        [StringLength(500)]
        public string? Description { get; set; }

        // 报告者 ID (Backend will populate from Token)
        public int? ReporterUserId { get; set; }
    }
}

