using Microsoft.AspNetCore.Identity;

namespace PostApiService.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime Expiration) GenerateJwtToken(IdentityUser user);
    }
}
