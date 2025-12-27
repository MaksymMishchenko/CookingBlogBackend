using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public record class CreateCategoryDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        [StringLength(20, MinimumLength = 3, ErrorMessage = Global.Validation.LengthRange)]
        public string Name { get; set; } = default!;
    }
}
