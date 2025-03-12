using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Data;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly IApplicationDbContext _context;

        public PostService(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of posts from the database with optional comments pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (starting from 1).</param>
        /// <param name="pageSize">The number of posts to retrieve per page.</param>
        /// <param name="commentPageNumber">The page number to retrieve comments for each post (starting from 1).</param>
        /// <param name="commentsPerPage">The number of comments to retrieve per page for each post.</param>
        /// <param name="includeComments">Indicates whether to include comments in the response.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of posts.</returns>        
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Posts.AsNoTracking();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            var posts = await query
                .OrderBy(p => p.PostId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            ProcessComments(posts, includeComments, commentPageNumber, commentsPerPage);

            return posts;

            void ProcessComments(List<Post> posts, bool includeComments, int commentPageNumber, int commentsPerPage)
            {
                if (includeComments)
                {
                    foreach (var post in posts)
                    {
                        post.Comments = post.Comments
                            .OrderBy(c => c.CreatedAt)
                            .Skip((commentPageNumber - 1) * commentsPerPage)
                            .Take(commentsPerPage)
                            .ToList();
                    }
                }
                else
                {
                    foreach (var post in posts)
                    {
                        post.Comments = new List<Comment>();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a post by its ID from the database, with optional inclusion of comments.
        /// </summary>
        /// <param name="postId">The ID of the post to retrieve.</param>
        /// <param name="includeComments">A boolean flag indicating whether to include comments for the post. Default is <c>true</c>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the post if found.</returns>
        /// <exception cref="PostNotFoundException">Thrown when a post with the specified ID is not found in the database.</exception>
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            var query = _context.Posts.AsNoTracking();

            if (includeComments)
            {
                query = query.Include(p => p.Comments);
            }

            var post = await query.FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                throw new PostNotFoundException(postId);
            }

            return post;
        }

        /// <summary>
        /// Adds a new post to the database. If a post with the same title already exists, an exception is thrown.
        /// If the post is successfully added, the method returns the created post object.
        /// </summary>
        /// <param name="post">The post object to be added to the database.</param>
        /// <returns>The added post object, including its generated properties such as PostId.</returns>
        /// <exception cref="PostAlreadyExistException">Thrown if a post with the same title already exists in the database.</exception>
        /// <exception cref="PostNotSavedException">Thrown if the post could not be saved to the database.</exception>
        public async Task<Post> AddPostAsync(Post post)
        {
            var existingPost = await _context.Posts
                .AsNoTracking()
                .AnyAsync(p => p.Title == post.Title);

            if (existingPost)
            {
                throw new PostAlreadyExistException(post.Title);
            }

            await _context.Posts.AddAsync(post);
            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new AddPostFailedException(post.Title);
            }

            return post;
        }

        /// <summary>
        /// Updates an existing post with the provided data.
        /// Throws a <see cref="PostNotFoundException"/> if the post with the specified ID does not exist.
        /// Throws an <see cref="UpdatePostFailedException"/> if the changes could not be saved to the database.
        /// </summary>
        /// <param name="post">The post object containing the updated data to be saved.</param>
        /// <exception cref="PostNotFoundException">Thrown when no post with the specified ID is found in the database.</exception>
        /// <exception cref="UpdatePostFailedException">Thrown when the update operation fails to save changes.</exception>
        public async Task UpdatePostAsync(Post post)
        {
            var existingPost = await _context.Posts
                .FindAsync(post.PostId);

            if (existingPost == null)
            {
                throw new PostNotFoundException(post.PostId);
            }

            existingPost.Title = post.Title;
            existingPost.Description = post.Description;
            existingPost.Content = post.Content;
            existingPost.ImageUrl = post.ImageUrl;
            existingPost.MetaTitle = post.MetaTitle;
            existingPost.MetaDescription = post.MetaDescription;
            existingPost.Slug = post.Slug;

            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new UpdatePostFailedException(post.Title);
            }
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>
        /// <param name="postId">The ID of the post to delete.</param>
        /// <exception cref="PostNotFoundException">
        /// Thrown when the post with the specified ID is not found.
        /// </exception>
        /// <exception cref="DeletePostFailedException">
        /// Thrown when the post deletion fails.
        /// </exception>
        /// <returns>An asynchronous task with no return value.</returns>
        public async Task DeletePostAsync(int postId)
        {
            var existingPost = await _context.Posts.FindAsync(postId);

            if (existingPost == null)
            {
                throw new PostNotFoundException(postId);
            }

            _context.Posts.Remove(existingPost);

            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                throw new DeletePostFailedException(postId);
            }
        }
    }
}
