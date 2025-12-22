using Microsoft.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Constants;
using PostApiService.Models.Dto;
using PostApiService.Repositories;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

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
        public async Task<(List<PostListDto> Posts, int TotalPostCount)> GetPostsWithTotalPostCountAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var totalPostCount = await _repository.GetTotalCountAsync(cancellationToken);

            var query = _repository.AsQueryable();

            var posts = await query
                .OrderBy(p => p.Id)
                .Select(p => new PostListDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Author = p.Author,
                    Category = p.Category.Name ?? ContentConstants.DefaultCategory,
                    CreatedAt = p.CreatedAt,
                    Description = p.Description,
                    CommentsCount = p.Comments.Count()
                })
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (posts, totalPostCount);
        }

        /// <summary>
        /// Searches for posts based on a query string matching Title, Description, or Content.
        /// Results are sorted by creation date (descending) and returned with pagination.
        /// </summary>
        public async Task<(List<SearchPostListDto> SearchPostList, int SearchTotalPosts)> SearchPostsWithTotalCountAsync
            (string query, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            Expression<Func<Post, bool>> searchPredicate = p =>
                p.Title.Contains(query) ||
                p.Description.Contains(query) ||
                p.Content.Contains(query);

            var queryable = _repository.GetFilteredQueryable(searchPredicate);

            var searchTotalPosts = await queryable.CountAsync(cancellationToken);

            if (searchTotalPosts == 0)
            {
                return (new List<SearchPostListDto>(), 0);
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
                    p.Category
                })
                .ToListAsync(cancellationToken);

            var searchPostList = postsWithContent.Select(item =>
            {
                var snippet = _snippetGenerator.CreateSnippet(item.Content, query, 100);

                return new SearchPostListDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Slug = item.Slug,
                    Author = item.Author,
                    SearchSnippet = snippet,
                    Category = item.Category.Name ?? ContentConstants.DefaultCategory,
                };
            }).ToList();

            return (searchPostList, searchTotalPosts);
        }

        /// <summary>
        /// Retrieves a post by its ID from the database.
        /// </summary>        
        public async Task<PostAdminDetailsDto> GetPostByIdAsync(int postId)
        {
            var postDto = await _repository.AsQueryable()
        .Where(p => p.Id == postId)
        .Select(p => new PostAdminDetailsDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Content = p.Content,
            Author = p.Author,
            ImageUrl = p.ImageUrl,
            Slug = p.Slug,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            CategoryId = p.CategoryId,
            CreatedAt = p.CreatedAt
        })
        .FirstOrDefaultAsync()
        ?? throw new PostNotFoundException(postId);

            return postDto;
        }

        /// <summary>
        /// Adds a new post to the database.
        /// </summary>        
        public async Task<PostAdminDetailsDto> AddPostAsync(Post post)
        {
            var alreadyExists = await _repository
                .AnyAsync(p => p.Title == post.Title || p.Slug == post.Slug);

            if (alreadyExists)
            {
                string fullMessage = string.Format(PostErrorMessages.PostAlreadyExist, post.Title, post.Slug);
                throw new PostAlreadyExistException
                    (fullMessage, post.Title, post.Slug);
            }

            try
            {
                Post addedPost = await _repository.AddAsync(post);

                return MapToAdminDto(addedPost);
            }
            catch (DbException ex)
            {
                throw new AddPostFailedException(post.Title, ex);
            }
        }

        /// <summary>
        /// Updates an existing post with the provided data.        
        /// </summary>        
        public async Task<PostAdminDetailsDto> UpdatePostAsync(int postId, Post post)
        {
            var existingPost = await _repository
                .GetByIdAsync(postId);

            if (existingPost == null)
            {
                throw new PostNotFoundException(postId);
            }

            if (existingPost.Title != post.Title || existingPost.Slug != post.Slug)
            {
                var alreadyExists = await _repository.AnyAsync(p =>
                    p.Id != postId && (p.Title == post.Title || p.Slug == post.Slug));

                if (alreadyExists)
                {
                    string fullMessage = string.Format(PostErrorMessages.PostAlreadyExist, post.Title, post.Slug);
                    throw new PostAlreadyExistException(fullMessage, post.Title, post.Slug);
                }
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
                existingPost.CategoryId = post.CategoryId;

                await _repository.UpdateAsync(existingPost);

                return MapToAdminDto(existingPost);
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

        private PostAdminDetailsDto MapToAdminDto(Post p) => new PostAdminDetailsDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Content = p.Content,
            Author = p.Author,
            ImageUrl = p.ImageUrl,
            Slug = p.Slug,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            CategoryId = p.CategoryId,
            CreatedAt = p.CreatedAt
        };
    }
}
