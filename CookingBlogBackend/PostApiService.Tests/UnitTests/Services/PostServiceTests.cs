using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PostApiService.Exceptions;
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

            Assert.All(result, post =>
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
            });
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPaginatedPosts_WithoutComments()
        {
            // Arrange
            var postService = CreatePostService();

            var totalPosts = 25;
            var pageNumber = 2;
            var pageSize = 10;

            var posts = TestDataHelper.GetPostsWithComments
                (count: totalPosts, generateComments: false);

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
        public async Task GetPostByIdAsync_ShouldThrowPostNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var postService = CreatePostService();
            var post = TestDataHelper.GetSinglePost();

            using var context = _fixture.CreateContext();
            await context.AddAsync(post);
            await context.SaveChangesAsync();

            var invalidPostId = 999;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PostNotFoundException>(() =>
            postService.GetPostByIdAsync(invalidPostId));
            Assert.NotNull(exception);
            Assert.Equal(string.Format
                (ErrorMessages.PostNotFound, invalidPostId), exception.Message);
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

            // Act
            var post = await postService.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
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

            // Act
            var post = await postService.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowPostAlreadyExistException_IfPostExists()
        {
            // Arrange
            var postService = CreatePostService();
            var post = TestDataHelper.GetSinglePost();

            using var context = _fixture.CreateContext();
            await context.AddAsync(post);
            await context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PostAlreadyExistException>(() =>
            postService.AddPostAsync(post));

            Assert.NotNull(exception);
            Assert.Equal(string.Format
                (ErrorMessages.PostAlreadyExist, post.Title), exception.Message);
        }

        [Fact]
        public async Task AddPostAsync_ShouldThrowPostNotSavedException_IfPostNotSaved()
        {
            // Arrange
            var saveChangesResult = 0;
            var newPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(c => c.Posts)
                .ReturnsDbSet(TestDataHelper.GetEmptyPostList());

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangesResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PostNotSavedException>(() =>
            _postService.AddPostAsync(newPost));

            Assert.Equal(string.Format
                (ErrorMessages.PostNotSaved, newPost.Title), exception.Message);

            _mockContext.Verify(c => c.Posts.AddAsync(It.Is<Post>(p => p.Title == newPost.Title &&
            p.Content == newPost.Content), It.IsAny<CancellationToken>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange
            var saveChangesResult = 1;
            var newPost = TestDataHelper.GetSinglePost();

            _mockContext.Setup(c => c.Posts)
                .ReturnsDbSet(TestDataHelper.GetEmptyPostList());

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangesResult);

            // Act
            var result = await _postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);

            _mockContext.Verify(c => c.Posts.AddAsync(It.Is<Post>(p => p.Title == newPost.Title &&
            p.Content == newPost.Content), It.IsAny<CancellationToken>()), Times.Once);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrowKeyNotFoundException_IdPostDoesNotExist()
        {
            // Arrange           
            var existingPost = TestDataHelper.GetSinglePost();
            int noChangesSaved = 0;

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync((Post)null);

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _postService.UpdatePostAsync(existingPost));

            Assert.Equal($"Post with ID {existingPost.PostId} not found. Please check the Post ID.", exception.Message);

            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Post with ID {existingPost.PostId} does not exist.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
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
            await _postService.UpdatePostAsync(existingPost);

            // Assert            
            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No changes were made to post")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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

            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Database error occurred while updating post.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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

            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Unexpected error occurred while updating post with ID {existingPost.PostId}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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

            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Post with ID {postId} does not exist. Deletion aborted.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePost_WhenSaveChangesSucceeds()
        {
            // Arrange
            var postId = 1;
            var post = TestDataHelper.GetSinglePost();
            var saveChangedResult = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(post);

            _mockContext.Setup(m => m.Posts.Remove(It.IsAny<Post>()));

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var postService = new PostService(_mockContext.Object, _mockLoggerService.Object);

            // Act
            await postService.DeletePostAsync(postId);

            // Assert            
            _mockContext.Verify(m => m.Posts.FindAsync(postId), Times.Once);
            _mockContext.Verify(m => m.Posts.Remove(It.Is<Post>(p => p.PostId == postId)), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task DeletePostAsync_ShouldThrowInvalidOperationException_IfNoChangesWereMade()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 0;
            var post = TestDataHelper.GetSinglePost();
            _mockContext.Setup(p => p.Posts.FindAsync(post.PostId)).ReturnsAsync(post);

            _mockContext.Setup(m => m.Posts.Remove(It.IsAny<Post>()));

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _postService.DeletePostAsync(post.PostId));

            Assert.Equal($"Failed to delete post with ID {post.PostId}. No changes were made.", exception.Message);

            _mockContext.Verify(m => m.Posts.FindAsync(postId), Times.Once);
            _mockContext.Verify(m => m.Posts.Remove(It.Is<Post>(p => p.PostId == postId)), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()
                .Contains($"No rows were affected when attempting to delete post with ID {post.PostId}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowDbUpdateException_WhenDatabaseUpdateFails()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.Posts.Remove(It.IsAny<Post>()));

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException($"Database delete failed for post with ID {postId}."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _postService.DeletePostAsync(postId));

            Assert.Equal($"Database delete failed for post with ID {postId}.", exception.Message);

            _mockContext.Verify(m => m.Posts.FindAsync(postId), Times.Once);
            _mockContext.Verify(m => m.Posts.Remove(It.Is<Post>(p => p.PostId == postId)), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()
                .Contains($"Database error occurred while deleting post with ID {postId}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var postId = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(postId))
                .ReturnsAsync(TestDataHelper.GetSinglePost());

            _mockContext.Setup(m => m.Posts.Remove(It.IsAny<Post>()));

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception($"Unexpected error occurred while deleting post with ID {postId}."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _postService.DeletePostAsync(postId));

            Assert.Equal($"Unexpected error occurred while deleting post with ID {postId}.", exception.Message);

            _mockContext.Verify(m => m.Posts.FindAsync(postId), Times.Once);
            _mockContext.Verify(m => m.Posts.Remove(It.Is<Post>(p => p.PostId == postId)), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockLoggerService.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()
                .Contains($"An unexpected error occurred while deleting post with ID {postId}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}