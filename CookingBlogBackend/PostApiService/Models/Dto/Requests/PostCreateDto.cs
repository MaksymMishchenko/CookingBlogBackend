using PostApiService.Controllers.Filters;
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

        [Display(Name = "Preview image")]
        [AllowedFile(Global.FileSettings.MaxPostImageSizeMb)]
        public IFormFile? PreviewImage { get; init; }

        [MaxLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? PreviewImageAlt { get; init; }

        [MaxLength(100, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaTitle { get; init; }

        [StringLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaDescription { get; init; }

        [Required(ErrorMessage = Global.Validation.Required)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = Global.Validation.SlugFormat)]
        public string Slug { get; init; } = default!;

        [Display(Name = "Category")]
        [Required(ErrorMessage = Global.Validation.Required)]
        [Range(1, int.MaxValue, ErrorMessage = Global.Validation.InvalidCategory)]
        public int? CategoryId { get; init; }

        [Display(Name = "Active status")]
        [Required(ErrorMessage = Global.Validation.Required)]
        public bool? IsActive { get; init; }
    }
}