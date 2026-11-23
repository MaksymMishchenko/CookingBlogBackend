using PostApiService.Controllers.Filters;
using PostApiService.Infrastructure.Constants;
using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class UploadContentImageDto
    {
        [Display(Name = "Content Image")]
        [Required(ErrorMessage = Global.Validation.Required)]
        [AllowedFile(maxFileSizeMb: StorageConstants.Limits.MaxImageSizeMb, Required = true)]
        public IFormFile ContentImage { get; set; } = default!;

        [Display(Name = "Content Image Alt")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = Global.Validation.StringLength)]
        public string? Alt { get; set; }
    }
}
