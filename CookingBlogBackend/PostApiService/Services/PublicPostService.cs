using PostApiService.Helper;
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
    public class PublicPostService : BaseService, IPublicPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ICategoryService _catService;
        private readonly ISnippetGeneratorService _snippetGenerator;

        public PublicPostService(IPostRepository postRepository,
            ICategoryService catService,            
            ISnippetGeneratorService snippetGenerator)
        {
            _postRepository = postRepository;
            _catService = catService;
            _snippetGenerator = snippetGenerator;
        }

        private async Task<Result<PagedSearchResult<SearchPostListDto>>> GetPostsWithSnippetsAsync(
            IQueryable<Post> queryable,
            AppliedFilters appliedFilters,
            string? query,
            int pageNumber,
            int pageSize,
            CancellationToken ct)
        {
            var totalCount = await queryable.CountAsync(ct);

            var categoryPart = !string.IsNullOrEmpty(appliedFilters.CategoryName)
                ? string.Format(PostM.Success.CategoryPart, appliedFilters.CategoryName)
                : string.Empty;

            var template = totalCount == 0
                ? PostM.Success.SearchNoResults
                : PostM.Success.SearchResultsFound;

            var message = string.Format(template, totalCount, appliedFilters.SearchTerm, categoryPart);

            var filtersDto = new AppliedFiltersDto(appliedFilters.SearchTerm, appliedFilters.CategoryName);

            if (totalCount == 0)
            {
                return Success(new PagedSearchResult<SearchPostListDto>(
                    new List<SearchPostListDto>(), filtersDto, totalCount, pageNumber, pageSize, message));
            }

            var postsFromDb = await queryable
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Content,
                    p.Description,
                    p.Author,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug
                })
                .ToListAsync(ct);

            var searchPostList = postsFromDb.Select(item =>
            {
                bool hasQuery = !string.IsNullOrWhiteSpace(query);

                string? snippet = hasQuery
                    ? _snippetGenerator.CreateSnippet(item.Content, query!, 100)
                    : null;

                string? description = !hasQuery
                    ? (item.Description?.Length > 100 ? item.Description[..100] + "..." : item.Description)
                    : null;

                return new SearchPostListDto(
                    item.Id,
                    item.Title,
                    item.Slug,
                    snippet,
                    description,
                    item.Author,
                    item.CategoryName ?? ContentConstants.DefaultCategory,
                    item.CategorySlug ?? ContentConstants.DefaultSlugCategory
                );
            }).ToList();

            return Success(new PagedSearchResult<SearchPostListDto>(
                searchPostList, filtersDto, totalCount, pageNumber, pageSize, message));
        }

        /// <summary>
        /// Retrieves a paginated list of active posts. 
        /// Depending on the parameters, returns either standard PostListDto (for home page) 
        /// or SearchPostListDto with snippets/descriptions (for search page).
        /// </summary>
        // TODO: Refactor to unify PostListDto and SearchPostListDto to avoid 'object' return type and branching logic.
        // See: https://github.com/MaksymMishchenko/CookingBlogBackend/issues/49
        public async Task<Result<object>> GetPostsPagedAsync(PublicPostQueryDto postQuery, CancellationToken ct = default)
        {
            string? categoryName = null;

            if (!string.IsNullOrWhiteSpace(postQuery.CategorySlug))
            {
                categoryName = await _catService.GetNameBySlugAsync(postQuery.CategorySlug, ct);

                if (categoryName == null)
                {
                    return NotFound<object>(CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode);
                }
            }

            var appliedFilters = new AppliedFilters(
                SearchTerm: postQuery.SearchTerm,
                CategoryName: categoryName
            );

            var query = _postRepository.GetPublicFilteredPosts(postQuery.SearchTerm, onlyActive: true, postQuery.CategorySlug);

            if (!string.IsNullOrWhiteSpace(postQuery.SearchTerm) || postQuery.IsSearchMode)
            {
                var searchResult = await GetPostsWithSnippetsAsync(
                    query,
                    appliedFilters,
                    postQuery.SearchTerm,
                    postQuery.PageNumber,
                    postQuery.PageSize,
                    ct);

                return Success<object>(searchResult.Value!);
            }

            var result = await GetPagedDataAsync(
                query,
                appliedFilters,
                postQuery.PageNumber,
                postQuery.PageSize,
                PostMappingExtensions.ToDtoExpression,
                ct);

            return Success<object>(result);
        }

        /// <summary>
        /// Retrieves the details of a specific active post based on its slug and category slug.
        /// </summary>       
        public async Task<Result<PostDetailsDto>> GetPostBySlugAsync(PostRequestBySlug dto, CancellationToken ct = default)
        {
            var cleanSlug = dto.Slug.StripHtml().Trim().ToLowerInvariant();
            var cleanCategory = dto.Category.StripHtml().Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(cleanSlug) || string.IsNullOrWhiteSpace(cleanCategory))
            {
                return Invalid<PostDetailsDto>(PostM.Errors.SlugAndCategoryRequired,
                    PostM.Errors.SlugAndCategoryRequiredCode);
            }

            var postDto = await _postRepository
                .GetPublicFilteredPosts(null, onlyActive: true, categorySlug: cleanCategory)
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
    }
}