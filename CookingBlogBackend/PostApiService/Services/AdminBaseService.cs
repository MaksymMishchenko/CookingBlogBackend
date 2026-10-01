using PostApiService.Infrastructure.Services;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Services
{
    public abstract class AdminBaseService : BaseService
    {
        protected AdminBaseService(IWebContext webContext) : base(webContext) { }

        /// <summary>
        /// Validates that the user is an Admin or Contributor. Returns error Result or null if valid.
        /// </summary>
        protected Result? ValidateAdminOrContributor()
        {
            var userId = WebContext?.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (WebContext?.IsAdmin != true && WebContext?.IsContributor != true)
            {
                return Forbidden();
            }

            return null;
        }

        /// <summary>
        /// Validates that the user is an Admin or Contributor (Generic).
        /// </summary>
        protected Result<T>? ValidateAdminOrContributor<T>()
        {
            var userId = WebContext?.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<T>();
            }

            if (WebContext?.IsAdmin != true && WebContext?.IsContributor != true)
            {
                return Forbidden<T>();
            }

            return null;
        }

        /// <summary>
        /// Validates that the user is exclusively an Admin.
        /// </summary>
        protected Result? ValidateAdminOnly()
        {
            var userId = WebContext?.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (WebContext?.IsAdmin != true)
            {
                return Forbidden();
            }

            return null;
        }

        /// <summary>
        /// Validates that the user is exclusively an Admin (Generic).
        /// </summary>
        protected Result<T>? ValidateAdminOnly<T>()
        {
            var userId = WebContext?.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<T>();
            }

            if (WebContext?.IsAdmin != true)
            {
                return Forbidden<T>();
            }

            return null;
        }
    }
}