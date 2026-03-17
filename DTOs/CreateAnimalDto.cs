using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StrayCareAPI.DTOs
{
    public class CreateAnimalDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // BreedId 是必填的，Species 会自动从 Breed 中获取
        [Required(ErrorMessage = "必须选择品种")]
        public int? BreedId { get; set; }

        public bool IsNeutered { get; set; } = false;

        [StringLength(20)]
        public string? Gender { get; set; }  // 可选，默认 "Unknown"

        [StringLength(20)]
        public string? Size { get; set; }  // 可选，默认 "Unknown"

        [StringLength(50)]
        public string? EstAge { get; set; }  // 可选，默认 "Unknown"

        // 多文件上传（可选，最多 3 张）
        public List<IFormFile>? ImageFiles { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }  // 可选，默认 "Stray"

        [StringLength(100)]
        public string? LocationName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}
