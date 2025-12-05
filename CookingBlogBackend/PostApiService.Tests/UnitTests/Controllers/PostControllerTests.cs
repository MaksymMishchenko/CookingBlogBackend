using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto;
using PostApiService.Models.Dto.Requests;
using System.Net;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class PostControllerTests
    {
        private readonly IPostService _mockPostService;
        private readonly PostsController _postsController;

        public PostControllerTests()
        {
            _mockPostService = Substitute.For<IPostService>();
            _postsController = new PostsController(_mockPostService);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnNotFound_WhenPostsAreNotFound()
        {
            // Arrange
            var queryParameters = new PostQueryParameters();
            queryParameters.PageNumber = 2;
            queryParameters.PageSize = 10;

            var posts = TestDataHelper.GetEmptyPostListDtos();
            const int TotalPostCount = 0;

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                    Posts: posts,
                    TotalPostCount
                )));

            // Act
            var result = await _postsController.GetPostsWithTotalPostCountAsync(queryParameters);

            // Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PostListDto>>(notFoundObjectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.NoPostsFound, response.Message);

            await _mockPostService.Received(1)
                .GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnOk_WhenPostsAreFound()
        {
            // Arrange
            const int MockTotalPostsCount = 50;
            const int ExpectedPageNumber = 3;
            const int ExpectedPageSize = 10;

            var queryParameters = new PostQueryParameters();
            queryParameters.PageNumber = ExpectedPageNumber;
            queryParameters.PageSize = ExpectedPageSize;

            var mockPosts = TestDataHelper.GetPostListDtos(MockTotalPostsCount);

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                Posts: mockPosts,
                MockTotalPostsCount
                )));

            // Act
            var result = await _postsController.GetPostsWithTotalPostCountAsync(queryParameters);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PostListDto>>(okResult.Value);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.NotNull(response.DataList);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostsRetrievedSuccessfully, response.DataList.Count), response.Message);

            Assert.Equal(ExpectedPageNumber, response.PageNumber);
            Assert.Equal(ExpectedPageSize, response.PageSize);
            Assert.Equal(MockTotalPostsCount, response.TotalCount);

            await _mockPostService.Received(1)
                .GetPostsWithTotalPostCountAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldPassCancellationTokenToService()
        {
            // Arrange
            var queryParameters = new PostQueryParameters();

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                    Posts: TestDataHelper.GetPostListDtos(1),
                    TotalPostCount: 1)));

            // Act
            var result = await _postsController.GetPostsWithTotalPostCountAsync(queryParameters);

            // Assert           
            await _mockPostService.Received(1)
                .GetPostsWithTotalPostCountAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var expectedPost = TestDataHelper.GetSinglePost();
            _mockPostService.GetPostByIdAsync(expectedPost.Id, true)
                .Returns(expectedPost);

            // Act
            var result = await _postsController.GetPostByIdAsync
                (expectedPost.Id, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostRetrievedSuccessfully, expectedPost.Id), response.Message);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            await _mockPostService.Received(1)
                .GetPostByIdAsync(expectedPost.Id, true);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostIsAddedSuccessfully()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();

            _mockPostService.AddPostAsync(post)
                .Returns(post);

            // Act
            var result = await _postsController.AddPostAsync(post);

            // Assert            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(createdAtActionResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostAddedSuccessfully, post.Id), response.Message);

            await _mockPostService.Received(1).AddPostAsync(post);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnExpectedResult_WhenPostIsUpdated()
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

            _mockPostService.UpdatePostAsync(postId, inputPostData)
                .Returns(Task.FromResult(inputPostData));

            // Act
            var result = await _postsController.UpdatePostAsync(postId, inputPostData);

            // Assert            
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var actualResponse = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(actualResponse.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, postId), actualResponse.Message);
            Assert.Equal(postId, actualResponse.Data.Id);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);

            await _mockPostService.Received(1).UpdatePostAsync(postId, inputPostData);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnOk_IfPostRemovedSuccessfully()
        {
            // Arrange
            var postId = 1;
            _mockPostService.DeletePostAsync(postId)
                 .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostDeletedSuccessfully, postId), response.Message);

            await _mockPostService.Received(1).DeletePostAsync(postId);
        }
    }
}