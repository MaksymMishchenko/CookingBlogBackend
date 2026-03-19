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
            var dto = queryParams.ToDto();
            var mockData = new PagedResult<PostListDto>(new List<PostListDto>(), 0, 1, 10);

            _mockPostService.GetPostsPagedAsync(dto, Arg.Any<CancellationToken>())
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

            await _mockPostService.Received(1).GetPostsPagedAsync(dto, Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData("pizza", "recipes", "Recipes")]
        [InlineData(null, "desserts", "Desserts")]
        [InlineData("salad", null, null)]
        public async Task GetPostsAsync_WithValidFilters_ShouldReturnOkWithFilters(
            string? searchTerm,
            string? categorySlug,
            string? expectedCategoryName)
        {
            // Arrange
            var queryParams = new PostQueryParameters
            {
                Search = searchTerm,
                CategorySlug = categorySlug,
                PageNumber = 1,
                PageSize = 10
            };

            var dto = queryParams.ToDto();

            var appliedFiltersDto = new AppliedFiltersDto(searchTerm, expectedCategoryName);

            object mockData = !string.IsNullOrWhiteSpace(searchTerm)
                ? new PagedSearchResult<SearchPostListDto>(new List<SearchPostListDto>(), appliedFiltersDto, 0, 1, 10, "Found 0 posts")
                : new PagedResult<PostListDto>(new List<PostListDto>(), 0, 1, 10, appliedFiltersDto);

            _mockPostService.GetPostsPagedAsync(dto, Arg.Any<CancellationToken>())
                .Returns(Result<object>.Success(mockData));

            // Act
            var result = await _postsController.GetPostsAsync(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.AppliedFilters);
            Assert.Equal(searchTerm, response.AppliedFilters.Search);
            Assert.Equal(expectedCategoryName, response.AppliedFilters.CategoryName);
        }

        [Theory]
        [InlineData("invalid-slug")]
        [InlineData("non-existent-category")]
        public async Task GetPostsAsync_WithInvalidCategory_ShouldReturnNotFound(string invalidCategorySlug)
        {
            // Arrange
            var queryParams = new PostQueryParameters { CategorySlug = invalidCategorySlug };
            var dto = queryParams.ToDto();

            _mockPostService.GetPostsPagedAsync(dto, Arg.Any<CancellationToken>())
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
        public async Task GetPostsAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound()
        {
            // Arrange
            var queryParams = new PostQueryParameters { CategorySlug = "invalid-cat" };
            var dto = queryParams.ToDto();

            _mockPostService.GetPostsPagedAsync(dto, Arg.Any<CancellationToken>())
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
        public async Task GetCommentsByPostIdAsync_ShouldReturnOk_WithCursorPagedResult()
        {
            // Arrange           
            int postId = 1;
            var queryParams = new CommentCursorParameters
            {
                LastId = null,
                PageSize = 10
            };

            var dtos = new List<CommentDto>
            {
                new CommentDto(
                    Id: 100,
                    Content: "First Comment",
                    Author: "John Doe",
                    CreatedAt: DateTime.UtcNow,
                    UserId: "user-001",
                    ReplyToUserName: null,
                    ParentId: null),
                new CommentDto(
                    Id: 99,
                    Content: "Second Comment",
                    Author: "Jane Smith",
                    CreatedAt: DateTime.UtcNow,
                    UserId: "user-002",
                    ReplyToUserName: null,
                    ParentId: null)
            };

            var scrollResponse = new CommentScrollResponse<CommentDto>(
                Items: dtos,
                LastId: 99,
                HasNextPage: true,
                TotalCount: 25
            );

            var serviceResult = Result<CommentScrollResponse<CommentDto>>.Success(scrollResponse);

            _mockCommentService.GetCommentsByPostIdAsync(
                    postId,
                    queryParams.LastId,
                    queryParams.PageSize,
                    Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var actionResult = await _postsController.GetCommentsByPostIdAsync(postId, queryParams, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);

            var apiResponse = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(apiResponse.Success);

            Assert.Equal(dtos.Count, apiResponse.Data.Count());

            Assert.Equal(99, apiResponse.LastId);
            Assert.Equal(25, apiResponse.TotalCount);
            Assert.True(apiResponse.HasNextPage);

            await _mockCommentService.Received(1).GetCommentsByPostIdAsync(
                postId,
                queryParams.LastId,
                queryParams.PageSize,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCommentsByPostIdAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange            
            int postId = 1;
            var queryParams = new CommentCursorParameters { LastId = null, PageSize = 10 };

            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            var serviceResult = Result<CommentScrollResponse<CommentDto>>.NotFound(errorMessage, errorCode);

            _mockCommentService.GetCommentsByPostIdAsync(
                    postId,
                    Arg.Any<int?>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var actionResult = await _postsController.GetCommentsByPostIdAsync(postId, queryParams, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Equal(errorMessage, apiResponse.Message);
            Assert.Equal(errorCode, apiResponse.ErrorCode);
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