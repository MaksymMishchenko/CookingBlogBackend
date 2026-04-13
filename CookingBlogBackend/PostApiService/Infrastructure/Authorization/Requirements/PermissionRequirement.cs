using PostApiService.Models.TypeSafe;

namespace PostApiService.Infrastructure.Authorization.Requirements
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Resource { get; }
        public PermissionRequirement(string resource) => Resource = resource;
    }

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {            
            if (context.User.IsInRole(TS.Roles.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            var claims = context.User.Claims;
            
            var userPermissions = AuthorizeHelper.GetPermissionFromClaim(requirement.Resource, claims);

            if (userPermissions != null &&
                userPermissions.Contains(TS.Permissions.Write) &&
                userPermissions.Contains(TS.Permissions.Update) &&
                userPermissions.Contains(TS.Permissions.Delete))
            {
                context.Succeed(requirement);
            }
            else
            {              
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}