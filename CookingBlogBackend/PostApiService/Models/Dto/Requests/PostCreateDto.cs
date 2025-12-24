using Newtonsoft.Json.Serialization;
using PostApiService.Infrastructure.Constants;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PostCreateDto
    {
        [Required(ErrorMessage = ValidationErrorMessages.Required)]
        [StringLength(200, MinimumLength = 10, ErrorMessage = ValidationErrorMessages.LengthRange)]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = ValidationErrorMessages.Required)]
        [StringLength(250, MinimumLength = 10, ErrorMessage = ValidationErrorMessages.LengthRange)]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = ValidationErrorMessages.Required)]
        [StringLength(2500, MinimumLength = 20, ErrorMessage = ValidationErrorMessages.LengthRange)]
        public string Content { get; set; } = default!;

        [Required(ErrorMessage = ValidationErrorMessages.Required)]

        [StringLength(50, MinimumLength = 3, ErrorMessage = ValidationErrorMessages.LengthRange)]
        public string Author { get; set; } = default!;

        [Required(ErrorMessage = ValidationErrorMessages.Required)]
        [Url(ErrorMessage = ValidationErrorMessages.InvalidUrl)]
        public string ImageUrl { get; set; } = default!;

        [MaxLength(100, ErrorMessage = ValidationErrorMessages.MaxLength)]
        public string? MetaTitle { get; set; } = default!;

        [StringLength(200, ErrorMessage = ValidationErrorMessages.MaxLength)]
        public string? MetaDescription { get; set; } = default!;

        [Required(ErrorMessage = ValidationErrorMessages.Required)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = ValidationErrorMessages.SlugFormat)]
        public string Slug { get; set; } = default!;

        [Range(1, int.MaxValue, ErrorMessage = ValidationErrorMessages.InvalidCategory)]
        public int CategoryId { get; set; }
    }
}
