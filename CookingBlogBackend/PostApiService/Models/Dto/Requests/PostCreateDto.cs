using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PostCreateDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        public string Title { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        public string Description { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        public string Content { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        public string PreviewImageUrl { get; init; } = default!;

        [MaxLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? PreviewImageAlt { get; init; }

        [MaxLength(100, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaTitle { get; init; }

        [StringLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaDescription { get; init; }

        [Required(ErrorMessage = Global.Validation.Required)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = Global.Validation.SlugFormat)]
        public string Slug { get; init; } = default!;

        [Range(1, int.MaxValue, ErrorMessage = Global.Validation.InvalidCategory)]
        public int CategoryId { get; init; }
        public bool IsActive { get; init; } = false;
    }
}