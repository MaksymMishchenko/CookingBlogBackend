using PostApiService.Models.TypeSafe;
using System.Security.Claims;

namespace PostApiService.Infrastructure.Services
{
    public class WebContext : IWebContext
    {
        private readonly IHttpContextAccessor _accessor;

        public WebContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string UserName => User?.Identity?.Name ?? UnknownUser;

        public bool IsAdmin => User?.IsInRole(TS.Roles.Admin) ?? false;

        public string IpAddress => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                                   ?? UnknownIp;
    }
}
