using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PostApiService.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class Comment : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 500 characters.")]
        public string Content { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PostId { get; set; }

        [ValidateNever]
        public string UserId { get; set; } = default!;

        [ValidateNever]
        public virtual IdentityUser User { get; set; } = default!;

        [JsonIgnore]
        public Post? Post { get; set; }
    }
}
