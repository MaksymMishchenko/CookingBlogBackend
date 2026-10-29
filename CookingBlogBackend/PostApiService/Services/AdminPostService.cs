using PostApiService.Helper;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;

namespace PostApiService.Services
{
    public class AdminPostService : AdminBaseService, IAdminPostService
    {
        private readonly IPostRepository _postRepository;

        private readonly IHtmlSanitizationService _sanitizer;

        private readonly IPublicCategoryService _categoryService;

        private readonly IFileStorageService _fileStorageService;

        public AdminPostService(IPostRepository postRepository,
            IWebContext webContext,
            IHtmlSanitizationService sanitizer,
            IPublicCategoryService categoryService,
            IFileStorageService fileStorageService
            ) : base(webContext)
        {
            _postRepository = postRepository;
            _sanitizer = sanitizer;
            _categoryService = categoryService;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Retrieves a paginated list of posts specifically for the administrative dashboard.
        /// Includes extended metadata such as update timestamps, publication status, and authorship.
        /// Supports full-text search, filtering by category, filtering by activity status, and sorting.
        /// </summary>
        public async Task<Result<PagedResult<AdminPostListDto>>> GetAdminPostsPagedAsync(
            AdminPostQueryDto postQuery, CancellationToken ct = default)
        {
            var accessError = ValidateAdminOrContributor<PagedResult<AdminPostListDto>>();
            if (accessError != null) return accessError;

            var userId = WebContext!.UserId;
            bool isAdmin = WebContext.IsAdmin;

            var updatedQuery = isAdmin
                 ? postQuery
                 : postQuery with { AuthorId = userId };

            string? categoryName = null;

            if (updatedQuery.CategoryId.HasValue)
            {
                categoryName = await _categoryService.GetNameByIdAsync(updatedQuery.CategoryId.Value, ct);

                if (categoryName == null)
                {
                    return NotFound<PagedResult<AdminPostListDto>>(CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
                }
            }

            var query = _postRepository.GetAdminFilteredAndSortedPosts(
                updatedQuery.SearchTerm,
                updatedQuery.OnlyActive,
                updatedQuery.CategoryId,
                updatedQuery.SortBy,
                updatedQuery.SortDirection,
                updatedQuery.AuthorId
            );

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
            var accessError = ValidateAdminOrContributor<PostAdminDetailsDto>();
            if (accessError != null) return accessError;

            var userId = WebContext!.UserId;
            bool isAdmin = WebContext!.IsAdmin;

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

            if (!isAdmin && postDto.AuthorId != userId)
            {
                return Forbidden<PostAdminDetailsDto>();
            }

            return Success(postDto);
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default)
        {
            var accessError = ValidateAdminOrContributor<PostAdminDetailsDto>();
            if (accessError != null) return accessError;

            var userId = WebContext!.UserId!;
            var categoryId = postDto.CategoryId!.Value;

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

            var uniquenessError = await ValidatePostUniquenessAsync<PostAdminDetailsDto>(
                _postRepository, cleanTitle, cleanSlug, ct);

            if (uniquenessError != null) return uniquenessError;

            var categoryExists = await _categoryService.ExistsAsync(categoryId, ct);

            if (!categoryExists)
            {
                Log.Warning(Posts.CategoryNotFound, categoryId);

                return NotFound<PostAdminDetailsDto>
                    (CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
            }

            var imageResult = await _fileStorageService.SavePostPreviewAsync(postDto.PreviewImage!, cleanSlug);

            if (!imageResult.IsSuccess)
            {
                return Invalid<PostAdminDetailsDto>(imageResult.Message!, imageResult.ErrorCode);
            }

            string previewImageUrl = imageResult.Value!;

            var postEntity = postDto.ToEntity(sanitizedContent, userId, previewImageUrl);

            await _postRepository.AddAsync(postEntity, ct);
            await _postRepository.SaveChangesAsync(ct);

            Log.Information(Posts.Created, postEntity.Title, postEntity.Id);

            var createdPost = await _postRepository.GetByIdWithAuthorsAsync(postEntity.Id, ct);

            if (createdPost == null)
            {
                return NotFound<PostAdminDetailsDto>(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            var responseDto = createdPost.MapToAdminDto();

            return Success(responseDto, PostM.Success.PostAddedSuccessfully);
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default)
        {
            var accessError = ValidateAdminOrContributor<PostAdminDetailsDto>();
            if (accessError != null) return accessError;

            var userId = WebContext!.UserId;
            bool isAdmin = WebContext!.IsAdmin;
            var categoryId = postDto.CategoryId!.Value;

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

            var postEntity = await _postRepository.GetByIdWithAuthorsAsync(postId, ct);

            if (postEntity == null)
            {
                Log.Warning(Posts.NotFound, postId);

                return NotFound<PostAdminDetailsDto>(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            if (!isAdmin && postEntity.AuthorId != userId)
            {
                Log.Warning(Security.UnauthorizedPostAccessAttempt, userId, postId, postEntity.AuthorId, WebContext.IpAddress);
                return Forbidden<PostAdminDetailsDto>();
            }

            var cleanTitle = postDto.Title.StripHtml();
            var cleanSlug = postDto.Slug.StripHtml();
            
            var uniquenessError = await ValidatePostUniquenessAsync<PostAdminDetailsDto>(
                _postRepository, cleanTitle, cleanSlug, ct, postId);

            if (uniquenessError != null) return uniquenessError;

            if (postEntity.CategoryId != categoryId)
            {
                var categoryExists = await _categoryService.ExistsAsync(categoryId, ct);
                if (!categoryExists)
                {
                    Log.Warning(Posts.CategoryNotFound, categoryId);

                    return NotFound<PostAdminDetailsDto>
                        (CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
                }
            }

            string? oldPreviewUrl = null;
            string? newPreviewUrl = null;

            if (postDto.PreviewImage is not null)
            {
                var imageResult = await _fileStorageService.SavePostPreviewAsync(postDto.PreviewImage, cleanSlug);

                if (!imageResult.IsSuccess)
                {
                    return Invalid<PostAdminDetailsDto>(imageResult.Message!, imageResult.ErrorCode);
                }

                oldPreviewUrl = postEntity.PreviewImageUrl;
                newPreviewUrl = imageResult.Value!;
            }

            postDto.UpdateEntity(postEntity, sanitizedContent, newPreviewUrl);
            await _postRepository.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(oldPreviewUrl))
            {
                var deleteResult = await _fileStorageService.DeleteByUrlAsync(oldPreviewUrl);

                if (!deleteResult.IsSuccess)
                {
                    Log.Warning(Posts.OldPreviewNotDeleted, postEntity.Id, oldPreviewUrl);
                }
            }

            Log.Information(Posts.Updated, postEntity.Title, postEntity.Id);

            var responseDto = postEntity.MapToAdminDto();

            return Success(responseDto, PostM.Success.PostUpdatedSuccessfully);
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>        
        public async Task<Result> DeletePostAsync(int postId, CancellationToken ct = default)
        {
            var accessError = ValidateAdminOrContributor();
            if (accessError != null) return accessError;

            var userId = WebContext!.UserId;
            bool isAdmin = WebContext!.IsAdmin;

            var existingPost = await _postRepository.GetByIdAsync(postId, ct);

            if (existingPost == null)
            {
                return NotFound(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            if (!isAdmin && existingPost.AuthorId != userId)
            {
                Log.Warning(Security.UnauthorizedPostAccessAttempt, userId, postId, existingPost.AuthorId, WebContext.IpAddress);
                return Forbidden();
            }

            var imageUrl = existingPost.PreviewImageUrl;

            await _postRepository.DeleteAsync(existingPost, ct);
            await _postRepository.SaveChangesAsync(ct);

            Log.Information(Posts.Deleted, postId);

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var deleteImageResult = await _fileStorageService.DeleteByUrlAsync(imageUrl);

                if (!deleteImageResult.IsSuccess)
                {
                    Log.Warning(Posts.PreviewNotDeletedAfterPostDelete, postId, imageUrl);
                }
            }

            return Success(PostM.Success.PostDeletedSuccessfully);
        }
    }
}