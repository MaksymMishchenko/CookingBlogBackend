using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Category : IEntity
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string Name { get; set; } = default!;

        [MaxLength(50)]
        [RegularExpression(@"^[a-z0-9-]+$")]
        public string Slug { get; set; } = default!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
