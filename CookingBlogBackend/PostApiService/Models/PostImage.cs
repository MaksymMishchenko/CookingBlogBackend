using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class PostImage : IEntity
    {
        [Required]
        public int Id { get; set; }
        
        public int? PostId { get; set; }

        [ValidateNever]
        public Post? Post { get; set; }

        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = default!;

        [MaxLength(100)]
        public string PublicId { get; set; } = default!;

        [MaxLength(200)]
        public string Alt { get; set; } = default!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
