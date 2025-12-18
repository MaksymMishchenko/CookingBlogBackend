using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Category : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category is required.")] 
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Category must be between 3 and 20 characters.")]
        public string Name { get; set; } = default!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
