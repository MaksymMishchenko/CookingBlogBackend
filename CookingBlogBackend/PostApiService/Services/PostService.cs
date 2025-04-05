using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Repositories;
using System.Data;
using System.Data.Common;

namespace PostApiService.Services
{
    public class PostService : IPostService
    {
        private readonly IRepository<Post> _repository;

        public PostService(IRepository<Post> repository)
        {
            _repository = repository;
        }

        private void ProcessComments(List<Post> posts, bool includeComments, int commentPageNumber, int commentsPerPage)
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

        /// <summary>
        /// Retrieves a paginated list of posts from the database with optional comments pagination.
        /// </summary>
        public async Task<List<Post>> GetAllPostsAsync(int pageNumber,
            int pageSize,
            int commentPageNumber = 1,
            int commentsPerPage = 10,
            bool includeComments = true,
            CancellationToken cancellationToken = default)
        {
            var query = _repository.AsQueryable();

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
        }

        /// <summary>
        /// Retrieves a post by its ID from the database, with optional inclusion of comments.
        /// </summary>        
        public async Task<Post> GetPostByIdAsync(int postId, bool includeComments = true)
        {
            var query = _repository.AsQueryable();

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
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<Post> AddPostAsync(Post post, CancellationToken cancel = default)
        {
            var existingPost = await _repository                
                .AnyAsync(p => p.Title == post.Title);

            if (existingPost)
            {
                throw new PostAlreadyExistException(post.Title);
            }

            try
            {
                Post addedPost = await _repository.AddAsync(post, cancel);
                return addedPost;
            }
            catch (DbException ex)
            {
                throw new AddPostFailedException(post.Title, ex);
            }
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task UpdatePostAsync(Post post)
        {
            var existingPost = await _repository
                .GetByIdAsync(post.PostId);

            if (existingPost == null)
            {
                throw new PostNotFoundException(post.PostId);
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

                await _repository.UpdateAsync(existingPost);
            }
            catch (DbException ex)
            {
                throw new UpdatePostFailedException(post.Title, ex);
            }
        }

        /// <summary>
        /// Deletes a post from the database by the specified post ID.
        /// </summary>        
        public async Task DeletePostAsync(int postId)
        {
            var existingPost = await _repository.GetByIdAsync(postId);

            if (existingPost == null)
            {
                throw new PostNotFoundException(postId);
            }

            try
            {
                await _repository.DeleteAsync(existingPost);
            }
            catch (DbException ex)
            {
                throw new DeletePostFailedException(postId, ex);
            }
        }       
    }
}
