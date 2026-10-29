using PostApiService.Infrastructure.Services;
using PostApiService.Repositories;

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

        /// <summary>
        /// Validates the uniqueness of the title and slug for posts (or other entities).
        /// </summary>
        protected async Task<Result<T>?> ValidatePostUniquenessAsync<T>(
            IPostRepository postRepository,
            string cleanTitle,
            string cleanSlug,           
            CancellationToken ct = default,
            int? excludePostId = null)
        {
            var cleanTitleLower = cleanTitle.ToLower();
            var cleanSlugLower = cleanSlug.ToLower();

            bool titleExists = excludePostId.HasValue
                ? await postRepository.AnyAsync(p => p.Title.ToLower() == cleanTitleLower && p.Id != excludePostId.Value, ct)
                : await postRepository.AnyAsync(p => p.Title.ToLower() == cleanTitleLower, ct);

            if (titleExists)
            {
                return Conflict<T>(
                    string.Format(PostM.Errors.PostTitleAlreadyExist, cleanTitle),
                    PostM.Errors.PostTitleAlreadyExistCode);
            }

            bool slugExists = excludePostId.HasValue
                ? await postRepository.AnyAsync(p => p.Slug.ToLower() == cleanSlugLower && p.Id != excludePostId.Value, ct)
                : await postRepository.AnyAsync(p => p.Slug.ToLower() == cleanSlugLower, ct);

            if (slugExists)
            {
                return Conflict<T>(
                    string.Format(PostM.Errors.PostSlugAlreadyExist, cleanSlug),
                    PostM.Errors.PostSlugAlreadyExistCode);
            }

            return null;
        }
    }
}