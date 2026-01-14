using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class CommentUpdateDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        [MaxLength(500, ErrorMessage = Global.Validation.MaxLength)]
        public string Content { get; set; } = default!;
    }
}
