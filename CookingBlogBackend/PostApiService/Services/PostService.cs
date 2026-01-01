using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Constants;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Data;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly IRepository<Post> _repository;
        private readonly ICategoryService _categoryService;
        private readonly ISnippetGeneratorService _snippetGenerator;

        public PostService(IRepository<Post> repository,
            ICategoryService categoryService,
            ISnippetGeneratorService snippetGenerator)
        {
            _repository = repository;
            _categoryService = categoryService;
            _snippetGenerator = snippetGenerator;
        }

        /// <summary>
        /// Retrieves a paginated list of posts, including the **aggregated comment count** for each post,
        /// and the total count of all posts in the database.
        /// </summary>
        public async Task<Result<PagedResult<PostListDto>>> GetPostsWithTotalPostCountAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            var totalPostCount = await _repository.GetTotalCountAsync(ct);

            var posts = await _repository.AsQueryable()
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(PostMappingExtensions.ToDtoExpression)
                .ToListAsync(ct);

            var pagedData = new PagedResult<PostListDto>(posts, totalPostCount, pageNumber, pageSize);

            return Result<PagedResult<PostListDto>>.Success(pagedData);
        }

        /// <summary>
        /// Searches for posts based on a query string matching Title, Description, or Content.
        /// Results are sorted by creation date (descending) and returned with pagination.
        /// </summary>
        public async Task<Result<PagedSearchResult<SearchPostListDto>>> SearchPostsWithTotalCountAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var queryable = _repository.GetFilteredQueryable(p =>
                p.Title.Contains(query) ||
                p.Description.Contains(query) ||
                p.Content.Contains(query)
            );

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
                    CategoryName = p.Category.Name
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
                   item.CategoryName ?? ContentConstants.DefaultCategory
                   );

            }).ToList();

            return Result<PagedSearchResult<SearchPostListDto>>.Success(new PagedSearchResult<SearchPostListDto>
                (query, searchPostList, searchTotalPosts, pageNumber, pageSize, message));
        }

        /// <summary>
        /// Retrieves detailed information for a specific post by its identifier for administrative use.
        /// </summary>       
        public async Task<Result<PostAdminDetailsDto>> GetPostByIdAsync(int postId, CancellationToken ct = default)
        {
            var postDto = await _repository.AsQueryable()
            .Where(p => p.Id == postId)
            .Select(PostMappingExtensions.ToAdminDetailsDto)
            .FirstOrDefaultAsync();

            if (postDto == null)
            {
                Log.Warning(Posts.NotFound, postId);
                return Result<PostAdminDetailsDto>.NotFound
                    (PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            return Result<PostAdminDetailsDto>.Success(postDto);
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> AddPostAsync(PostCreateDto postDto, CancellationToken ct = default)
        {
            var alreadyExists = await _repository
               .AnyAsync(p => p.Title == postDto.Title || p.Slug == postDto.Slug, ct);

            if (alreadyExists)
            {
                return Result<PostAdminDetailsDto>.Conflict
                    (string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist, postDto.Title, postDto.Slug), PostM.Errors.PostAlreadyExistCode);
            }

            var categoryExists = await _categoryService.ExistsAsync(postDto.CategoryId, ct);

            if (!categoryExists)
            {
                Log.Warning(Posts.CategoryNotFound, postDto.CategoryId);

                return Result<PostAdminDetailsDto>.NotFound
                    (string.Format(CategoryM.Errors.CategoryNotFound), PostM.Errors.CategoryNotFoundCode);
            }

            var postEntity = postDto.ToEntity();

            await _repository.AddAsync(postEntity, ct);
            await _repository.SaveChangesAsync(ct);

            Log.Information(Posts.Created, postEntity.Title, postEntity.Id);

            var responseDto = postEntity.MapToAdminDto();
            var successMessage = string.Format(PostM.Success.PostAddedSuccessfully);

            return Result<PostAdminDetailsDto>.Success(responseDto, successMessage);
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task<Result<PostAdminDetailsDto>> UpdatePostAsync
            (int postId, PostUpdateDto postDto, CancellationToken ct = default)
        {
            var postEntity = await _repository
                .GetByIdAsync(postId, ct);

            if (postEntity == null)
            {
                Log.Warning(Posts.NotFound, postId);

                return Result<PostAdminDetailsDto>.NotFound(string.Format
                    (PostM.Errors.PostNotFound, postDto.Title), PostM.Errors.PostNotFoundCode);
            }

            var alreadyExists = await _repository
                .AnyAsync(p => p.Title == postDto.Title && p.Id != postId, ct);

            if (alreadyExists)
            {
                Log.Information(Posts.AlreadyExists, postDto.Title, postDto.Slug);

                return Result<PostAdminDetailsDto>.Conflict(string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist,
                    postDto.Title, postDto.Slug), PostM.Errors.PostAlreadyExistCode);
            }

            postDto.UpdateEntity(postEntity);

            await _repository.UpdateAsync(postEntity, ct);
            await _repository.SaveChangesAsync(ct);

            Log.Information(Posts.Updated, postEntity.Title, postEntity.Id);

            var responseDto = postEntity.ToDto();
            var successMessage = string.Format(PostM.Success.PostUpdatedSuccessfully);

            return Result<PostAdminDetailsDto>.Success
                (responseDto, successMessage);
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>        
        public async Task<Result<bool>> DeletePostAsync(int postId, CancellationToken ct = default)
        {
            var existingPost = await _repository.GetByIdAsync(postId, ct);

            if (existingPost == null)
            {
                return Result<bool>.NotFound(PostM.Errors.PostNotFound, PostM.Errors.PostNotFoundCode);
            }

            await _repository.DeleteAsync(existingPost, ct);
            await _repository.SaveChangesAsync(ct);

            Log.Information(Posts.Deleted, postId);

            return Result<bool>.Success(true, PostM.Success.PostDeletedSuccessfully);
        }
    }
}
