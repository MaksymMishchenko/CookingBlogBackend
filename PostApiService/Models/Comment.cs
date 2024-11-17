using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Comment
    {
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(500, ErrorMessage = "Author cannot exceed 25 characters.")]
        public string? Author { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, ErrorMessage = "Content cannot exceed 500 characters.")]
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public int PostId { get; set; }
        public Post? Post { get; set; }
    }
}
