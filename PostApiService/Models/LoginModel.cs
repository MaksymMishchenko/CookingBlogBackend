using System.ComponentModel.DataAnnotations;

namespace PostApiService.Models
{
    public class LoginModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [UIHint("password")]
        public string Password { get; set; }
    }
}
