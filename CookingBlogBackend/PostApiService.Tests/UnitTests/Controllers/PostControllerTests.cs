using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
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
        public async Task GetPostsAsync_ShouldReturnEmptyList_WhenNoPostsAvailableYet()
        {
            // Arrange
            const int TotalPostCount = 0;
            var queryParameters = new PostQueryParameters { PageNumber = 2, PageSize = 10 };

            var posts = TestDataHelper.GetEmptyPostListDtos();

            var pagedData = new PagedResult<PostListDto>(
                posts,
                TotalPostCount,
                queryParameters.PageNumber,
                queryParameters.PageSize);

            var pagedResult = Result<PagedResult<PostListDto>>.Success(pagedData);

            _mockPostService.GetPostsPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(pagedResult);

            // Act
            var result = await _postsController.GetPostsAsync(queryParameters);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);
            Assert.True(response.Success);

            await _mockPostService.Received(1)
                .GetPostsPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsAsync_ShouldReturnOk_WhenPostsExist()
        {
            // Arrange
            const int MockTotalPostsCount = 50;
            const int ExpectedPageNumber = 3;
            const int ExpectedPageSize = 10;

            var token = CancellationToken.None;

            var queryParameters = new PostQueryParameters { PageNumber = ExpectedPageNumber, PageSize = ExpectedPageSize };

            var categories = TestDataHelper.GetCulinaryCategories();
            var mockPosts = TestDataHelper.GetPostListDtos(MockTotalPostsCount, categories);

            var pagedData = new PagedResult<PostListDto>(
                mockPosts,
                MockTotalPostsCount,
                queryParameters.PageNumber,
                queryParameters.PageSize);

            var pagedResult = Result<PagedResult<PostListDto>>.Success(pagedData);

            _mockPostService.GetPostsPagedAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                token)
                .Returns(pagedResult);

            // Act
            var result = await _postsController.GetPostsAsync(queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);


            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);

            Assert.Equal(ExpectedPageNumber, response.PageNumber);
            Assert.Equal(ExpectedPageSize, response.PageSize);
            Assert.Equal(MockTotalPostsCount, response.TotalCount);

            await _mockPostService.Received(1)
                .GetPostsPagedAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                token);
        }

        [Fact]
        public async Task GetAdminPostsAsync_ShouldReturnUnauthorized_WhenServiceReturnsUnauthorized()
        {
            // Arrange
            var query = new PostQueryParameters();
            var ct = CancellationToken.None;
           
            var expectedMessage = Auth.LoginM.Errors.UnauthorizedAccess;
            var expectedErrorCode = Auth.LoginM.Errors.UnauthorizedAccessCode;

            var authError = Result<PagedResult<AdminPostListDto>>.Unauthorized(
                expectedMessage,
                expectedErrorCode);

            _mockPostService.GetAdminPostsPagedAsync(
                    Arg.Any<bool?>(),
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    ct)
                .Returns(authError);

            // Act
            var result = await _postsController.GetAdminPostsAsync(query, ct);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);            
            var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);
            Assert.Equal(expectedErrorCode, response.ErrorCode);
            Assert.Equal(expectedMessage, response.Message);
        }

        [Fact]
        public async Task GetAdminPostsAsync_ShouldReturnOk_WithAdminPostListDtos()
        {
            // Arrange
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int TotalCount = 0;
            var ct = CancellationToken.None;

            var query = new PostQueryParameters
            {
                isActive = false,
                PageNumber = ExpectedPageNumber,
                PageSize = ExpectedPageSize
            };

            var pagedData = new PagedResult<AdminPostListDto>(
                new List<AdminPostListDto>(),
                TotalCount,
                ExpectedPageNumber,
                ExpectedPageSize);

            var pagedResult = Result<PagedResult<AdminPostListDto>>.Success(pagedData);

            _mockPostService.GetAdminPostsPagedAsync(query.isActive, ExpectedPageNumber, ExpectedPageSize, ct)
                .Returns(pagedResult);

            // Act
            var result = await _postsController.GetAdminPostsAsync(query, ct);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
           
            await _mockPostService.Received(1).GetAdminPostsPagedAsync(
                query.isActive,
                ExpectedPageNumber,
                ExpectedPageSize,
                ct);
        }

        [Fact]
        public async Task SearchActivePostsAsync_ShouldReturnOk_WhenPostsExist()
        {
            // Arrange
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 3;
            const int TotalCount = 3;
            const string Query = "Chili";
            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, TotalCount);

            var token = CancellationToken.None;

            var queryParameters = new SearchPostQueryParameters
            {
                PageNumber = ExpectedPageNumber,
                PageSize = ExpectedPageSize,
                QueryString = Query
            };

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetSearchedPostListDtos(categories);

            var searchPagedResult = new PagedSearchResult<SearchPostListDto>(
                Query,
                posts,
                TotalCount,
                ExpectedPageNumber,
                ExpectedPageSize,
                expectedMessage);

            var serviceResult = Result<PagedSearchResult<SearchPostListDto>>.Success(searchPagedResult);

            _mockPostService.SearchActivePostsPagedAsync(
                Query,
                ExpectedPageNumber,
                ExpectedPageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.SearchActivePostsAsync(queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Same(posts, response.Data);

            await _mockPostService.Received(1).SearchActivePostsPagedAsync(
                Query,
                ExpectedPageNumber,
                ExpectedPageSize,
                token);
        }

        [Fact]
        public async Task SearchActivePostsAsync_ShouldReturnOkWithEmptyList_WhenNoPostsMatch()
        {
            // Arrange
            const string Query = "NonExistent";
            const int ExpectedTotalCount = 0;
            var queryParameters = new SearchPostQueryParameters
            {
                QueryString = Query,
                PageNumber = 1,
                PageSize = 10
            };

            var token = CancellationToken.None;

            var expectedMessage = string.Format(PostM.Success.SearchNoResults, Query);

            var searchPagedResult = new PagedSearchResult<SearchPostListDto>(
                Query,
                new List<SearchPostListDto>(),
                ExpectedTotalCount,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                expectedMessage);

            var serviceResult = Result<PagedSearchResult<SearchPostListDto>>.Success(searchPagedResult);

            _mockPostService.SearchActivePostsPagedAsync(
                Query,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.SearchActivePostsAsync(queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Same(searchPagedResult.Items, response.Data);

            await _mockPostService.Received(1).SearchActivePostsPagedAsync(
                Query,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token);
        }

        [Fact]
        public async Task GetActivePostsByCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const string InvalidSlug = "non-existent-category";
            var queryParameters = new PostQueryParameters { PageNumber = 1, PageSize = 10 };
            var token = CancellationToken.None;

            var expectedErrorMessage = CategoryM.Errors.CategoryNotFound;
            var expectedErrorCode = PostM.Errors.CategoryNotFoundCode;

            var serviceResult = Result<PagedResult<PostListDto>>.NotFound(
                expectedErrorMessage,
                expectedErrorCode);

            _mockPostService.GetActivePostsByCategoryPagedAsync(
                InvalidSlug,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetActivePostsByCategoryAsync(InvalidSlug, queryParameters, token);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedErrorMessage, response.Message);
            Assert.Equal(expectedErrorCode, response.ErrorCode);

            await _mockPostService.Received(1).GetActivePostsByCategoryPagedAsync(
                InvalidSlug,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token);
        }

        [Fact]
        public async Task GetActivePostsByCategoryAsync_ShouldReturnOk_WhenCategoryExists()
        {
            // Arrange
            const string CategorySlug = "desserts";
            const int ExpectedTotalCount = 5;
            var queryParameters = new PostQueryParameters { PageNumber = 1, PageSize = 10 };
            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var mockPosts = TestDataHelper.GetPostListDtos(ExpectedTotalCount, categories);

            var pagedData = new PagedResult<PostListDto>(
                mockPosts,
                ExpectedTotalCount,
                queryParameters.PageNumber,
                queryParameters.PageSize);

            var serviceResult = Result<PagedResult<PostListDto>>.Success(pagedData);

            _mockPostService.GetActivePostsByCategoryPagedAsync(
                CategorySlug,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetActivePostsByCategoryAsync(CategorySlug, queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(ExpectedTotalCount, response.TotalCount);
            Assert.Same(mockPosts, response.Data);

            await _mockPostService.Received(1).GetActivePostsByCategoryPagedAsync(
                CategorySlug,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var expectedPost = TestDataHelper.GetSinglePost(categories);

            var token = CancellationToken.None;

            var postAdminDetailsDto = TestDataHelper.CreatePostAdminDetailsDto(expectedPost);
            var serviceResult = Result<PostAdminDetailsDto>.Success(postAdminDetailsDto);

            _mockPostService.GetPostByIdAsync(expectedPost.Id, token)
                .Returns(Task.FromResult(serviceResult));

            // Act
            var result = await _postsController.GetPostByIdAsync(expectedPost.Id, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PostAdminDetailsDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            await _mockPostService.Received(1)
                .GetPostByIdAsync(expectedPost.Id, token);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode404_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 999;
            var token = CancellationToken.None;

            var expectedErrorMessage = string.Format(PostM.Errors.PostNotFound, postId);
            var serviceResult = Result<PostAdminDetailsDto>.NotFound(
                expectedErrorMessage,
                PostM.Errors.PostNotFoundCode);

            _mockPostService.GetPostByIdAsync(postId, token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, token);

            // Assert            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedErrorMessage, response.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, response.ErrorCode);

            await _mockPostService.Received(1).GetPostByIdAsync(postId, token);
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

            _mockPostService.GetActivePostBySlugAsync(Arg.Any<PostRequestBySlug>(),
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

            await _mockPostService.Received(1).GetActivePostBySlugAsync(Arg.Any<PostRequestBySlug>(),
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

            _mockPostService.GetActivePostBySlugAsync(Arg.Any<PostRequestBySlug>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.GetActivePostBySlugAsync(requestDto);

            // Assert            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedErrorMessage, response.Message);
            Assert.Equal(expectedErrorCode, response.ErrorCode);

            await _mockPostService.Received(1).GetActivePostBySlugAsync
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

            _mockPostService.GetActivePostBySlugAsync(requestDto, ct)
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

            await _mockPostService.Received(1).GetActivePostBySlugAsync
               (Arg.Any<PostRequestBySlug>(), ct);
        }

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.Invalid, 400, typeof(BadRequestObjectResult))]
        [InlineData(ResultStatus.Conflict, 409, typeof(ConflictObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        public async Task AddPostAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result<PostAdminDetailsDto>.Unauthorized(msg, code),
                ResultStatus.Invalid => Result<PostAdminDetailsDto>.Invalid(msg, code),
                ResultStatus.Conflict => Result<PostAdminDetailsDto>.Conflict(msg, code),
                ResultStatus.NotFound => Result<PostAdminDetailsDto>.NotFound(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockPostService
                .AddPostAsync(Arg.Any<PostCreateDto>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.AddPostAsync(new PostCreateDto { Content = "New content" });

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

            await _mockPostService.Received(1)
                .AddPostAsync(Arg.Any<PostCreateDto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostAddedSuccessfully()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);

            var postDto = TestDataHelper.GetPostCreateDto();

            var token = CancellationToken.None;

            var responseDto = TestDataHelper.CreatePostAdminDetailsDto(post);
            var successMessage = PostM.Success.PostAddedSuccessfully;

            var serviceResult = Result<PostAdminDetailsDto>.Success(responseDto, successMessage);

            _mockPostService.AddPostAsync(postDto, token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.AddPostAsync(postDto, token);

            // Assert            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<PostAdminDetailsDto>>(createdAtActionResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(successMessage, response.Message);
            Assert.NotNull(response.Data);
            var data = response.Data;

            Assert.Equal(responseDto.Title, data.Title);
            Assert.Equal(responseDto.CategoryId, data.CategoryId);

            await _mockPostService.Received(1).AddPostAsync(postDto, token);
        }

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.Invalid, 400, typeof(BadRequestObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        [InlineData(ResultStatus.Conflict, 409, typeof(ConflictObjectResult))]
        public async Task UpdatePostAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result<PostAdminDetailsDto>.Unauthorized(msg, code),
                ResultStatus.Invalid => Result<PostAdminDetailsDto>.Invalid(msg, code),
                ResultStatus.NotFound => Result<PostAdminDetailsDto>.NotFound(msg, code),
                ResultStatus.Conflict => Result<PostAdminDetailsDto>.Conflict(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockPostService
                .UpdatePostAsync(Arg.Any<int>(), Arg.Any<PostUpdateDto>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.UpdatePostAsync(1, new PostUpdateDto { Content = "Updated content" });

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

            await _mockPostService.Received(1)
                .UpdatePostAsync(Arg.Any<int>(), Arg.Any<PostUpdateDto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int postId = 1;
            var postDto = TestDataHelper.GetPostUpdateDto();
            var errorMessage = CategoryM.Errors.CategoryNotFound;
            var errorCode = PostM.Errors.CategoryNotFoundCode;

            var serviceResult = Result<PostAdminDetailsDto>.NotFound(errorMessage, errorCode);

            _mockPostService.UpdatePostAsync(1, postDto, Arg.Any<CancellationToken>()).Returns(serviceResult);

            // Act
            var result = await _postsController.UpdatePostAsync(postId, postDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);

            await _mockPostService.Received(1).UpdatePostAsync(postId, postDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn200OkAndSuccessMessage_WhenPostUpdatedSuccessfully()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postId = post.Id;
            var postDto = TestDataHelper.ToPostUpdateDto(post);

            var token = CancellationToken.None;

            var responseDto = TestDataHelper.CreatePostAdminDetailsDto(post);
            var successMessage = PostM.Success.PostUpdatedSuccessfully;

            var serviceResult = Result<PostAdminDetailsDto>.Success(responseDto, successMessage);

            _mockPostService.UpdatePostAsync(postId, postDto, token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.UpdatePostAsync(postId, postDto, token);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PostAdminDetailsDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.Equal(successMessage, response.Message);
            Assert.NotNull(response.Data);
            var data = response.Data!;

            Assert.Equal(responseDto.Title, data.Title);
            Assert.Equal(responseDto.Id, data.Id);

            await _mockPostService.Received(1).UpdatePostAsync(postId, postDto, token);
        }

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        public async Task DeletePostAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result.Unauthorized(msg, code),
                ResultStatus.NotFound => Result.NotFound(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockPostService
                .DeletePostAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _postsController.DeletePostAsync(1);

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

            await _mockPostService.Received(1)
                .DeletePostAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnOk_IfPostRemovedSuccessfully()
        {
            // Arrange
            var postId = 1;
            var token = CancellationToken.None;

            var successMessage = PostM.Success.PostDeletedSuccessfully;

            var serviceResult = Result<bool>.Success(true, successMessage);

            _mockPostService.DeletePostAsync(postId, token)
                 .Returns(serviceResult);

            // Act
            var result = await _postsController.DeletePostAsync(postId, token);

            // Assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(okObjectResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);
            Assert.Equal(successMessage, response.Message);

            await _mockPostService.Received(1).DeletePostAsync(postId, token);
        }
    }
}