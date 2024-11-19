using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Comment
    {
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [MaxLength(50, ErrorMessage = "Author cannot exceed 50 characters.")]
        public string? Author { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 500 characters.")]
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "PostId is required.")]
        public int PostId { get; set; }

        public Post? Post { get; set; }
    }
}
