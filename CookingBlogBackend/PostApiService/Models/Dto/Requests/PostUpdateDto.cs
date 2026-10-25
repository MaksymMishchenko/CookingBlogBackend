using PostApiService.Controllers.Filters;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class PostUpdateDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        public string Title { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        public string Description { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        public string Content { get; init; } = default!;

        [Display(Name = "Preview Image")]
        [AllowedFile(Global.FileSettings.MaxPostImageSizeMb, Required = false)]
        public IFormFile? PreviewImage { get; init; }

        [Display(Name = "Preview Image Alt")]
        [MaxLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? PreviewImageAlt { get; init; }

        [Display(Name = "Meta Title")]
        [MaxLength(100, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaTitle { get; init; }

        [Display(Name = "Meta Description")]
        [StringLength(200, ErrorMessage = Global.Validation.MaxLength)]
        public string? MetaDescription { get; init; }

        [Required(ErrorMessage = Global.Validation.Required)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = Global.Validation.SlugFormat)]
        public string Slug { get; init; } = default!;

        [Display(Name = "Category")]
        [Required(ErrorMessage = Global.Validation.Required)]
        [Range(1, int.MaxValue, ErrorMessage = Global.Validation.InvalidCategory)]
        public int? CategoryId { get; init; }

        public bool IsActive { get; init; } = true;
    }
}