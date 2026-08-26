using PostApiService.Helper;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class AdminPostService : BaseService, IAdminPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IHtmlSanitizationService _sanitizer;
        private readonly ICategoryService _categoryService;

        public AdminPostService(IPostRepository postRepository,
            IWebContext webContext,
            IHtmlSanitizationService sanitizer,
            ICategoryService categoryService
            ) : base(webContext)
        {
            _postRepository = postRepository;
            _sanitizer = sanitizer;
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves a paginated list of posts specifically for the administrative dashboard.
        /// Includes extended metadata such as update timestamps, publication status, and authorship.
        /// Supports full-text search, filtering by category slug, and filtering by activity status.
        /// </summary>
        public async Task<Result<PagedResult<AdminPostListDto>>> GetAdminPostsPagedAsync(
            AdminPostQueryDto postQuery, CancellationToken ct = default)
        {
            var userId = WebContext!.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<PagedResult<AdminPostListDto>>();
            }

            string? categoryName = null;

            if (!string.IsNullOrWhiteSpace(postQuery.CategorySlug))
            {
                categoryName = await _categoryService.GetNameBySlugAsync(postQuery.CategorySlug, ct);

                if (categoryName == null)
                {
                    return NotFound<PagedResult<AdminPostListDto>>(CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
                }
            }

            var query = _postRepository.GetFilteredPosts(postQuery.SearchTerm,
                postQuery.OnlyActive, postQuery.CategorySlug);

            var appliedFilters = new AppliedFilters(
              SearchTerm: postQuery.SearchTerm,
              CategoryName: categoryName
            );

            var result = await GetPagedDataAsync(query, appliedFilters, postQuery.PageNumber, postQuery.PageSize,
                PostMappingExtensions.ToAdminPostListDto, ct);

            return Success(result);
        }

        /// <summary>
        /// Retrieves detailed information for a specific post by its identifier for administrative use.
        /// </summary>       
        public async Task<Result<PostAdminDetailsDto>> GetPostByIdAsync(int postId, CancellationToken ct = default)
        {
            var postDto = await _postRepository.AsQueryable()
                .Where(p => p.Id == postId)
                .Select(PostMappingExtensions.ToAdminDetailsDto)
                .FirstOrDefaultAsync(ct);

            if (postDto == null)
            {
                Log.Warning(Posts.NotFound, postId);

                return NotFound<PostAdminDetailsDto>
                    (PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            return Success(postDto);
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default)
        {
            var userId = WebContext!.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<PostAdminDetailsDto>();
            }

            var sanitizedContent = _sanitizer.SanitizePost(postDto.Content);

            if (!string.Equals(postDto.Content, sanitizedContent, StringComparison.Ordinal))
            {
                var traceContent = postDto.Content.Truncate(500);
                Log.Warning(Security.XssDetectedOnPostCreate, postDto.Title, userId, WebContext.IpAddress, traceContent);
            }

            if (string.IsNullOrWhiteSpace(sanitizedContent))
            {
                return Invalid<PostAdminDetailsDto>(PostM.Errors.Empty, PostM.Errors.EmptyCode);
            }

            var cleanTitle = postDto.Title.StripHtml();
            var cleanSlug = postDto.Slug.StripHtml();

            var alreadyExists = await _postRepository
               .AnyAsync(p => p.Title == cleanTitle || p.Slug == cleanSlug, ct);

            if (alreadyExists)
            {
                return Conflict<PostAdminDetailsDto>(string.Format(
                    PostM.Errors.PostTitleOrSlugAlreadyExist, cleanTitle, cleanSlug),
                    PostM.Errors.PostAlreadyExistCode);
            }

            var categoryExists = await _categoryService.ExistsAsync(postDto.CategoryId, ct);

            if (!categoryExists)
            {
                Log.Warning(Posts.CategoryNotFound, postDto.CategoryId);

                return NotFound<PostAdminDetailsDto>
                    (CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
            }

            var postEntity = postDto.ToEntity(sanitizedContent);

            await _postRepository.AddAsync(postEntity, ct);
            await _postRepository.SaveChangesAsync(ct);

            Log.Information(Posts.Created, postEntity.Title, postEntity.Id);

            var responseDto = postEntity.MapToAdminDto();

            return Success(responseDto, PostM.Success.PostAddedSuccessfully);
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default)
        {
            var userId = WebContext!.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized<PostAdminDetailsDto>();
            }

            var sanitizedContent = _sanitizer.SanitizePost(postDto.Content);

            if (!string.Equals(postDto.Content, sanitizedContent, StringComparison.Ordinal))
            {
                var traceContent = postDto.Content.Truncate(500);
                Log.Warning(Security.XssDetectedOnPostUpdate, postId, userId, WebContext.IpAddress, traceContent);
            }

            if (string.IsNullOrWhiteSpace(sanitizedContent))
            {
                return Invalid<PostAdminDetailsDto>(PostM.Errors.Empty, PostM.Errors.EmptyCode);
            }

            var postEntity = await _postRepository.GetByIdAsync(postId, ct);

            if (postEntity == null)
            {
                Log.Warning(Posts.NotFound, postId);

                return NotFound<PostAdminDetailsDto>(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            var cleanTitle = postDto.Title.StripHtml();
            var cleanSlug = postDto.Slug.StripHtml();

            var alreadyExists = await _postRepository.AnyAsync(p => (p.Title == cleanTitle ||
            p.Slug == cleanSlug) && p.Id != postId, ct);

            if (alreadyExists)
            {
                Log.Information(Posts.AlreadyExists, cleanTitle, cleanSlug);

                return Conflict<PostAdminDetailsDto>(string.Format(
                    PostM.Errors.PostTitleOrSlugAlreadyExist, cleanTitle, cleanSlug), PostM.Errors.PostAlreadyExistCode);
            }

            if (postEntity.CategoryId != postDto.CategoryId)
            {
                var categoryExists = await _categoryService.ExistsAsync(postDto.CategoryId, ct);
                if (!categoryExists)
                {
                    Log.Warning(Posts.CategoryNotFound, postDto.CategoryId);

                    return NotFound<PostAdminDetailsDto>
                        (CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
                }
            }

            postDto.UpdateEntity(postEntity, sanitizedContent);
            await _postRepository.SaveChangesAsync(ct);

            Log.Information(Posts.Updated, postEntity.Title, postEntity.Id);

            var responseDto = postEntity.MapToAdminDto();

            return Success(responseDto, PostM.Success.PostUpdatedSuccessfully);
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>        
        public async Task<Result> DeletePostAsync(int postId, CancellationToken ct = default)
        {
            var userId = WebContext!.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var existingPost = await _postRepository.GetByIdAsync(postId, ct);

            if (existingPost == null)
            {
                return NotFound(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            await _postRepository.DeleteAsync(existingPost, ct);
            await _postRepository.SaveChangesAsync(ct);

            Log.Information(Posts.Deleted, postId);

            return Success(PostM.Success.PostDeletedSuccessfully);
        }
    }
}
