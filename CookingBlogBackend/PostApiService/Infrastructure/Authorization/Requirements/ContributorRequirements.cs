using PostApiService.Models.TypeSafe;

namespace PostApiService.Infrastructure.Authorization.Requirements
{
    public class ContributorRequirements : IAuthorizationRequirement
    {
    }

    public class ContributorRequirementHandler : AuthorizationHandler<ContributorRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ContributorRequirements requirement)
        {
            var claims = context.User.Claims;

            var userPermission = AuthorizeHelper.GetPermissionFromClaim(TS.Controller.Comment, claims);

            if (context.User.IsInRole(TS.Roles.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (userPermission != null &&
                userPermission.Contains(TS.Permissions.Write) &&
                userPermission.Contains(TS.Permissions.Update) &&
                userPermission.Contains(TS.Permissions.Delete))
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