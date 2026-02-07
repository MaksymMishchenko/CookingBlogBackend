using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IPostRepository _mockRepository;
        private readonly IWebContext _mockWebContext;
        private readonly IHtmlSanitizationService _mockSanitizationService;
        private readonly ICategoryService _mockCategoryService;
        private readonly ISnippetGeneratorService _mockSnippetGenerator;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IPostRepository>();
            _mockWebContext = Substitute.For<IWebContext>();
            _mockSanitizationService = Substitute.For<IHtmlSanitizationService>();
            _mockCategoryService = Substitute.For<ICategoryService>();
            _mockSnippetGenerator = Substitute.For<ISnippetGeneratorService>();
            _postService = new PostService(_mockRepository, _mockWebContext, _mockSanitizationService, _mockCategoryService, _mockSnippetGenerator);
        }

        [Theory]
        [InlineData(1, 2, 2, 5)]
        [InlineData(3, 2, 1, 5)]
        public async Task GetPostsPagedAsync_Pagination_ShouldReturnCorrectSubsets(
        int page, int size, int expectedItemsCount, int totalInDb)
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(totalInDb, categories, generateIds: true);
            var mockQuery = posts.AsQueryable().BuildMock();

            _mockRepository.GetFilteredPosts(null, true, null).Returns(mockQuery);

            // Act
            var result = await _postService.GetPostsPagedAsync(pageNumber: page, pageSize: size);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedResult<PostListDto>>(result.Value);

            Assert.Equal(expectedItemsCount, data.Items.Count());
            Assert.Equal(totalInDb, data.TotalCount);
        }

        [Fact]
        public async Task GetPostsPagedAsync_SearchMode_ShouldReturnSearchPostListDto()
        {
            // Arrange
            const string SearchTerm = "pizza";
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories);
            var mockQuery = posts.AsQueryable().BuildMock();

            _mockRepository.GetFilteredPosts(SearchTerm, true, null).Returns(mockQuery);
            _mockSnippetGenerator.CreateSnippet(Arg.Any<string>(), SearchTerm, 100).Returns("...pizza...");

            // Act
            var result = await _postService.GetPostsPagedAsync(search: SearchTerm);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedSearchResult<SearchPostListDto>>(result.Value);
            Assert.Equal(SearchTerm, data.Query);
            Assert.All(data.Items, item => Assert.NotEmpty(item.SearchSnippet));
        }

        [Fact]
        public async Task GetPostsPagedAsync_CategoryMode_ShouldCheckExistenceAndReturnPosts()
        {
            // Arrange
            const string CategorySlug = "cooking";
            _mockCategoryService.ExistsBySlugAsync(CategorySlug, Arg.Any<CancellationToken>()).Returns(true);

            var mockQuery = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.GetFilteredPosts(null, true, CategorySlug).Returns(mockQuery);

            // Act
            var result = await _postService.GetPostsPagedAsync(categorySlug: CategorySlug);

            // Assert
            Assert.True(result.IsSuccess);
            await _mockCategoryService.Received(1).ExistsBySlugAsync(CategorySlug, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsPagedAsync_InvalidCategory_ShouldReturnNotFound()
        {
            // Arrange
            const string FakeCategory = "fake-cat";
            _mockCategoryService.ExistsBySlugAsync(FakeCategory, Arg.Any<CancellationToken>()).Returns(false);

            // Act
            var result = await _postService.GetPostsPagedAsync(categorySlug: FakeCategory);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldReturnUnauthorized_WhenUserIdIsEmpty()
        {
            // Arrange
            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _postService.GetAdminPostsPagedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            _mockRepository.DidNotReceive().GetFilteredPosts(null, null, null);
        }

        [Theory]
        [MemberData(nameof(TestDataHelper.GetPostFilterData), MemberType = typeof(TestDataHelper))]
        public async Task GetAdminPostsPagedAsync_ShouldFilterCorrectlyByIsActive
            (string? search, string? categorySlug, bool? onlyActive, int expectedCount)
        {
            // Arrange                       
            const int LargePageSize = 10;
            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("admin-id");

            _mockCategoryService.ExistsBySlugAsync(Arg.Any<string>(), ct).Returns(true);

            var categories = TestDataHelper.GetCulinaryCategories();
            var allPosts = TestDataHelper.GetAdminTestPosts
               (categories);

            var expectedFilteredList = allPosts
                .Where(p => string.IsNullOrEmpty(search) || p.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Where(p => string.IsNullOrEmpty(categorySlug) || p.Category.Slug == categorySlug)
                .Where(p => !onlyActive.HasValue || p.IsActive == onlyActive.Value)
                .AsQueryable()
                .BuildMock();

            _mockRepository.GetFilteredPosts(search, onlyActive, categorySlug)
                .Returns(expectedFilteredList);

            // Act
            var result = await _postService.GetAdminPostsPagedAsync(search,
                categorySlug, onlyActive, pageSize: LargePageSize, ct: ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedCount, result.Value!.Items.Count());

            _mockRepository.Received(1).GetFilteredPosts(search, onlyActive, categorySlug);
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldMapToAdminPostListDtoWithCorrectFields()
        {
            // Arrange
            const int ExpectedCommentCount = 5;
            const int PostCount = 1;
            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("admin-id");

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(
                count: PostCount, categories, commentCount: ExpectedCommentCount, generateIds: true);
            var testPost = posts.First();
            testPost.IsActive = true;

            var mockQueryable = posts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredPosts(null, null, null)
                .Returns(mockQueryable);

            // Act
            var result = await _postService.GetAdminPostsPagedAsync(ct: ct);

            // Assert            
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var pagedData = result.Value!;
            var dto = pagedData.Items.First();

            Assert.IsType<AdminPostListDto>(dto);

            Assert.Equal(testPost.Id, dto.Id);
            Assert.Equal(testPost.Title, dto.Title);
            Assert.Equal(testPost.IsActive, dto.IsActive);
            Assert.Equal(testPost.Category.Name, dto.CategoryName);
            Assert.Equal(testPost.CreatedAt, dto.CreatedAt);

            _mockRepository.Received(1).GetFilteredPosts(null, null, null);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSuccessResult_WithCorrectData_WhenPostExists()
        {
            // Arrange
            int postId = 2;
            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, categories, generateComments: false, generateIds: true);

            var expectedPost = testPosts.First(p => p.Id == postId);

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _postService.GetPostByIdAsync(postId, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.IsType<PostAdminDetailsDto>(result.Value);

            Assert.Equal(expectedPost.Id, result.Value.Id);
            Assert.Equal(expectedPost.Title, result.Value.Title);
            Assert.Equal(expectedPost.Author, result.Value.Author);

            _mockRepository.Received(1).AsQueryable();
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int nonExistentId = 999;
            var testPosts = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(testPosts);

            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            // Act
            var result = await _postService.GetPostByIdAsync(nonExistentId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(errorCode, result.ErrorCode);

            _mockRepository.Received(1).AsQueryable();
        }

        [Theory]
        [InlineData("valid-category", "<h1></h1>")]
        [InlineData("   ", "valid-slug")]
        [InlineData("  ", "  ")]
        public async Task GetPostBySlugAsync_ShouldReturnInvalid_WhenInputsAreEmptyAfterHtmlStriping(string category, string slug)
        {
            // Act
            var dto = new PostRequestBySlug { Category = category, Slug = slug };

            var result = await _postService.GetPostBySlugAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(PostM.Errors.SlugAndCategoryRequired, result.Message);
            Assert.Equal(PostM.Errors.SlugAndCategoryRequiredCode, result.ErrorCode);

            _mockRepository.DidNotReceive().GetFilteredPosts(null, true, category);
        }

        [Fact]
        public async Task GetPostBySlugAsync_ShouldReturnNotFound_WhenPostDoesNotExistOrIsInactive()
        {
            // Arrange
            var dto = new PostRequestBySlug { Category = "any-category", Slug = "unknown-slug" };
            var ct = CancellationToken.None;

            var emptyData = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.GetFilteredPosts(
                Arg.Any<string>(),
                Arg.Is(true),
                Arg.Is<string>(s => s.Trim().ToLowerInvariant() == dto.Category)
            )
            .Returns(emptyData);

            // Act
            var result = await _postService.GetPostBySlugAsync(dto, ct);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFoundByPath, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundByPathCode, result.ErrorCode);

            _mockRepository.Received(1).GetFilteredPosts(
                Arg.Any<string>(),
                Arg.Is(true),
                Arg.Is<string>(s => s.Trim().ToLowerInvariant() == dto.Category)
            );
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnNotFound_WhenCategoryMismatch()
        {
            // Arrange
            var categoryPasta = new Category { Name = "Pasta", Slug = "pasta" };

            var requestDto = new PostRequestBySlug
            {
                Category = "desserts",
                Slug = "carbonara"
            };

            var testPosts = new List<Post>
            {
                new Post
                {
                    Slug = "carbonara",
                    Category = categoryPasta,
                    IsActive = true
                }
            }.AsQueryable().BuildMock();

            _mockRepository.GetFilteredPosts(null, true, requestDto.Category).Returns(testPosts);

            // Act            
            var result = await _postService.GetPostBySlugAsync(requestDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFoundByPath, result.Message);

            _mockRepository.Received(1).GetFilteredPosts(null, true, requestDto.Category);
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnPost_WhenCategoryAndSlugAreCorrectAndPostIsActive()
        {
            // Arrange
            const string expectedSlug = "classic-carbonara";
            const string expectedCategory = "pasta";

            var requestDto = TestDataHelper.CreatePostRequest("  PASTA  ", "Classic-Carbonara");

            var pastaCategory = new Category { Slug = expectedCategory, Name = "Italian Pasta" };
            var recipePost = new Post
            {
                Id = 1,
                Slug = expectedSlug,
                Category = pastaCategory,
                Title = "Classic Carbonara with Guanciale",
                IsActive = true
            };

            var mockData = new List<Post> { recipePost }.AsQueryable().BuildMock();
            _mockRepository.GetFilteredPosts(
                Arg.Any<string>(),
                Arg.Is(true),
                Arg.Is<string>(s => s.Trim().ToLowerInvariant() == expectedCategory)
            )
            .Returns(mockData);

            // Act
            var result = await _postService.GetPostBySlugAsync(requestDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var dto = result.Value!;
            Assert.Equal(expectedSlug, dto.Slug);
            Assert.Equal(expectedCategory, dto.CategorySlug);
            Assert.Contains("Carbonara", dto.Title);

            _mockRepository.Received(1).GetFilteredPosts(
                Arg.Any<string>(),
                Arg.Is(true),
                Arg.Is<string>(s => s.Trim().ToLowerInvariant() == expectedCategory)
            );
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _postService.AddPostAsync(postCreateDto);

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
        public async Task AddPostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const string invalidPostContent = "<script></script>";
            var postCreateDto = TestDataHelper.GetPostCreateDto(invalidPostContent);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await _postService.AddPostAsync(postCreateDto);

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
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var result = await _postService.AddPostAsync(createPostDto);

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
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var expectedMessage = CategoryM.Errors.CategoryNotFound;
            var expectedErrorCode = PostM.Errors.CategoryNotFoundCode;

            // Act
            var result = await _postService.AddPostAsync(postCreateDto);

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
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), ct).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), ct).Returns(true);

            // Act
            var result = await _postService.AddPostAsync(postCreateDto, ct);

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
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(false);
            _mockCategoryService.ExistsAsync(postCreateDto.CategoryId, token).Returns(true);

            // Act
            var result = await _postService.AddPostAsync(postCreateDto, token);

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
        public async Task UpdatePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            var postUpdateDto = TestDataHelper.GetPostUpdateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const int postId = 1;
            const string invalidPostContent = "<script></script>";
            var postUpdateDto = TestDataHelper.GetPostUpdateDto(invalidPostContent);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto);

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
            const string content = "<script>alert('xss')</script>";
            var postUpdateDto = TestDataHelper.GetPostUpdateDto(content);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns((Post?)null);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnConflict_WhenTitleAlreadyExistsForAnotherPost()
        {
            // Arrange           
            const int postId = 1;
            const string content = "Test post content";
            var postUpdateDto = TestDataHelper.GetPostUpdateDto(content);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post());
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(),
                Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist,
                postUpdateDto.Title, postUpdateDto.Slug), result.Message);
            Assert.Equal(PostM.Errors.PostAlreadyExistCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_WhenCategoryDoesNotExists()
        {
            // Arrange
            const int postId = 1;
            var postUpdateDto = TestDataHelper.GetPostUpdateDto(categoryId: 2);

            _mockWebContext.UserId.Returns("user-id");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post { CategoryId = 99 });
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(false);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);

            await _mockCategoryService.Received(1)
                .ExistsAsync(Arg.Is(postUpdateDto.CategoryId), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldStripHtmlFromTitleAndSlug()
        {
            // Arrange
            const int postId = 1;
            var postUpdateDto = TestDataHelper.GetPostUpdateDto(
                title: "<h1>Clean Title</h1>",
                slug: "<b>clean-slug</b>"
            );

            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post());
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), ct).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), ct).Returns(true);

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Clean Title", result.Value!.Title);
            Assert.Equal("clean-slug", result.Value.Slug);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnSuccess_WhenPostUpdatedSuccessfully()
        {
            // Arrange
            const int postId = 1;
            var postUpdateDto = TestDataHelper.GetPostUpdateDto();

            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post());
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), ct).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), ct).Returns(true);

            var expectedMessage = PostM.Success.PostUpdatedSuccessfully;

            // Act
            var result = await _postService.UpdatePostAsync(postId, postUpdateDto, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.IsType<PostAdminDetailsDto>(result.Value);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var data = result.Value!;
            Assert.Equal(postUpdateDto.Title, data.Title);
            Assert.Equal(postUpdateDto.Slug, data.Slug);
            Assert.Equal(expectedMessage, result.Message);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn404NotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int postId = 99;

            _mockWebContext.UserId.Returns("user-id");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns((Post)null!);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == postId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn200Ok_WhenPostRemove()
        {
            // Arrange
            int postId = 1;
            var existingPost = new Post { Id = postId, Title = "To be deleted" };
            _mockWebContext.UserId.Returns("user-id");
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            var ct = CancellationToken.None;

            // Act
            var result = await _postService.DeletePostAsync(postId, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).DeleteAsync(existingPost, ct);
            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }
    }
}