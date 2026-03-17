using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrayCareAPI.Models
{
    public class AnimalImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        // 导航属性
        [ForeignKey("AnimalId")]
        public virtual Animal Animal { get; set; } = null!;
    }
}
