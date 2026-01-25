using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class LoginUserDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        [StringLength(50, ErrorMessage = Global.Validation.MaxLength)]
        public string UserName { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = Global.Validation.LengthRange)]
        public string Password { get; init; } = default!;
    }
}
