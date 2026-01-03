using PostApiService.Exceptions;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Constants;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Data;
using System.Data.Common;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly IRepository<Post> _repository;
        private readonly ISnippetGeneratorService _snippetGenerator;

        public PostService(IRepository<Post> repository,
            ISnippetGeneratorService snippetGenerator)
        {
            _repository = repository;
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
        /// Retrieves a post by its ID from the database, with optional inclusion of comments.
        /// </summary>        
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true, CancellationToken ct = default)
        {
            var query = _repository.AsQueryable();

            query = query.Include(p => p.Category);

            if (includeComments)
            {
                query = query
                    .Include(p => p.Comments);
            }

            var post = await query
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post == null)
            {
                throw new PostNotFoundException(postId);
            }

            return post;
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Post> AddPostAsync(Post post, CancellationToken ct = default)
        {
            var existingPost = await _repository
                .AnyAsync(p => p.Title == post.Title, ct);

            if (existingPost)
            {
                throw new PostAlreadyExistException(post.Title);
            }

            try
            {
                await _repository.AddAsync(post, ct);
                await _repository.SaveChangesAsync(ct);

                return post;
            }
            catch (DbException ex)
            {
                throw new AddPostFailedException(post.Title, ex);
            }
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task<Post> UpdatePostAsync(int postId, Post post, CancellationToken ct = default)
        {
            var existingPost = await _repository
                .GetByIdAsync(postId, ct);

            if (existingPost == null)
            {
                throw new PostNotFoundException(postId);
            }

            try
            {
                existingPost.Title = post.Title;
                existingPost.Description = post.Description;
                existingPost.Content = post.Content;
                existingPost.ImageUrl = post.ImageUrl;
                existingPost.MetaTitle = post.MetaTitle;
                existingPost.MetaDescription = post.MetaDescription;
                existingPost.Slug = post.Slug;

                await _repository.UpdateAsync(existingPost, ct);
                await _repository.SaveChangesAsync(ct);

                return existingPost;
            }
            catch (DbException ex)
            {
                throw new UpdatePostFailedException(post.Title, ex);
            }
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>        
        public async Task DeletePostAsync(int postId, CancellationToken ct = default)
        {
            var existingPost = await _repository.GetByIdAsync(postId, ct);

            if (existingPost == null)
            {
                throw new PostNotFoundException(postId);
            }

            try
            {
                await _repository.DeleteAsync(existingPost, ct);
                await _repository.SaveChangesAsync(ct);
            }
            catch (DbException ex)
            {
                throw new DeletePostFailedException(postId, ex);
            }
        }
    }
}
