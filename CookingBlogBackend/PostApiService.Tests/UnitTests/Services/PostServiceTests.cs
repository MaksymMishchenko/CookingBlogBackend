using MockQueryable;
using NSubstitute;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPagedPosts()
        {
            // Arrange                                   
            int pageNumber = 1;
            int pageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments(count: 25, commentCount: 10);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var result = await service.GetAllPostsAsync(pageNumber, pageSize);

            // Assert
            Assert.Equal(pageSize, result.Count);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnPaginatedPosts_WithoutComments()
        {
            // Arrange                        
            var pageNumber = 2;
            var pageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments(count: 25, commentCount: 10);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            var mockRepository = Substitute.For<IRepository<Post>>();
            mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(mockRepository);

            // Act
            var result = await service.GetAllPostsAsync(pageNumber, pageSize, includeComments: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pageSize, result.Count);

            var actualPostIds = result.Select(p => p.PostId).ToList();

            Assert.All(result, post =>
            {
                Assert.NotNull(post);
                Assert.Empty(post.Comments);
            });
        }
    }
}