using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Start working Login");
            // Приклад перевірки користувача (зробіть більш надійну перевірку)
            if (model.Username == "test@mail.ua" && model.Password == "123456")
            {
                var claims = new[]
                {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(ClaimTypes.Role, "User") };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var expires = DateTime.Now.AddMinutes(30);
                var token = new JwtSecurityToken(
                    issuer: _configuration["JwtSettings:Issuer"],
                    audience: _configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expires
                });
            }

            return Unauthorized();
        }
    }
}
