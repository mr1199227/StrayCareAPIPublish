using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.Models
{
    public class Breed
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Species { get; set; } = "Dog"; // "Dog" or "Cat"

        // 导航属性
        public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();
    }
}
