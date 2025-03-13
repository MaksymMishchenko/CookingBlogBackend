using System.Security.Claims;

namespace PostApiService.Interfaces
{
    public interface ITokenService
    {
        string GenerateTokenString(IEnumerable<Claim> claims);
    }
}
