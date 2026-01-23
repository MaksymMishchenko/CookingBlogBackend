using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class LoginUser
    {
        [Required(ErrorMessage = "UserName is required.")]
        [StringLength(50, ErrorMessage = "UserName cannot exceed 50 characters.")]
        public string UserName { get; set; } = default!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; } = default!;
    }
}
