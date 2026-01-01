using PostApiService.Controllers;
using PostApiService.Interfaces;
using PostApiService.Models.Dto;
using PostApiService.Models.Dto.Requests;
using System.Net;
using static PostApiService.Tests.Helper.HttpHelper.Urls;

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
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyList_WhenNoPostsAvailableYet()
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
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PostListDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(PostM.Success.NoPostsAvailableYet, response.Message);

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

            var categories = TestDataHelper.GetCulinaryCategories();
            var mockPosts = TestDataHelper.GetPostListDtos(MockTotalPostsCount, categories);

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
                (PostM.Success.PostsRetrievedSuccessfully, response.DataList.Count), response.Message);

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
            var categories = TestDataHelper.GetCulinaryCategories();

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                    Posts: TestDataHelper.GetPostListDtos(1, categories),
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
        public async Task SearchPosts_ShouldReturnOk_WhenPostsExist()
        {
            // Arrange
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 3;
            const int MockSearchedTotalPosts = 3;

            var queryParameters = new SearchPostQueryParameters();
            queryParameters.PageNumber = ExpectedPageNumber;
            queryParameters.PageSize = ExpectedPageSize;

            queryParameters.QueryString = "Chili";

            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetSearchedPostListDtos(categories);
            const int TotalPostCount = 3;

            _mockPostService.SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((
                    SearchPostList: posts,
                    SearchTotalPosts: TotalPostCount
                )));

            // Act
            var result = await _postsController.SearchPostsWithTotalCountAsync(queryParameters);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<SearchPostListDto>>(okResult.Value);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.NotNull(response.DataList);
            Assert.Equal(string.Format
                (PostM.Success.PostsRetrievedSuccessfully, response.DataList.Count), response.Message);

            Assert.Equal(ExpectedPageNumber, response.PageNumber);
            Assert.Equal(ExpectedPageSize, response.PageSize);
            Assert.Equal(MockSearchedTotalPosts, response.TotalCount);
            Assert.Equal("Chili", response.SearchQuery);

            await _mockPostService.Received(1)
                .SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SearchPosts_ShouldReturnOkWithEmptyList_WhenNoPostsMatch()
        {
            // Arrange
            var query = new SearchPostQueryParameters();
            query.QueryString = "NonExistent";
            query.PageNumber = 1;
            query.PageSize = 10;

            var emptySearchPostList = TestDataHelper.GetEmptySearchPostListDtos();

            _mockPostService.SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
                ).Returns(Task.FromResult((
                    SearchPostList: emptySearchPostList,
                    SearchTotalPosts: 0
                )));

            // Act
            var result = await _postsController.SearchPostsWithTotalCountAsync(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<SearchPostListDto>>(okResult.Value);
            Assert.Empty(response.DataList);
            Assert.Equal(0, response.TotalCount);

            await _mockPostService.Received(1)
                .SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldPassCancellationTokenToService()
        {
            // Arrange
            var queryParameters = new SearchPostQueryParameters { QueryString = "Chili" };

            using var cts = new CancellationTokenSource();
            var expectedToken = cts.Token;

            _mockPostService.SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                expectedToken)
                .Returns(Task.FromResult((
                    searchPostList: new List<SearchPostListDto>(),
                    searchTotalPosts: 0)));

            // Act            
            await _postsController.SearchPostsWithTotalCountAsync(queryParameters, expectedToken);

            // Assert           
            await _mockPostService.Received(1).SearchPostsWithTotalCountAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                expectedToken);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var expectedPost = TestDataHelper.GetSinglePost(categories);
            _mockPostService.GetPostByIdAsync(expectedPost.Id, true, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedPost));

            // Act
            var result = await _postsController.GetPostByIdAsync
                (expectedPost.Id, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostM.Success.PostRetrievedSuccessfully, expectedPost.Id), response.Message);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            await _mockPostService.Received(1)
                .GetPostByIdAsync(expectedPost.Id, true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostIsAddedSuccessfully()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            _mockPostService.AddPostAsync(post, token)
                .Returns(Task.FromResult(post));

            // Act
            var result = await _postsController.AddPostAsync(post, token);

            // Assert            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(createdAtActionResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(string.Format
                (PostM.Success.PostAddedSuccessfully, post.Id), response.Message);

            await _mockPostService.Received(1).AddPostAsync(post, token);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnExpectedResult_WhenPostIsUpdated()
        {
            // Arrange            
            var categories = TestDataHelper.GetCulinaryCategories();
            var originalPost = TestDataHelper.GetSinglePost(categories);
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

            _mockPostService.UpdatePostAsync(postId, inputPostData, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(inputPostData));

            // Act
            var result = await _postsController.UpdatePostAsync(postId, inputPostData);

            // Assert            
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var actualResponse = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(actualResponse.Success);
            Assert.Equal(string.Format
                (PostM.Success.PostUpdatedSuccessfully, postId), actualResponse.Message);
            Assert.Equal(postId, actualResponse.Data.Id);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);

            await _mockPostService.Received(1).UpdatePostAsync
                (postId, inputPostData, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnOk_IfPostRemovedSuccessfully()
        {
            // Arrange
            var postId = 1;
            _mockPostService.DeletePostAsync(postId, Arg.Any<CancellationToken>())
                 .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);
            Assert.Equal(string.Format
                (PostM.Success.PostDeletedSuccessfully, postId), response.Message);

            await _mockPostService.Received(1).DeletePostAsync(postId, Arg.Any<CancellationToken>());
        }
    }
}