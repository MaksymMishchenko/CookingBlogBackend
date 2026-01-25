using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models.Dto.Requests
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = Global.Validation.Required)]
        [StringLength(50, ErrorMessage = Global.Validation.MaxLength)]
        public string UserName { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        [EmailAddress(ErrorMessage = Global.Validation.InvalidEmail)]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = Global.Validation.InvalidEmail)]
        public string Email { get; init; } = default!;

        [Required(ErrorMessage = Global.Validation.Required)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = Global.Validation.LengthRange)]
        public string Password { get; init; } = default!;
    }
}
