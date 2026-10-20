using Microsoft.AspNetCore.Http;
using MockQueryable;
using PostApiService.Helper;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AdminPostServiceTests
    {
        private readonly IPostRepository _mockRepository;
        private readonly IWebContext _mockWebContext;
        private readonly IHtmlSanitizationService _mockSanitizationService;
        private readonly IPublicCategoryService _mockCategoryService;
        private readonly IFileStorageService _mockFileStorageService;
        private readonly AdminPostService _adminPostService;
        private const string OldPreviewUrl = "https://res.cloudinary.com/x/image/upload/v1/posts/old-abc123.webp";
        private const string NewPreviewUrl = "https://res.cloudinary.com/x/image/upload/v2/posts/new-def456.webp";

        public AdminPostServiceTests()
        {
            _mockRepository = Substitute.For<IPostRepository>();
            _mockWebContext = Substitute.For<IWebContext>();
            _mockSanitizationService = Substitute.For<IHtmlSanitizationService>();
            _mockCategoryService = Substitute.For<IPublicCategoryService>();
            _mockFileStorageService = Substitute.For<IFileStorageService>();
            _adminPostService = new AdminPostService(_mockRepository,
                _mockWebContext, _mockSanitizationService, _mockCategoryService, _mockFileStorageService);
        }

        private Post ArrangeSuccessfulUpdate(int postId, string? existingPreviewUrl)
        {
            var post = new Post
            {
                Id = postId,
                CategoryId = 99,
                AuthorId = "user-id",
                PreviewImageUrl = existingPreviewUrl!
            };

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>()).Returns(post);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

            return post;
        }

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

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSuccessResult_WithCorrectData_WhenPostExists()
        {
            // Arrange
            int postId = 2;
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns(TestUserData.AdminId);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IsContributor.Returns(false);

            var categories = TestDataHelper.GetCulinaryCategories();

            string[] authorIds = new[] { TestUserData.AdminId, TestUserData.ContributorId };
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, categories, authorIds: authorIds, generateComments: false, generateIds: true);

            var expectedPost = testPosts.First(p => p.Id == postId);

            expectedPost.Author = new Microsoft.AspNetCore.Identity.IdentityUser
            {
                Id = expectedPost.AuthorId,
                UserName = TestUserData.AdminUserName
            };

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.IsType<PostAdminDetailsDto>(result.Value);

            Assert.Equal(expectedPost.Id, result.Value.Id);
            Assert.Equal(expectedPost.Title, result.Value.Title);
            Assert.Equal(expectedPost.Author.UserName, result.Value.Author);

            _mockRepository.Received(1).AsQueryable();
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int nonExistentId = 999;

            _mockWebContext.UserId.Returns(TestUserData.AdminId);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IsContributor.Returns(false);

            var testPosts = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable => testPosts);

            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            // Act
            var result = await _adminPostService.GetPostByIdAsync(nonExistentId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(errorCode, result.ErrorCode);

            _mockRepository.Received(1).AsQueryable();
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            int postId = 1;
            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnForbidden_WhenContributorRequestsForeignPost()
        {
            // Arrange
            int postId = 1;
            string contributorId = "contributor-1-id";
            string anotherAuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns(contributorId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments(count: 1, categories, authorIds: new[] { anotherAuthorId }, generateComments: false, generateIds: true);
            testPosts.First().Id = postId;

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const string invalidPostContent = "<script></script>";
            var postCreateDto = TestDataHelper.GetPostCreateDto(invalidPostContent);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(PostM.Errors.Empty, result.Message);
            Assert.Equal(PostM.Errors.EmptyCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnConflict_WhenPostExists()
        {
            // Arrange
            var createPostDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var result = await _adminPostService.AddPostAsync(createPostDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist,
                createPostDto.Title, createPostDto.Slug), result.Message);
            Assert.Equal(PostM.Errors.PostAlreadyExistCode, result.ErrorCode);

            await _mockRepository.Received(1)
                .AnyAsync(Arg.Is<Expression<Func<Post, bool>>>(p =>
                    p.Compile()(new Post { Title = createPostDto.Title, Slug = createPostDto.Slug })),
                    Arg.Any<CancellationToken>());

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnNotFound_WhenCategoryDoesNotExists()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var expectedMessage = CategoryM.Errors.CategoryNotFound;
            var expectedErrorCode = PostM.Errors.CategoryNotFoundCode;

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Equal(expectedErrorCode, result.ErrorCode);

            await _mockRepository.Received(1)
                .AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>());

            await _mockCategoryService.Received(1)
                .ExistsAsync(Arg.Is(postCreateDto.CategoryId), Arg.Any<CancellationToken>());

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldStripHtmlFromTitleAndSlug()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto(
                title: "<h1>Clean Title</h1>",
                slug: "<b>clean-slug</b>"
            );

            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), ct).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), ct).Returns(true);

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success("/images/posts/clean-slug.webp"));

            var savedPost = TestDataHelper.ToEntity(postCreateDto, "Safe content", "user-id");
            savedPost.Title = postCreateDto.Title.StripHtml();
            savedPost.Slug = postCreateDto.Slug.StripHtml();

            _mockRepository.GetByIdWithAuthorsAsync(Arg.Any<int>(), ct)
                .Returns(savedPost);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Clean Title", result.Value!.Title);
            Assert.Equal("clean-slug", result.Value.Slug);

            await _mockRepository.Received(1).AddAsync(Arg.Any<Post>(), ct);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnSuccess_WhenPostAddedSuccessfully()
        {
            // Arrange                       
            var postCreateDto = TestDataHelper.GetPostCreateDto();
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(false);
            _mockCategoryService.ExistsAsync(postCreateDto.CategoryId, token).Returns(true);

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success("/images/posts/test-preview.webp"));

            var savedPost = TestDataHelper.ToEntity(postCreateDto, "Safe content", "user-id");
            _mockRepository.GetByIdWithAuthorsAsync(Arg.Any<int>(), token)
                .Returns(savedPost);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var data = result.Value!;
            Assert.Equal(postCreateDto.Title, data.Title);
            Assert.Equal(postCreateDto.Slug, data.Slug);
            Assert.Equal(postCreateDto.CategoryId, data.CategoryId);

            Assert.Equal(PostM.Success.PostAddedSuccessfully, result.Message);

            await _mockRepository.Received(1).AddAsync(Arg.Is<Post>(p =>
                 p.Title == postCreateDto.Title &&
                 p.Slug == postCreateDto.Slug &&
                 p.CategoryId == postCreateDto.CategoryId), token);
            await _mockRepository.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnInvalid_WhenImageSaveFails()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto();
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(false);
            _mockCategoryService.ExistsAsync(postCreateDto.CategoryId, token)
                .Returns(true);

            var expectedMessage = "Зовнішнє повідомлення про помилку розміру";
            var expectedErrorCode = "IMAGE_TOO_SMALL";

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Invalid(expectedMessage, expectedErrorCode));

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, token);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Equal(expectedErrorCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), token);
            await _mockRepository.DidNotReceive().SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            var dto = TestDataHelper.GetPostUpdateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            const int postId = 1;
            var dto = TestDataHelper.GetPostUpdateDto();

            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnForbidden_WhenContributorUpdatesForeignPost()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.AuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns("contributor-1-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(
                title: "Title", slug: "slug", content: "Some valid content");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldSucceed_WhenAdminUpdatesForeignPost()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.AuthorId = "another-author-id";

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, TestDataHelper.GetPostUpdateDto());

            // Assert
            Assert.True(result.IsSuccess);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            var dto = TestDataHelper.GetPostUpdateDto(content: "<script></script>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(PostM.Errors.Empty, result.Message);
            Assert.Equal(PostM.Errors.EmptyCode, result.ErrorCode);

            _mockSanitizationService.Received(1).SanitizePost(Arg.Any<string>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>())
                .Returns((Post?)null);

            var dto = TestDataHelper.GetPostUpdateDto(content: "<script>alert('xss')</script>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnConflict_WhenTitleAlreadyExistsForAnotherPost()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(content: "Test post content");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist,
                dto.Title, dto.Slug), result.Message);
            Assert.Equal(PostM.Errors.PostAlreadyExistCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_WhenCategoryDoesNotExists()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

            var dto = TestDataHelper.GetPostUpdateDto(categoryId: 2);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);

            await _mockCategoryService.Received(1)
                .ExistsAsync(Arg.Is(dto.CategoryId), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldNotCheckCategory_WhenCategoryUnchanged()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.CategoryId = 1;

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, TestDataHelper.GetPostUpdateDto());

            // Assert
            Assert.True(result.IsSuccess);
            await _mockCategoryService.DidNotReceive()
                .ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnSuccess_WhenPostUpdatedSuccessfully()
        {
            // Arrange
            const int postId = 1;
            var ct = CancellationToken.None;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);

            var dto = TestDataHelper.GetPostUpdateDto();

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostUpdatedSuccessfully, result.Message);

            var data = Assert.IsType<PostAdminDetailsDto>(result.Value);
            Assert.Equal(dto.Title, data.Title);
            Assert.Equal(dto.Slug, data.Slug);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldStripHtmlFromTitleAndSlug()
        {
            // Arrange
            const int postId = 1;
            var ct = CancellationToken.None;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);

            var dto = TestDataHelper.GetPostUpdateDto(
                title: "<h1>Clean Title</h1>",
                slug: "<b>clean-slug</b>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Clean Title", result.Value!.Title);
            Assert.Equal("clean-slug", result.Value.Slug);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReplacePreviewAndDeleteOld_WhenNewFileProvided()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var file = Substitute.For<IFormFile>();
            var dto = TestDataHelper.GetPostUpdateDto(slug: "<b>clean-slug</b>", previewImage: file);

            _mockFileStorageService.SavePostPreviewAsync(file, "clean-slug")
                .Returns(Result<string>.Success(NewPreviewUrl));
            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewPreviewUrl, post.PreviewImageUrl);
            Assert.Equal(NewPreviewUrl, result.Value!.PreviewImageUrl);

            await _mockFileStorageService.Received(1).SavePostPreviewAsync(file, "clean-slug");
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldKeepPreview_WhenNoFileProvided()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto();

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OldPreviewUrl, post.PreviewImageUrl);

            await _mockFileStorageService.DidNotReceive()
                .SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInvalid_WhenImageUploadFails()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Error(ImageM.Errors.UploadFailed, ImageM.Errors.UploadFailedCode));

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(ImageM.Errors.UploadFailed, result.Message);
            Assert.Equal(OldPreviewUrl, post.PreviewImageUrl);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldStillSucceed_WhenDeletingOldPreviewFails()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success(NewPreviewUrl));
            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl)
                .Returns(Result.Error(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode));

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewPreviewUrl, post.PreviewImageUrl);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldNotUploadImage_WhenSlugConflicts()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.Equal(ResultStatus.Conflict, result.Status);
            await _mockFileStorageService.DidNotReceive()
                .SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnForbidden_WhenContributorDeletesForeignPost()
        {
            // Arrange
            const int postId = 1;
            string contributorId = "contributor-1-id";
            string anotherAuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns(contributorId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post { Id = postId, AuthorId = anotherAuthorId, PreviewImageUrl = OldPreviewUrl });

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().DeleteAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn404NotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int postId = 99;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns((Post)null!);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == postId), Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn200Ok_WhenPostRemove()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, Title = "To be deleted", AuthorId = userId };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).DeleteAsync(existingPost, ct);
            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePreviewImage_WhenPostHasPreview()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, AuthorId = userId, PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).DeleteAsync(existingPost, ct);
            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnSuccess_WhenPreviewDeletionFails()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, AuthorId = userId, PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl)
                .Returns(Result.Error(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode));

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePreviewImage_WhenAdminDeletesForeignPost()
        {
            // Arrange
            int postId = 1;
            var existingPost = new Post { Id = postId, AuthorId = "another-author-id", PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.True(result.IsSuccess);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }
    }
}
