using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class Post
    {
        public int PostId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(50, MinimumLength = 10, ErrorMessage = "Title must be between 10 and 50 characters.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(250, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 250 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, MinimumLength = 20, ErrorMessage = "Content must be between 20 and 500 characters.")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Author must be between 3 and 50 characters.")]
        public string? Author { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow.ToLocalTime();

        [Required(ErrorMessage = "Image URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string? ImageUrl { get; set; }

        [MaxLength(100, ErrorMessage = "Meta title cannot exceed 100 characters.")]
        public string? MetaTitle { get; set; }

        [StringLength(200, ErrorMessage = "Meta description cannot exceed 200 characters.")]
        public string? MetaDescription { get; set; }

        [Required(ErrorMessage = "Slug is required.")]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must only contain lowercase letters, numbers, and hyphens.")]
        public string Slug { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
