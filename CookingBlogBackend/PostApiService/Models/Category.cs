using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Category : IEntity
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string Name { get; set; } = default!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
