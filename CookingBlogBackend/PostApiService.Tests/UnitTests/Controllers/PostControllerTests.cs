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
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyList_WhenNoPostsAvailableYet()
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

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(pagedResult);

            // Act
            var result = await _postsController.GetPostsWithTotalPostCountAsync(queryParameters);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);
            Assert.True(response.Success);

            await _mockPostService.Received(1)
                .GetPostsWithTotalPostCountAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnOk_WhenPostsExist()
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

            _mockPostService.GetPostsWithTotalPostCountAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                token)
                .Returns(pagedResult);

            // Act
            var result = await _postsController.GetPostsWithTotalPostCountAsync(queryParameters, token);

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
                .GetPostsWithTotalPostCountAsync(
                Arg.Is(ExpectedPageNumber),
                Arg.Is(ExpectedPageSize),
                token);
        }

        [Fact]
        public async Task SearchPosts_ShouldReturnOk_WhenPostsExist()
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

            _mockPostService.SearchPostsWithTotalCountAsync(
                Query,
                ExpectedPageNumber,
                ExpectedPageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.SearchPostsWithTotalCountAsync(queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Same(posts, response.Data);

            await _mockPostService.Received(1).SearchPostsWithTotalCountAsync(
                Query,
                ExpectedPageNumber,
                ExpectedPageSize,
                token);
        }

        [Fact]
        public async Task SearchPosts_ShouldReturnOkWithEmptyList_WhenNoPostsMatch()
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

            _mockPostService.SearchPostsWithTotalCountAsync(
                Query,
                queryParameters.PageNumber,
                queryParameters.PageSize,
                token)
                .Returns(serviceResult);

            // Act
            var result = await _postsController.SearchPostsWithTotalCountAsync(queryParameters, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Same(searchPagedResult.Items, response.Data);

            await _mockPostService.Received(1).SearchPostsWithTotalCountAsync(
                Query,
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
        public async Task AddPostAsync_ShouldReturn409Conflict_WhenPostAlreadyExists()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postDto = TestDataHelper.ToPostCreateDto(post);

            var errorMessage = PostM.Errors.PostTitleOrSlugAlreadyExist;
            var errorCode = PostM.Errors.PostAlreadyExistCode;

            var serviceResult = Result<PostAdminDetailsDto>.Conflict(errorMessage, errorCode);

            _mockPostService.AddPostAsync(postDto, Arg.Any<CancellationToken>()).Returns(serviceResult);

            // Act
            var result = await _postsController.AddPostAsync(postDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(conflictResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.Equal((int)HttpStatusCode.Conflict, conflictResult.StatusCode);

            await _mockPostService.Received(1).AddPostAsync(postDto);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postDto = TestDataHelper.ToPostCreateDto(post);
            var errorMessage = CategoryM.Errors.CategoryNotFound;
            var errorCode = PostM.Errors.CategoryNotFoundCode;

            var serviceResult = Result<PostAdminDetailsDto>.NotFound(errorMessage, errorCode);

            _mockPostService.AddPostAsync(postDto, Arg.Any<CancellationToken>()).Returns(serviceResult);

            // Act
            var result = await _postsController.AddPostAsync(postDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);

            await _mockPostService.Received(1).AddPostAsync(postDto);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostAddedSuccessfully()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postDto = TestDataHelper.ToPostCreateDto(post);

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

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn404NotFound_WhenPostDoesNotExist()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postId = post.Id;

            var postDto = TestDataHelper.ToPostUpdateDto(post);

            var errorMessage = string.Format(PostM.Errors.PostNotFound, postDto.Title);
            var errorCode = PostM.Errors.PostNotFoundCode;

            var serviceResult = Result<PostAdminDetailsDto>.NotFound(errorMessage, errorCode);

            _mockPostService.UpdatePostAsync(postId, postDto, Arg.Any<CancellationToken>()).Returns(serviceResult);

            // Act
            var result = await _postsController.UpdatePostAsync(postId, postDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);

            await _mockPostService.Received(1).UpdatePostAsync(postId, postDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturn409Conflict_WhenPostAlreadyExist()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);
            var postId = post.Id;

            var updatedTitle = post.Title;
            var postDto = TestDataHelper.ToPostUpdateDto(post, updatedTitle);

            var errorMessage = string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist, postDto.Title, postDto.Slug);
            var errorCode = PostM.Errors.PostAlreadyExistCode;

            var serviceResult = Result<PostAdminDetailsDto>.Conflict(errorMessage, errorCode);

            _mockPostService.UpdatePostAsync(postId, postDto, Arg.Any<CancellationToken>()).Returns(serviceResult);

            // Act
            var result = await _postsController.UpdatePostAsync(postId, postDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(conflictResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal((int)HttpStatusCode.Conflict, conflictResult.StatusCode);

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

        [Fact]
        public async Task DeletePostAsync_ShouldReturn404NotFoundResult_IfPostDoesNotExists()
        {
            // Arrange
            var postId = 1;
            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            var serviceResult = Result<bool>.NotFound(errorMessage, errorCode);

            _mockPostService.DeletePostAsync(postId, Arg.Any<CancellationToken>())
                 .Returns(serviceResult);

            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundObjectResult.Value);

            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundObjectResult.StatusCode);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);

            await _mockPostService.Received(1).DeletePostAsync(postId, Arg.Any<CancellationToken>());
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
            var response = Assert.IsType<ApiResponse<bool>>(okObjectResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);
            Assert.Equal(successMessage, response.Message);

            await _mockPostService.Received(1).DeletePostAsync(postId, token);
        }
    }
}