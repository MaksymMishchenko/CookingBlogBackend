using PostApiService.Helper;
using System.Security.Claims;

namespace PostApiService.Infrastructure.Authorization
{
    public class AuthorizeHelper
    {
        internal static IEnumerable<int> GetPermissionFromClaim(string controllerName, IEnumerable<Claim> claims)
        {
            if (!claims.Any(t => t.Type == controllerName))
            {
                return null;
            }
            return claims.Where(t => t.Type == controllerName).Select(t => t.Value.To<int>());
        }
    }
}
