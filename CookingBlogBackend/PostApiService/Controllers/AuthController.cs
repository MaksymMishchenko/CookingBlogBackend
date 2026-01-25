using PostApiService.Controllers.Filters;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user in the system by accepting user credentials.               
        /// </summary>        
        [AllowAnonymous]
        [HttpPost("Register")]
        [ValidateModel(InvalidErrorMessage = Auth.Registration.Errors.InvalidRegistrationData)]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto userDto,
            CancellationToken ct = default)
        {
            var result = await _authService.RegisterUserAsync(userDto);

            return result.ToActionResult();
        }

        /// <summary>
        /// Authenticates a user based on the provided credentials and returns the authentication result.
        /// </summary>       
        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateModel(InvalidErrorMessage = Auth.LoginM.Errors.InvalidCredentials)]
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginUserDto credentials,
            CancellationToken ct = default)
        {
            var result = await _authService.AuthenticateAsync(credentials);

            return result.ToActionResult();
        }
    }
}

