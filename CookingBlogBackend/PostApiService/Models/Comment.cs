using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PostApiService.Models
{
    public class Comment
    {
        public int CommentId { get; set; }

        [StringLength(50, MinimumLength = 1, ErrorMessage = "Author must be between 1 and 50 characters.")]
        public string? Author { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 500 characters.")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

        public int PostId { get; set; }

        [ValidateNever]
        public string UserId { get; set; }

        [JsonIgnore]
        public Post? Post { get; set; }
    }
}
