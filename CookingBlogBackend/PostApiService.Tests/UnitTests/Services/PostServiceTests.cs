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
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange
            var newPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(c => c.Posts)
                .ReturnsDbSet(TestDataHelper.GetEmptyPostList());

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var postService = new PostService(_mockContext.Object, _mockLoggerService.Object);

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);

            _mockContext.Verify(c => c.Posts.AddAsync(It.Is<Post>(p => p.Title == newPost.Title && p.Content == newPost.Content), It.IsAny<CancellationToken>()), Times.Once);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Post was added successfully.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnNull_WhenSaveChangesFails()
        {
            // Arrange
            var newPost = new Post
            {
                Title = "Unique Title",
                Content = "Sample content"
            };

            _mockContext.Setup(c => c.Posts)
                .ReturnsDbSet(new List<Post>());

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var postService = new PostService(_mockContext.Object, _mockLoggerService.Object);

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.Null(result);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to add post")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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
            Assert.Equal("An unexpected error occurred while adding post to database.", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowKeyNotFoundException_IdPostDoesNotExist()
        {
            // Arrange           
            var existingPost = TestDataHelper.GetSinglePost();
            int noChangesSaved = 0;

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync((Post)null);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(noChangesSaved);

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _postService.UpdatePostAsync(existingPost));

            Assert.Equal($"Post with ID {existingPost.PostId} not found. Please check the Post ID.", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnTrue_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();
            int changesSavedSuccessfully = 1;

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(changesSavedSuccessfully);

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act
            var result = await _postService.UpdatePostAsync(existingPost);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowInvalidOperationException_IdPostDoesNotUpdate()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();
            int noChangesSaved = 0;

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(noChangesSaved);

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _postService.UpdatePostAsync(existingPost));

            Assert.Equal($"No changes were made to post with ID {existingPost.PostId}.", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowDbUpdateConcurrencyException_WhenDbSaveFailsDueToConcurrency()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("Simulated concurrency issue"));

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _postService
            .UpdatePostAsync(existingPost));

            Assert.Equal("Simulated concurrency issue", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowDbUpdateException_WhenDatabaseUpdateFails()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Simulated DbUpdateException"));

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _postService
            .UpdatePostAsync(existingPost));

            Assert.Equal("Simulated DbUpdateException", exception.Message);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowAnException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulated UnexpectedExceptionOccurs"));

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<Exception>(() => _postService
            .UpdatePostAsync(existingPost));

            Assert.Equal("Simulated UnexpectedExceptionOccurs", exception.Message);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowKeyNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 1;
            _mockContext.Setup(p => p.Posts.FindAsync(postId)).ReturnsAsync((Post)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _postService.DeletePostAsync(postId));

            Assert.Equal($"Post with ID {postId} does not exist.", exception.Message);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnTrue_WhenSaveChangesSucceeds()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowInvalidOperationException_IfNoChangesWereMade()
        {
            // Arrange            
            var post = TestDataHelper.GetSinglePost();
            _mockContext.Setup(p => p.Posts.FindAsync(post.PostId)).ReturnsAsync(post);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _postService.DeletePostAsync(post.PostId));

            Assert.Equal($"Failed to delete post with ID {post.PostId}. No changes were made.", exception.Message);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowDbUpdateConcurrencyException_WhenDatabaseUpdateFails()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException($"Database concurrency error occurred while deleting the post with ID {postId}." +
                    " This may be caused by conflicting changes in the database."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _postService.DeletePostAsync(postId));

            Assert.Equal($"Database concurrency error occurred while deleting the post with ID {postId}." +
                         " This may be caused by conflicting changes in the database.", exception.Message);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowDbUpdateException_WhenDatabaseUpdateFails()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException($"Database delete failed for post with ID {postId}."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _postService.DeletePostAsync(postId));

            Assert.Equal($"Database delete failed for post with ID {postId}.", exception.Message);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception($"Unexpected error occurred while deleting post with ID {postId}."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _postService.DeletePostAsync(postId));

            Assert.Equal($"Unexpected error occurred while deleting post with ID {postId}.", exception.Message);
        }
    }
}