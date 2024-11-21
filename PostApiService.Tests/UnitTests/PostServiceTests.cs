using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private InMemoryDatabaseFixture _fixture;

        public PostServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnTrueWithPostId_IfPostAdded()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var newPost = GetPost();

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(newPost.PostId, result.PostId);
            Assert.Empty(newPost.Comments.ToList()); // comments list
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowArgumentNullException_IfPostIsNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => postService.AddPostAsync(null));
            Assert.Equal("Post cannot be null. (Parameter 'post')", exception.Message);
        }

        [Fact]
        public async Task EditPostAsync_ShouldReturnTrueWithPostId_IfPostEdited()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var newPost = GetPost();
            context.Posts.Add(newPost);
            await context.SaveChangesAsync();

            newPost.Title = "Edited title";
            newPost.Content = "Edited content";

            // Act
            var result = await postService.EditPostAsync(newPost);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.PostId);
            var editedPost = await context.Posts.FirstOrDefaultAsync(p => p.PostId == newPost.PostId);
            Assert.Equal(newPost.Title, editedPost.Title);
            Assert.Equal(newPost.Content, editedPost.Content);
        }

        [Fact]
        public async Task EditCommentAsync_ShouldReturnFalseWithZero_IfPostDoesNotEdit()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var nonExistentPost = new Post
            {
                PostId = 999,
                Title = "Non existent title",
                Content = "Non existent content",
                Description = "Non existent description",
                Slug = "non-existent-slug",
                ImageUrl = "non_existent_image",
                MetaTitle = "Non existent title",
                MetaDescription = "Non existent description"
            };

            // Act
            var result = await postService.EditPostAsync(nonExistentPost);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.PostId);
        }

        [Fact]
        public async Task EditPostAsync_ShouldThrowArgumentNullException_IfPostIsNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => postService.EditPostAsync(null));
            Assert.Equal("Post cannot be null. (Parameter 'post')", exception.Message);
        }

        [Fact]
        public async Task EditPostAsync_ShouldReturnFalseAndZero_WhenDbUpdateExceptionOccurs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var service = new PostService(context, logger);

            var post = GetPost();
            context.Add(post);
            await context.SaveChangesAsync();
            context.Database.EnsureDeleted();

            // Act
            var result = await service.EditPostAsync(post);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.PostId);
        }

        [Fact]
        public async Task EditPostAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var service = new PostService(context, logger);

            var post = GetPost();
            context.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => service.EditPostAsync(post));
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnTrue_IfPostDeleted()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var newPost = GetPost();

            context.Posts.Add(newPost);
            await context.SaveChangesAsync();

            // Act
            var result = await postService.DeletePostAsync(newPost.PostId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<CommentService>();
            var postService = new CommentService(context, logger);

            var nonExistentComment = 999;
            // Act
            var result = await postService.DeleteCommentAsync(nonExistentComment);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteCommentAsync_ShouldThrowArgumentException_IfPostIdIsInvalid(int postId)
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => postService.DeletePostAsync(postId));

            Assert.Equal("Invalid post ID. (Parameter 'postId')", exception.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPostsWithComments()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var pageNumber = 1;
            var pageSize = 10;
            var commentPageNumber = 1;
            var commentsPerPage = 10;
            var includeComments = true;

            for (int i = 0; i < 5; i++)
            {
                var post = GetPost();
                for (int j = 0; j < 3; j++)
                {
                    post.Comments.Add(new Comment
                    {
                        Content = $"Comment {j} for Post {i}",
                        Author = $"Author {j} for Post {i}",
                        CreatedAt = DateTime.UtcNow.AddDays(-j)
                    });
                }
                context.Posts.Add(post);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await postService.GetAllPostsAsync(pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments);

            // Assert
            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            Assert.All(result, post =>
            {
                Assert.NotNull(post.Comments);
                Assert.True(post.Comments.Count <= commentsPerPage);
            });
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPostsWithoutComments()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var pageNumber = 1;
            var pageSize = 10;
            var commentPageNumber = 1;
            var commentsPerPage = 10;
            var includeComments = false;

            for (int i = 0; i < 5; i++)
            {
                var post = GetPost();
                context.Posts.Add(post);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await postService.GetAllPostsAsync(pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            Assert.All(result, post =>
            {
                Assert.NotNull(post.Comments);
                Assert.Empty(post.Comments);
            });
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnEmptyPostList()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var pageNumber = 1;
            var pageSize = 10;
            var commentPageNumber = 1;
            var commentsPerPage = 10;
            var includeComments = false;

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Act
            var result = await postService.GetAllPostsAsync(pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnTenPostsAndTenComments()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var pageNumber = 1;
            var pageSize = 10;
            var commentPageNumber = 1;
            var commentsPerPage = 10;
            var includeComments = true;

            for (int i = 0; i < 12; i++)
            {
                var post = GetPost();
                for (int j = 0; j < 12; j++)
                {
                    post.Comments.Add(new Comment
                    {
                        Content = $"Comment {j} for Post {i}",
                        Author = $"Author {j} for Post {i}",
                        CreatedAt = DateTime.UtcNow.AddDays(-j)
                    });
                }
                context.Posts.Add(post);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await postService.GetAllPostsAsync(pageNumber,
                pageSize,
                commentPageNumber,
                commentsPerPage,
                includeComments);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Count);
            Assert.All(result, post =>
            {
                Assert.Equal(10, post.Comments.Count);
            });
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPostByIdWithComments()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<PostService>();
            var postService = new PostService(context, logger);

            var postId = 1;
            var newPost = GetPost();

            context.Posts.Add(newPost);

            for (int i = 0; i < 3; i++)
            {
                newPost.Comments.Add(new Comment { Content = $"Test comment {i}", Author = "Comment author" });
            }
            await context.SaveChangesAsync();

            // Act
            var post = await postService.GetPostByIdAsync(postId);

            // Assert
            Assert.NotNull(post);
            Assert.Equal(postId, post.PostId);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
            Assert.Equal(3, post.Comments.Count);
        }

        private Post GetPost()
        {
            return new Post
            {
                Title = "Test title",
                Content = "Test content",
                Description = "Test desc",
                Author = "Test author",
                Slug = "test-slug",
                ImageUrl = "testIamge.jpg",
                MetaTitle = "Test meta title",
                MetaDescription = "Test meta description"
            };
        }
    }
}