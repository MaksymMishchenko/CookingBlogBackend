using PostApiService.Helper;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Constants;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Data;

namespace PostApiService.Services
{
    // TODO (TechDebt): #30 Transition to a dedicated mapping service (e.g., AutoMapper or Mapperly).
    // Current implementation relies on static PostMappingExtensions, which is becoming hard to maintain 
    // as the number of DTO variations increases.
    public class PostService : BaseService, IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IHtmlSanitizationService _sanitizer;
        private readonly ICategoryService _categoryService;
        private readonly ISnippetGeneratorService _snippetGenerator;

        public PostService(IPostRepository postRepository,
            IWebContext webContext,
            IHtmlSanitizationService sanitizer,
            ICategoryService categoryService,
            ISnippetGeneratorService snippetGenerator) : base(webContext)
        {
            _postRepository = postRepository;
            _sanitizer = sanitizer;
            _categoryService = categoryService;
            _snippetGenerator = snippetGenerator;
        }

        /// <summary>
        /// Retrieves a paginated list of ACTIVE posts, including the aggregated comment count for each post,
        /// and the total count of active posts for correct pagination.
        /// </summary>
        public async Task<Result<PagedResult<PostListDto>>> GetActivePostsPagedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            var query = _postRepository.GetActive();

            var totalCount = await query.CountAsync(ct);

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(PostMappingExtensions.ToDtoExpression)
                .ToListAsync(ct);

            var pagedData = new PagedResult<PostListDto>(posts, totalCount, pageNumber, pageSize);

            return Result<PagedResult<PostListDto>>.Success(pagedData);
        }

        /// <summary>
        /// Searches for ACTIVE posts based on a query string matching Title, Description, or Content.
        /// Results are sorted by creation date (descending) and returned with pagination.
        /// </summary>
        public async Task<Result<PagedSearchResult<SearchPostListDto>>> SearchActivePostsPagedAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var queryable = _postRepository.GetActive();

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(p =>
                    p.Title.Contains(query) ||
                    p.Description.Contains(query) ||
                    p.Content.Contains(query)
                );
            }

            var searchTotalPosts = await queryable.CountAsync(ct);

            var message = searchTotalPosts == 0
                ? string.Format(PostM.Success.SearchNoResults, query)
                : string.Format(PostM.Success.SearchResultsFound, query, searchTotalPosts);

            if (searchTotalPosts == 0)
            {
                return Result<PagedSearchResult<SearchPostListDto>>.Success(new PagedSearchResult<SearchPostListDto>
                    (query, new List<SearchPostListDto>(), searchTotalPosts, pageNumber, pageSize, message));
            }

            var postsWithContent = await queryable
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Content,
                    p.Author,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug,
                })
                .ToListAsync(ct);

            var searchPostList = postsWithContent.Select(item =>
            {
                var snippet = _snippetGenerator.CreateSnippet(item.Content, query, 100);

                return new SearchPostListDto(
                   item.Id,
                   item.Title,
                   item.Slug,
                   snippet,
                   item.Author,
                   item.CategoryName ?? ContentConstants.DefaultCategory,
                   item.CategorySlug ?? ContentConstants.DefaultSlugCategory
                   );

            }).ToList();

            return Result<PagedSearchResult<SearchPostListDto>>.Success(new PagedSearchResult<SearchPostListDto>
                (query, searchPostList, searchTotalPosts, pageNumber, pageSize, message));
        }

        /// <summary>
        /// Retrieves a paginated list of active posts belonging to a specific category identified by its slug.
        /// </summary>
        public async Task<Result<PagedResult<PostListDto>>> GetActivePostsByCategoryPagedAsync
            (string slug, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var categoryExists = await _categoryService.ExistsBySlugAsync(slug, ct);

            if (!categoryExists)
            {
                return NotFound<PagedResult<PostListDto>>
                    (CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
            }

            var query = _postRepository.GetActive()
                .Where(p => p.Category.Slug == slug);

            var totalPostCount = await query.CountAsync(ct);

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(PostMappingExtensions.ToDtoExpression)
                .ToListAsync(ct);

            var pagedData = new PagedResult<PostListDto>(posts, totalPostCount, pageNumber, pageSize);

            return Result<PagedResult<PostListDto>>.Success(pagedData);
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
        /// Retrieves the details of a specific active post based on its slug and category slug.
        /// </summary>       
        public async Task<Result<PostDetailsDto>> GetActivePostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default)
        {
            var cleanSlug = dto.Slug.StripHtml().Trim().ToLowerInvariant();
            var cleanCategory = dto.Category.StripHtml().Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(cleanSlug) || string.IsNullOrWhiteSpace(cleanCategory))
            {
                return Invalid<PostDetailsDto>(PostM.Errors.SlugAndCategoryRequired,
                    PostM.Errors.SlugAndCategoryRequiredCode);
            }

            var postDto = await _postRepository.GetActive()
                .Where(p => p.Slug == cleanSlug && p.Category.Slug == cleanCategory)
                .ToDetailsDtoExpression()
                .FirstOrDefaultAsync(ct);

            if (postDto == null)
            {
                Log.Warning(Posts.NotFoundByPath, cleanSlug, cleanCategory);

                return NotFound<PostDetailsDto>
                    (PostM.Errors.PostNotFoundByPath, PostM.Errors.PostNotFoundByPathCode);
            }

            return Success(postDto);
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default)
        {
            var userId = WebContext.UserId;

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
            var userId = WebContext.UserId;

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
            var userId = WebContext.UserId;

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
