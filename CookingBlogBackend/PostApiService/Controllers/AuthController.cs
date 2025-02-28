using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly JwtConfiguration _config;

        public AuthController(
            IAuthService authService,
            IOptions<JwtConfiguration> config)
        {
            _authService = authService;
            _config = config.Value;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginUser credentials)
        {
            if (credentials == null)
            {
                return BadRequest("LoginCredentials");
            }

            if (await _authService.LoginAsync(credentials))
            {
                return Ok(new
                {
                    token = await _authService.GenerateTokenString(credentials.Username, _config)
                });
            }
            return BadRequest();
        }
    }
}

