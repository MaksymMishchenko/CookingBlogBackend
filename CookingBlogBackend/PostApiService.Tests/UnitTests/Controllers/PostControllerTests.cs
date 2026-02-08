using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class PostControllerTests
    {
        private readonly IPostService _mockPostService;
        private readonly ICommentService _mockCommentService;
        private readonly PostsController _postsController;

        public PostControllerTests()
        {
            _mockPostService = Substitute.For<IPostService>();
            _mockCommentService = Substitute.For<ICommentService>();
            _postsController = new PostsController(_mockPostService, _mockCommentService);
        }

        [Fact]
        public async Task GetPostsAsync_InNormalMode_ShouldReturnOkWithPagedResult()
        {
            // Arrange            
            var queryParams = new PostQueryParameters { PageNumber = 1, PageSize = 10 };
            var mockData = new PagedResult<PostListDto>(new List<PostListDto>(), 0, 1, 10);

            _mockPostService.GetPostsPagedAsync(null, null, 1, 10, Arg.Any<CancellationToken>())
                .Returns(Result<object>.Success(mockData));

            // Act
            var result = await _postsController.GetPostsAsync(queryParams);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(0, response.TotalCount);
            Assert.Equal(1, response.PageNumber);

            Assert.IsAssignableFrom<IEnumerable<PostListDto>>(response.Data);
            Assert.Empty(response.Data);

            await _mockPostService.Received(1).GetPostsPagedAsync(null, null, 1, 10, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsAsync_InSearchMode_ShouldReturnOkWithPagedSearchResult()
        {
            // Arrange
            const string SearchTerm = "pizza";
            const string ExpectedMessage = "Results found";
            var queryParams = new PostQueryParameters { Search = SearchTerm, PageNumber = 1, PageSize = 5 };
            var mockSearchData = new PagedSearchResult<SearchPostListDto>(
                SearchTerm, new List<SearchPostListDto>(), 0, 1, 5, "Results found");

            _mockPostService.GetPostsPagedAsync(SearchTerm, null, 1, 5, Arg.Any<CancellationToken>())
                .Returns(Result<object>.Success(mockSearchData));

            // Act
            var result = await _postsController.GetPostsAsync(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);

            Assert.Equal(SearchTerm, response.SearchQuery);
            Assert.Equal(ExpectedMessage, response.Message);
            Assert.Equal(0, response.TotalCount);
            Assert.Equal(1, response.PageNumber);

            Assert.IsAssignableFrom<IEnumerable<SearchPostListDto>>(response.Data);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task GetPostsAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound()
        {
            // Arrange
            var queryParams = new PostQueryParameters { CategorySlug = "invalid-cat" };
            _mockPostService.GetPostsPagedAsync(null, "invalid-cat", Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result<object>.NotFound(CategoryM.Errors.CategoryNotFound, PostM.Errors.CategoryNotFoundCode));

            // Act
            var result = await _postsController.GetPostsAsync(queryParams);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, response.ErrorCode);
        }

        [Fact]
        public async Task GetCommentsByPostIdAsync_ShouldReturnOk_WithPagedResult()
        {
            // Arrange
            int postId = 1;
            var queryParams = new PaginationQueryParameters
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedPagedResult = new PagedResult<CommentDto>(
                new List<CommentDto>(), 10, queryParams.PageNumber, queryParams.PageSize);

            var serviceResult = Result<PagedResult<CommentDto>>.Success(expectedPagedResult);

            _mockCommentService.GetCommentsByPostIdAsync(
                    postId,
                    queryParams.PageNumber,
                    queryParams.PageSize,
                    Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var actionResult = await _postsController.GetCommentsByPostIdAsync(postId, queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var returnValue = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.Equal(expectedPagedResult.TotalCount, returnValue.TotalCount);

            await _mockCommentService.Received(1).GetCommentsByPostIdAsync(
                postId, queryParams.PageNumber, queryParams.PageSize, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCommentsByPostIdAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound()
        {
            // Arrange
            int postId = 99;
            var queryParams = new PaginationQueryParameters { PageNumber = 1, PageSize = 10 };

            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            var serviceResult = Result<PagedResult<CommentDto>>.NotFound(errorMessage, errorCode);

            _mockCommentService.GetCommentsByPostIdAsync(
                    postId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var actionResult = await _postsController.GetCommentsByPostIdAsync(postId, queryParams);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            var returnValue = Assert.IsType<ApiResponse>(notFoundResult.Value);

            // Assert
            Assert.False(returnValue.Success);
            Assert.Equal(returnValue.Message, errorMessage);
            Assert.Equal(returnValue.ErrorCode, errorCode);
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnBadRequest400_WhenInputsAreEmpty()
        {
            // Arrange
            const string InvalidCategorySlug = " ";
            const string InvalidPostSlug = " ";

            var requestDto = TestDataHelper.CreatePostRequest(InvalidCategorySlug, InvalidPostSlug);

            var expectedErrorMessage = PostM.Errors.SlugAndCategoryRequired;
            var expectedErrorCode = PostM.Errors.SlugAndCategoryRequiredCode;

            var serviceResult = Result<PostDetailsDto>.Invalid(
                expectedErrorMessage, expectedErrorCode);

            _mockPostService.GetPostBySlugAsync(Arg.Any<PostRequestBySlug>(),
                Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetActivePostBySlugAsync(requestDto);

            // Assert            
            var invalidResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(invalidResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedErrorMessage, response.Message);
            Assert.Equal(expectedErrorCode, response.ErrorCode);

            await _mockPostService.Received(1).GetPostBySlugAsync(Arg.Any<PostRequestBySlug>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ReturnsNotFound_WhenSlugOrCategoryMismatch()
        {
            // Arrange
            const string WrongCategorySlug = "wrong-category";
            const string ValidPostSlug = "valid-slug";

            var requestDto = TestDataHelper.CreatePostRequest(WrongCategorySlug, ValidPostSlug);

            var expectedErrorMessage = PostM.Errors.PostNotFoundByPath;
            var expectedErrorCode = PostM.Errors.PostNotFoundByPathCode;

            var serviceResult = Result<PostDetailsDto>.NotFound(
                expectedErrorMessage, expectedErrorCode);

            _mockPostService.GetPostBySlugAsync(Arg.Any<PostRequestBySlug>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetActivePostBySlugAsync(requestDto);

            // Assert            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedErrorMessage, response.Message);
            Assert.Equal(expectedErrorCode, response.ErrorCode);

            await _mockPostService.Received(1).GetPostBySlugAsync
                (Arg.Any<PostRequestBySlug>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ReturnsOk200_WhenPostExists()
        {
            // Arrange
            const string ValidCategorySlug = "desserts";
            const string ValidSlug = "chocolate-cake";
            var ct = CancellationToken.None;

            var requestDto = TestDataHelper.CreatePostRequest(ValidCategorySlug, ValidSlug);

            var expectedPostDto = TestDataHelper.GetPostDetailsDto
                (slug: ValidSlug, categorySlug: ValidCategorySlug);
            var serviceResult = Result<PostDetailsDto>.Success(expectedPostDto);

            _mockPostService.GetPostBySlugAsync(requestDto, ct)
                .Returns(serviceResult);

            // Act             
            var result = await _postsController.GetActivePostBySlugAsync(requestDto, ct);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);

            var response = Assert.IsType<ApiResponse<PostDetailsDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(ValidSlug, response.Data.Slug);
            Assert.Equal(ValidCategorySlug, response.Data.CategorySlug);
            Assert.Equal(expectedPostDto.Title, response.Data.Title);

            await _mockPostService.Received(1).GetPostBySlugAsync
               (Arg.Any<PostRequestBySlug>(), ct);
        }
    }
}