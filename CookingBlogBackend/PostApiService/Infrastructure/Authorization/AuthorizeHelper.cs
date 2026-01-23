using PostApiService.Helper;
using System.Security.Claims;

namespace PostApiService.Infrastructure.Authorization
{
    public class AuthorizeHelper
    {
        internal static IEnumerable<int> GetPermissionFromClaim(string controllerName, IEnumerable<Claim> claims)
        {
            var relevantClaims = claims.Where(t => t.Type == controllerName).ToList();

            if (!relevantClaims.Any())
            {
                return Enumerable.Empty<int>();
            }

            return relevantClaims.Select(t => t.Value.To<int>());
        }
    }
}
