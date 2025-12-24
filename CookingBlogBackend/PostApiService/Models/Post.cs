using Microsoft.EntityFrameworkCore;
using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostApiService.Models
{
    [Index(nameof(Slug), IsUnique = true)]
    public class Post : IEntity
    {
        [Column("PostId")]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = default!;

        [MaxLength(250)]
        public string Description { get; set; } = default!;

        [MaxLength(2500)]
        public string Content { get; set; } = default!;

        [MaxLength(50)]
        public string Author { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string ImageUrl { get; set; } = default!;

        [MaxLength(100)]
        public string MetaTitle { get; set; } = default!;

        [MaxLength(200)]
        public string MetaDescription { get; set; } = default!;

        [MaxLength(200)]
        public string Slug { get; set; } = default!;

        public int CategoryId { get; set; }

        public virtual Category Category { get; set; } = default!;
        public IList<Comment> Comments { get; set; } = new List<Comment>();
    }
}