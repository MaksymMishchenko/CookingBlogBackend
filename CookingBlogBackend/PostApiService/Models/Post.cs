using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Post : IEntity
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = default!;

        [Required]
        [MaxLength(2500)]
        public string Content { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string Author { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string ImageUrl { get; set; } = default!;

        [MaxLength(100)]
        public string? MetaTitle { get; set; }

        [MaxLength(200)]
        public string? MetaDescription { get; set; }

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; } = default!;

        public int CategoryId { get; set; }

        [ValidateNever]
        public virtual Category Category { get; set; } = default!;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public bool IsActive { get; set; } = true;
    }
}