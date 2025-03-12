using Microsoft.EntityFrameworkCore;
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
        private readonly PostService _postService;

        public PostServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _mockContext = new Mock<IApplicationDbContext>();
            _postService = new PostService(_mockContext.Object);
        }

        private IPostService CreatePostService()
        {
            var context = _fixture.CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return new PostService(context);
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
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange
            var existingPost = TestDataHelper.GetSinglePost();
            int saveChangesResult = 1;

            _mockContext.Setup(p => p.Posts.FindAsync(existingPost.PostId))
                .ReturnsAsync(existingPost);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangesResult);

            existingPost.Title = "Updated title";
            existingPost.Description = "Updated description";

            // Act
            await _postService.UpdatePostAsync(existingPost);

            // Assert            
            _mockContext.Verify(s => s.Posts.FindAsync(It.IsAny<object[]>()), Times.Once);

            _mockContext.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePost_WhenSaveChangesSucceeds()
        {
            // Arrange            
            var post = TestDataHelper.GetSinglePost();
            var saveChangedResult = 1;

            _mockContext.Setup(m => m.Posts.FindAsync(post.PostId))
                .ReturnsAsync(post);

            _mockContext.Setup(m => m.Posts.Remove(post));

            _mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            // Act
            await _postService.DeletePostAsync(post.PostId);

            // Assert            
            _mockContext.Verify(m => m.Posts.FindAsync(post.PostId), Times.Once);

            _mockContext.Verify(m => m.Posts.Remove
            (It.Is<Post>(p => p.PostId == post.PostId)), Times.Once);

            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}