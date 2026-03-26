using Microsoft.AspNetCore.Http;
using PostApiService.Infrastructure.Services;
using System.Security.Claims;

namespace PostApiService.Tests.Helper
{  
    public class TestWebContext : IWebContext
    {
        private readonly IHttpContextAccessor _accessor;

        private string? _manualUserId;
        private string? _manualUserName;
        private string? _manualIpAddress;
        private bool? _manualIsAdmin;

        public TestWebContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string? UserId
        {
            get => _manualUserId ?? _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            set => _manualUserId = value;
        }

        public string UserName
        {
            get => _manualUserName ?? _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? "TestUser";
            set => _manualUserName = value;
        }

        public string IpAddress
        {
            get => _manualIpAddress ?? _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            set => _manualIpAddress = value;
        }

        public bool IsAdmin
        {
            get => _manualIsAdmin ?? _accessor.HttpContext?.User?.IsInRole("Admin") ?? false;
            set => _manualIsAdmin = value;
        }
    }
}
