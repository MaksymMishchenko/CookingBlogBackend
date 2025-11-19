using MockQueryable;
using NSubstitute;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IRepository<Post> _mockRepository;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IRepository<Post>>();
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ShouldReturnPagedPosts()
        {
            // Arrange
            const int expectedTotalCount = 25;
            int pageNumber = 1;
            int pageSize = 10;


            var testPosts = TestDataHelper.GetPostsWithComments(count: expectedTotalCount, commentCount: 10);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            _mockRepository.GetTotalCountAsync()
                .Returns(Task.FromResult(expectedTotalCount));

            var service = new PostService(_mockRepository);

            // Act
            var result = await service.GetPostsWithTotalAsync(pageNumber, pageSize);

            // Assert
            Assert.Equal(pageSize, result.Posts.Count);
            Assert.Equal(expectedTotalCount, result.TotalCount);
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ShouldReturnPaginatedPosts_WithoutComments()
        {
            // Arrange
            const int expectedTotalCount = 23;
            var pageNumber = 2;
            var pageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments(count: expectedTotalCount, generateComments: false);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync().Returns(Task.FromResult(expectedTotalCount));

            var service = new PostService(_mockRepository);

            // Act
            var result = await service.GetPostsWithTotalAsync(pageNumber, pageSize, includeComments: false);

            // Assert
            Assert.Equal(pageSize, result.Posts.Count);
            Assert.Equal(expectedTotalCount, result.TotalCount);
            Assert.All(result.Posts, post =>
            {
                Assert.NotNull(post);
                Assert.Empty(post.Comments);
            });
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, commentCount: 3, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(_mockRepository);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithoutComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, generateComments: false, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(_mockRepository);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange           
            var newPost = TestDataHelper.GetSinglePost();

            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            _mockRepository.AddAsync(newPost, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(newPost));

            var postService = new PostService(_mockRepository);

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);

            await _mockRepository.Received(1)
                .AnyAsync(Arg.Is<Expression<Func<Post, bool>>>(p =>
                    p.Compile()(newPost)), Arg.Any<CancellationToken>());

            await _mockRepository.Received(1)
                .AddAsync(newPost, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange            
            var originalPost = TestDataHelper.GetSinglePost();
            int postId = originalPost.Id;

            var inputPostData = new Post
            {
                Id = postId,
                Title = "New Title",
                Description = originalPost.Description,
                Content = originalPost.Content,
                Author = originalPost.Author,
                ImageUrl = originalPost.ImageUrl,
                MetaTitle = originalPost.MetaTitle,
                MetaDescription = originalPost.MetaDescription,
                Slug = "new-slug-value"
            };

            _mockRepository.GetByIdAsync(postId)
                .Returns(Task.FromResult(originalPost));

            _mockRepository.UpdateAsync(Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            var service = new PostService(_mockRepository);

            // Act
            var resultPost = await service.UpdatePostAsync(postId, inputPostData);

            // Assert

            Assert.NotNull(resultPost);
            Assert.Equal(inputPostData.Title, resultPost.Title);
            Assert.Equal(inputPostData.Slug, resultPost.Slug);

            await _mockRepository.Received(1)
                .GetByIdAsync(originalPost.Id);

            await _mockRepository.Received(1)
                .UpdateAsync(Arg.Is<Post>(p =>
                    p.Title == "New Title" &&
            p.Slug == "new-slug-value" && 
            p.Content == originalPost.Content));
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePost_WhenSaveChangesSucceeds()
        {
            // Arrange            
            var post = TestDataHelper.GetSinglePost();

            var mockRepository = Substitute.For<IRepository<Post>>();

            mockRepository.GetByIdAsync(post.Id)
                .Returns(Task.FromResult(post));

            mockRepository.DeleteAsync(Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            var service = new PostService(mockRepository);

            // Act
            await service.DeletePostAsync(post.Id);

            // Assert
            await mockRepository.Received(1)
                .GetByIdAsync(post.Id);

            await mockRepository.Received(1)
                .DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == post.Id));
        }
    }
}