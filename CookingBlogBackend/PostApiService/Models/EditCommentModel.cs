using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class EditCommentModel
    {
        [Required(ErrorMessage = "Content is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 500 characters.")]
        public string Content { get; set; }
    }
}