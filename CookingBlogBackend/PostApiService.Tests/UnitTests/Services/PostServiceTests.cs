using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;
using System.Data;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<PostService>> _mockLoggerService;
        private readonly PostService _postService;

        public PostServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLoggerService = new Mock<ILogger<PostService>>();
            _postService = new PostService(_mockContext.Object, _mockLoggerService.Object);
        }

        private IPostService CreatePostService()
        {
            var context = _fixture.CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return new PostService(context, _mockLoggerService.Object);
        }

        [Fact]
        public async Task GetAllPostsAsync_GetAllPostsAsync_ShouldReturnPaginatedPosts_WithComments()
        {
            // Arrange
            var postService = CreatePostService();

            var posts = TestDataHelper.GetPostsWithComments(count: 25, commentCount: 10);

            using var context = _fixture.CreateContext();
            await context.AddRangeAsync(posts);
            await context.SaveChangesAsync();

            int pageNumber = 2;
            int pageSize = 10;
            int commentPageNumber = 1;
            int commentsPerPage = 3;

            var expectedPostIds = await context.Posts
                .OrderBy(p => p.PostId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.PostId)
                .ToListAsync();

            // Act
            var result = await postService.GetAllPostsAsync(
                pageNumber, pageSize, commentPageNumber, commentsPerPage, includeComments: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pageSize, result.Count);

            var actualPostIds = result.Select(p => p.PostId).ToList();
            Assert.Equal(expectedPostIds, actualPostIds);

            foreach (var post in result)
            {
                Assert.NotNull(post.Comments);
                Assert.Equal(commentsPerPage, post.Comments.Count);

                var expectedCommentIds = posts.First(p => p.PostId == post.PostId)
                    .Comments
                    .OrderBy(c => c.CreatedAt)
                    .Skip((commentPageNumber - 1) * commentsPerPage)
                    .Take(commentsPerPage)
                    .Select(c => c.CommentId)
                    .ToList();

                var actualCommentIds = post.Comments.Select(c => c.CommentId).ToList();
                Assert.Equal(expectedCommentIds, actualCommentIds);
            }
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPaginatedPosts_WithoutComments()
        {
            // Arrange
            var postService = CreatePostService();

            var totalPosts = 25;
            var pageNumber = 2;
            var pageSize = 10;

            var posts = TestDataHelper.GetPostsWithComments(count: totalPosts, generateComments: false);

            using var context = _fixture.CreateContext();
            await context.AddRangeAsync(posts);
            await context.SaveChangesAsync();

            var expectedPostIds = await context.Posts
                .OrderBy(p => p.PostId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.PostId)
                .ToListAsync();

            // Act
            var result = await postService.GetAllPostsAsync(pageNumber, pageSize, includeComments: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pageSize, result.Count);

            var actualPostIds = result.Select(p => p.PostId).ToList();
            Assert.Equal(expectedPostIds, actualPostIds);

            Assert.All(result, post =>
            {
                Assert.NotNull(post);
                Assert.Empty(post.Comments);
            });
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var mockContext = new Mock<IApplicationDbContext>();
            var mockLoggerService = new Mock<ILogger<PostService>>();
            var postService = new PostService(mockContext.Object, mockLoggerService.Object);

            mockContext.Setup(c => c.Posts).ReturnsDbSet(TestDataHelper.GetListWithPost());
            mockContext.Setup(c => c.Comments).ReturnsDbSet(TestDataHelper.GetEmptyCommentList());
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            var pageNumber = 2;
            var pageSize = 10;
            var includeComments = false;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
            postService.GetAllPostsAsync(pageNumber, pageSize, includeComments: false));
            Assert.Equal($"An unexpected error occurred while fetching posts from the database. PageNumber: {pageNumber}, PageSize: {pageSize}, IncludeComments: {includeComments}.", exception.Message);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldThrowKeyNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var postService = CreatePostService();
            var posts = TestDataHelper.GetEmptyPostList();

            using var context = _fixture.CreateContext();
            await context.AddRangeAsync(posts);
            await context.SaveChangesAsync();

            var invalidPostId = 999;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            postService.GetPostByIdAsync(invalidPostId));
            Assert.NotNull(exception);
            Assert.Equal($"Post with ID {invalidPostId} was not found.", exception.Message);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithComments()
        {
            // Arrange
            var postService = CreatePostService();

            var posts = TestDataHelper.GetPostsWithComments(count: 3, commentCount: 3);

            using var context = _fixture.CreateContext();
            await context.AddRangeAsync(posts);
            await context.SaveChangesAsync();

            var postId = 2;
            var commentsCount = 3;

            // Act
            var post = await postService.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.Equal(postId, post.PostId);
            Assert.NotNull(post.Comments);
            Assert.Equal(commentsCount, post.Comments.Count());
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithoutComments()
        {
            // Arrange
            var postService = CreatePostService();

            var posts = TestDataHelper.GetPostsWithComments(count: 3, commentCount: 3);

            using var context = _fixture.CreateContext();
            await context.AddRangeAsync(posts);
            await context.SaveChangesAsync();

            var postId = 2;
            var commentsCount = 0;

            // Act
            var post = await postService.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.Equal(postId, post.PostId);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
            Assert.Equal(commentsCount, post.Comments.Count());
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowDbUpdateException_IfPostHasTheSameTitle()
        {
            // Arrange            
            var post = TestDataHelper.GetPostsWithComments(count: 1, generateComments: false);

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(post);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("A post with this title already exists."));

            var newPost = new Post { Title = post[0].Title };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _postService.AddPostAsync(newPost));
            Assert.Contains("A post with this title already exists.", exception.Message);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public async Task AddPostAsync_ShouldReturnCorrectResult_AccordingToSaveChangesResult(int saveChangedResult, bool expectedResult)
        {
            // Arrange            
            _mockContext.Setup(c => c.Posts).ReturnsDbSet(TestDataHelper.GetPostsWithComments(count: 1, generateComments: false));
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var newPost = new Post { Title = "Unique Title", Content = "Sample content" };

            // Act
            var result = await _postService.AddPostAsync(newPost);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange            
            var post = TestDataHelper.GetPostsWithComments(count: 1, generateComments: false);

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(post);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("An unexpected error occurred while adding post to database.."));

            var newPost = new Post { Title = "Test title" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _postService.AddPostAsync(newPost));
            Assert.Contains("An unexpected error occurred while adding post to database.", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowKeyNotFoundException_IdPostDoesNotExist()
        {
            // Arrange            
            var post = TestDataHelper.GetPostsWithComments(count: 1, generateComments: false);
            _mockContext.Setup(c => c.Posts).ReturnsDbSet(post);

            var updatedPost = new Post { PostId = 999, Content = "Updated content" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _postService.UpdatePostAsync(updatedPost));

            Assert.Equal($"Post with ID {updatedPost.PostId} does not exist.", exception.Message);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public async Task UpdatePostAsync_ShouldReturnCorrectResult_AccordingToSaveChangesResult(int saveChangedResult, bool expectedResult)
        {
            // Arrange            
            _mockContext.Setup(c => c.Posts).ReturnsDbSet(TestDataHelper.GetPostsWithComments(count: 1, generateComments: false));
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var updatedPost = new Post { Content = "Updated content" };

            // Act
            var result = await _postService.UpdatePostAsync(updatedPost);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowDbUpdateConcurrencyException_WhenDbSaveFailsDueToConcurrency()
        {
            _mockContext.Setup(c => c.Posts)
                .ReturnsDbSet(TestDataHelper.GetPostsWithComments(count: 1));

            var updatedPost = new Post { Content = "Updated content" };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException($"Database concurrency error occurred while updating the post with ID {updatedPost.PostId}." +
                    " This may be caused by conflicting changes in the database."));

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _postService
            .UpdatePostAsync(updatedPost));

            Assert.Equal($"Database concurrency error occurred while updating the post with ID {updatedPost.PostId}." +
                    " This may be caused by conflicting changes in the database.", exception.Message);
        }

        //[Fact]
        //public async Task EditPostAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
        //{
        //    // Arrange
        //    using var context = _fixture.CreateContext();
        //    var service = new PostService(context, _logger);

        //    var post = GetPost();
        //    context.Dispose();

        //    // Act & Assert
        //    await Assert.ThrowsAsync<ObjectDisposedException>(() => service.EditPostAsync(post));
        //}

        //[Fact]
        //public async Task DeletePostAsync_ShouldReturnTrue_IfPostDeleted()
        //{
        //    // Arrange
        //    using var context = _fixture.CreateContext();
        //    var postService = new PostService(context, _logger);

        //    var newPost = GetPost();

        //    context.Posts.Add(newPost);
        //    await context.SaveChangesAsync();

        //    // Act
        //    var result = await postService.DeletePostAsync(newPost.PostId);

        //    // Assert
        //    Assert.True(result);
        //}

        //[Fact]
        //public async Task DeletePostAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        //{
        //    // Arrange
        //    var postService = CreatePostService();

        //    var nonExistentComment = 999;
        //    // Act
        //    var result = await postService.DeletePostAsync(nonExistentComment);

        //    // Assert
        //    Assert.False(result);
        //}

        //[Theory]
        //[InlineData(0)]
        //[InlineData(-1)]
        //public async Task DeletePostAsync_ShouldThrowArgumentException_IfPostIdIsInvalid(int postId)
        //{
        //    // Arrange
        //    var postService = CreatePostService();

        //    // Act & Assert
        //    var exception = await Assert.ThrowsAsync<ArgumentException>(() => postService.DeletePostAsync(postId));

        //    Assert.Equal("Invalid post ID. (Parameter 'postId')", exception.Message);
        //}        
    }
}