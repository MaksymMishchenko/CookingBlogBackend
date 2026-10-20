using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class GetAdminPostsPagedAsyncTests : AdminPostServiceTestsBase
    {
        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldReturnUnauthorized_WhenUserIdIsEmpty()
        {
            // Arrange
            var dto = new AdminPostQueryDto(
                SearchTerm: null,
                CategoryId: null,
                AuthorId: null,
                PageNumber: 1,
                PageSize: 10,
                SortBy: null,
                SortDirection: null,
                OnlyActive: null
            );

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            _mockRepository.DidNotReceive().GetAdminFilteredAndSortedPosts(null, null, null, null, null, null);
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            var dto = new AdminPostQueryDto(
                SearchTerm: null,
                CategoryId: null,
                AuthorId: null,
                PageNumber: 1,
                PageSize: 10,
                SortBy: null,
                SortDirection: null,
                OnlyActive: null
            );

            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            _mockRepository.DidNotReceive().GetAdminFilteredAndSortedPosts(null, null, null, null, null, null);
        }

        [Theory]
        [InlineData(true, "some-other-id", "some-other-id")]
        [InlineData(false, "some-other-id", "my-actual-id")]
        public async Task GetAdminPostsPagedAsync_ShouldEnforceAuthorFiltering_BasedOnRole(
            bool isAdmin, string? inputAuthorId, string expectedRepoAuthorId)
        {
            // Arrange
            var ct = CancellationToken.None;
            var currentUserId = "my-actual-id";

            var dto = new AdminPostQueryDto(
               SearchTerm: null,
               CategoryId: null,
               AuthorId: inputAuthorId,
               PageNumber: 1,
               PageSize: 10,
               SortBy: null,
               SortDirection: null,
               OnlyActive: null
            );

            _mockWebContext.UserId.Returns(currentUserId);
            _mockWebContext.IsAdmin.Returns(isAdmin);
            _mockWebContext.IsContributor.Returns(!isAdmin);

            var mockQueryable = TestDataHelper.GetAdminTestPosts(TestDataHelper.GetCulinaryCategories()).AsQueryable().BuildMock();

            _mockRepository.GetAdminFilteredAndSortedPosts(
                null, null, null, null, null, expectedRepoAuthorId)
                .Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(dto, ct);

            // Assert
            Assert.True(result.IsSuccess);

            _mockRepository.Received(1).GetAdminFilteredAndSortedPosts(
                null, null, null, null, null, expectedRepoAuthorId);
        }

        [Theory]
        [MemberData(nameof(TestDataHelper.GetPostFilterData), MemberType = typeof(TestDataHelper))]
        public async Task GetAdminPostsPagedAsync_ShouldFilterCorrectlyByIsActive
            (string? search, int? categoryId, bool? onlyActive, int expectedCount, string expectedName)
        {
            // Arrange                       
            const int LargePageSize = 10;
            var ct = CancellationToken.None;

            var dto = new AdminPostQueryDto(
               SearchTerm: search,
               CategoryId: categoryId,
               AuthorId: null,
               PageNumber: 1,
               PageSize: LargePageSize,
               SortBy: null,
               SortDirection: null,
               OnlyActive: onlyActive
            );

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            _mockCategoryService.GetNameByIdAsync(categoryId, ct)
               .Returns(Task.FromResult<string?>(expectedName));

            var categories = TestDataHelper.GetCulinaryCategories();
            var allPosts = TestDataHelper.GetAdminTestPosts(categories);

            var expectedFilteredList = allPosts
                .Where(p => string.IsNullOrEmpty(search) || p.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Where(p => !categoryId.HasValue || p.CategoryId == categoryId.Value)
                .Where(p => !onlyActive.HasValue || p.IsActive == onlyActive.Value)
                .AsQueryable()
                .BuildMock();

            _mockRepository.GetAdminFilteredAndSortedPosts(
                Arg.Is(search),
                Arg.Is(onlyActive),
                Arg.Is(categoryId),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>())
                .Returns(expectedFilteredList);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(dto, ct);

            // Assert
            var data = Assert.IsType<PagedResult<AdminPostListDto>>(result.Value);
            Assert.True(result.IsSuccess);
            Assert.Equal(search, data.AppliedFilters!.Search);
            Assert.Equal(expectedName, data.AppliedFilters!.CategoryName);
            Assert.Equal(expectedCount, result.Value!.Items.Count());

            _mockRepository.Received(1).GetAdminFilteredAndSortedPosts(
                Arg.Is(search),
                Arg.Is(onlyActive),
                Arg.Is(categoryId),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldMapToAdminPostListDtoWithCorrectFields()
        {
            // Arrange
            const int ExpectedCommentCount = 5;
            const int PostCount = 1;
            var ct = CancellationToken.None;

            var queryDto = new AdminPostQueryDto(
               SearchTerm: null,
               CategoryId: null,
               AuthorId: null,
               PageNumber: 1,
               PageSize: 10,
               SortBy: null,
               SortDirection: null,
               OnlyActive: null
            );

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            var categories = TestDataHelper.GetCulinaryCategories();

            string[] authorIds = new[] { TestUserData.AdminId, TestUserData.ContributorId };
            var posts = TestDataHelper.GetPostsWithComments(
                count: PostCount, categories, authorIds: authorIds, commentCount: ExpectedCommentCount, generateIds: true);
            var testPost = posts.First();
            testPost.IsActive = true;

            var mockQueryable = posts.BuildMock();
            _mockRepository.GetAdminFilteredAndSortedPosts(null, null, null, null, null, null)
                .Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(queryDto, ct);

            // Assert         
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var pagedData = result.Value!;
            var dto = pagedData.Items.First();

            Assert.IsType<AdminPostListDto>(dto);

            Assert.Equal(testPost.Id, dto.Id);
            Assert.Equal(testPost.Title, dto.Title);
            Assert.Equal(testPost.Slug, dto.Slug);
            Assert.Equal(testPost.IsActive, dto.IsActive);
            Assert.Equal(testPost.CategoryId, dto.CategoryId);
            Assert.Equal(testPost.Category.Name, dto.CategoryName);
            Assert.Equal(testPost.Category.Slug, dto.CategorySlug);
            Assert.Equal(testPost.CreatedAt, dto.CreatedAt);

            _mockRepository.Received(1).GetAdminFilteredAndSortedPosts(null, null, null, null, null, null);
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldPassSortingParametersToRepository()
        {
            // Arrange
            var ct = CancellationToken.None;
            const string expectedSortBy = "title";
            const string expectedSortDirection = "asc";

            var dto = new AdminPostQueryDto(
                SearchTerm: null,
                CategoryId: null,
                AuthorId: null,
                PageNumber: 1,
                PageSize: 10,
                SortBy: expectedSortBy,
                SortDirection: expectedSortDirection,
                OnlyActive: null
            );

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetAdminTestPosts(categories);
            var mockQueryable = posts.AsQueryable().BuildMock();

            _mockRepository.GetAdminFilteredAndSortedPosts(
                null,
                null,
                null,
                expectedSortBy,
                expectedSortDirection,
                null
            ).Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetAdminPostsPagedAsync(dto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepository.Received(1).GetAdminFilteredAndSortedPosts(
                null,
                null,
                null,
                expectedSortBy,
                expectedSortDirection,
                null
            );
        }
    }
}
