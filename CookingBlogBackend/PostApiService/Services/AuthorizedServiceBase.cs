using PostApiService.Infrastructure.Services;

namespace PostApiService.Services
{
    public abstract class AuthorizedServiceBase : BaseService
    {
        protected AuthorizedServiceBase(IWebContext webContext) : base(webContext) { }

        protected Result? ValidateAuthorized()
        {
            return string.IsNullOrEmpty(WebContext?.UserId) ? Unauthorized() : null;
        }

        protected Result<T>? ValidateAuthorized<T>()
        {
            return string.IsNullOrEmpty(WebContext?.UserId) ? Unauthorized<T>() : null;
        }

        protected Result? ValidateOwnershipOrAdmin(string resourceOwnerId, string errorCode, string errorDescription)
        {
            var userId = WebContext?.UserId;
            var isAdmin = WebContext?.IsAdmin ?? false;

            if (resourceOwnerId != userId && !isAdmin)
            {
                return Forbidden(errorDescription, errorCode);
            }

            return null;
        }

        protected Result<T>? ValidateOwnershipOrAdmin<T>(string resourceOwnerId, string errorCode, string errorDescription)
        {
            var userId = WebContext?.UserId;
            var isAdmin = WebContext?.IsAdmin ?? false;

            if (resourceOwnerId != userId && !isAdmin)
            {
                return Forbidden<T>(errorDescription, errorCode);
            }

            return null;
        }
    }
}