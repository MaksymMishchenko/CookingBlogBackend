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

        [Fact]
        public async Task GetActivePostsPagedAsync_ShouldReturnCorrectPageSortedByDateDescending()
        {
            // Arrange            
            const int PageNumber = 1;
            const int PageSize = 10;
            const int ActiveCount = 15;
            const int InactiveCount = 5;
            const int ExpectedCommentCountPerPost = 11;
            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var activePosts = TestDataHelper.GetPostsWithComments(count: ActiveCount, categories, commentCount: ExpectedCommentCountPerPost);
            activePosts.ForEach(p => p.IsActive = true);

            var inactivePosts = TestDataHelper.GetPostsWithComments(count: InactiveCount, categories, commentCount: ExpectedCommentCountPerPost);
            inactivePosts.ForEach(p => p.IsActive = false);

            var allPosts = activePosts.Concat(inactivePosts).ToList();

            var activeQueryableMock = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(activeQueryableMock);

            // Act
            var result = await _postService.GetActivePostsPagedAsync(PageNumber, PageSize, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(ActiveCount, pagedData.TotalCount);

            var expectedSortedActivePosts = activePosts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Assert.All(pagedData.Items.Select((postDto, index) => (postDto, index)), x =>
            {
                var expectedPost = expectedSortedActivePosts[x.index];
                TestDataHelper.AssertPostListDtoMapping(expectedPost, x.postDto, ExpectedCommentCountPerPost);
            });

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task GetActivePostsPagedAsync_ShouldReturnSecondPage_WhenMultiplePagesExist()
        {
            // Arrange
            const int PageNumber = 2;
            const int PageSize = 10;
            const int ActivePostsCount = 23;
            const int InactivePostsCount = 5;
            const int ExpectedCommentCountPerPost = 7;

            var categories = TestDataHelper.GetCulinaryCategories();

            var activePosts = TestDataHelper.GetPostsWithComments(
                count: ActivePostsCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            activePosts.ForEach(p => p.IsActive = true);

            var inactivePosts = TestDataHelper.GetPostsWithComments(
                count: InactivePostsCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            inactivePosts.ForEach(p => p.IsActive = false);

            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            // Act
            var result = await _postService.GetActivePostsPagedAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(PageSize, pagedData.Items.Count());
            Assert.Equal(ActivePostsCount, pagedData.TotalCount);

            var sortedActivePosts = activePosts.OrderByDescending(p => p.CreatedAt).ToList();
            var expectedFirstPostOnSecondPage = sortedActivePosts.Skip(PageSize).First();
            var unexpectedPostFromFirstPage = sortedActivePosts.First();

            Assert.Equal(expectedFirstPostOnSecondPage.Id, pagedData.Items.First().Id);
            Assert.NotEqual(unexpectedPostFromFirstPage.Id, pagedData.Items.First().Id);

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task GetActivePostsPagedAsync_ShouldReturnLastPartialPage()
        {
            // Arrange
            const int PageNumber = 3;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 5;
            const int ExpectedCountOnPage = 5;

            var categories = TestDataHelper.GetCulinaryCategories();

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            testPosts.ForEach(p => p.IsActive = true);

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            // Act
            var result = await _postService.GetActivePostsPagedAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            var pagedData = result.Value!;

            Assert.Equal(ExpectedCountOnPage, pagedData.Items.Count());
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            var expectedPostsOnPage = testPosts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Assert.Equal(
                expectedPostsOnPage.Select(p => p.Id),
                pagedData.Items.Select(p => p.Id)
            );

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task GetActivePostsPagedAsync_ShouldReturnEmptyList_WhenPageNumberIsTooLarge()
        {
            // Arrange
            const int PageNumber = 5;
            const int PageSize = 10;
            const int ActivePostsCount = 25;
            const int ExpectedCommentCountPerPost = 1;

            var categories = TestDataHelper.GetCulinaryCategories();

            var activePosts = TestDataHelper.GetPostsWithComments(
                count: ActivePostsCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            activePosts.ForEach(p => p.IsActive = true);

            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            // Act
            var result = await _postService.GetActivePostsPagedAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Empty(pagedData.Items);
            Assert.Equal(ActivePostsCount, pagedData.TotalCount);

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task GetActivePostsPagedAsync_ShouldReturnFullPage_WhenTotalCountEqualsPageSize()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 15;
            const int ActivePostsCount = 15;
            const int ExpectedCommentCountPerPost = 5;

            var categories = TestDataHelper.GetCulinaryCategories();

            var activePosts = TestDataHelper.GetPostsWithComments(
                count: ActivePostsCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            activePosts.ForEach(p => p.IsActive = true);

            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            // Act
            var result = await _postService.GetActivePostsPagedAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(ActivePostsCount, pagedData.Items.Count());
            Assert.Equal(ActivePostsCount, pagedData.TotalCount);

            var expectedSortedPosts = activePosts.OrderByDescending(p => p.CreatedAt).ToList();
            Assert.Equal(expectedSortedPosts.First().Id, pagedData.Items.First().Id);
            Assert.Equal(expectedSortedPosts.Last().Id, pagedData.Items.Last().Id);

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task SearchActivePostsPagedAsync_ShouldReturn_PagedSearchPosts_WithTotalCount()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 2;
            const int PageSize = 2;
            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var activePosts = allTestPosts.Where(p => p.IsActive).ToList();
            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            var expectedFilteredPosts = activePosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            int expectedTotalCount = expectedFilteredPosts.Count;
            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, expectedTotalCount);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Is(Query), Arg.Is(100))
                .Returns(ExpectedSnippet);

            // Act
            var result = await _postService.SearchActivePostsPagedAsync(Query, PageNumber, PageSize, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Equal(expectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            var expectedCountOnPage = expectedFilteredPosts.Skip((PageNumber - 1) * PageSize).Take(PageSize).Count();
            Assert.Equal(expectedCountOnPage, data.Items.Count());

            if (expectedCountOnPage > 0)
            {
                var expectedPostModel = expectedFilteredPosts.Skip((PageNumber - 1) * PageSize).First();
                var actualDto = data.Items.First();

                Assert.Equal(expectedPostModel.Id, actualDto.Id);
                Assert.Equal(ExpectedSnippet, actualDto.SearchSnippet);
                TestDataHelper.AssertSearchActivePostsPagedAsync(expectedPostModel, actualDto);
            }

            _mockRepository.Received(1).GetActive();

            _mockSnippetGenerator.Received(expectedCountOnPage).CreateSnippet(
                Arg.Any<string>(),
                Arg.Is(Query),
                Arg.Is(100)
            );
        }

        [Fact]
        public async Task SearchActivePostsPagedAsync_ShouldReturn_FirstPage_WithDefaultParameters()
        {
            // Arrange
            const string Query = "Chili";
            const int ExpectedActiveMatchCount = 3;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var activePosts = allTestPosts.Where(p => p.IsActive).ToList();
            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Is(Query), Arg.Is(100))
                .Returns(ExpectedSnippet);

            var expectedSortedPosts = activePosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, ExpectedActiveMatchCount);

            // Act            
            var result = await _postService.SearchActivePostsPagedAsync(Query);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Equal(ExpectedActiveMatchCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);
            Assert.Equal(ExpectedActiveMatchCount, data.Items.Count());

            Assert.Equal(expectedSortedPosts.First().Id, data.Items.First().Id);

            Assert.All(data.Items.Select((searchPostDto, index) => (searchPostDto, index)), x =>
            {
                var expectedPost = expectedSortedPosts[x.index];
                TestDataHelper.AssertSearchActivePostsPagedAsync(expectedPost, x.searchPostDto);
                Assert.Equal(ExpectedSnippet, x.searchPostDto.SearchSnippet);
            });

            _mockRepository.Received(1).GetActive();

            _mockSnippetGenerator.Received(ExpectedActiveMatchCount).CreateSnippet(
                Arg.Any<string>(),
                Arg.Is(Query),
                Arg.Is(100)
            );
        }

        [Fact]
        public async Task SearchActivePostsPagedAsync_ShouldReturn_EmptyPostsList_WithZeroTotalCount()
        {
            // Arrange
            const string Query = "Not Found Query";
            const int ExpectedTotalCount = 0;
            const int PageNumber = 1;
            const int PageSize = 10;

            var expectedMessage = string.Format(PostM.Success.SearchNoResults, Query);

            var categories = TestDataHelper.GetCulinaryCategories();

            var allTestPosts = TestDataHelper.GetSearchedPost(categories);
            var activePosts = allTestPosts.Where(p => p.IsActive).ToList();

            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            // Act
            var result = await _postService.SearchActivePostsPagedAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.NotNull(data);
            Assert.Empty(data.Items);

            Assert.Equal(ExpectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            _mockRepository.Received(1).GetActive();

            _mockSnippetGenerator.DidNotReceive().CreateSnippet(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>());
        }

        [Fact]
        public async Task SearchActivePostsPagedAsync_ShouldReturn_EmptyItems_ButCorrectTotalCount_WhenPageNumberIsOutOfRange()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 10;
            const int PageSize = 2;
            const int ExpectedActiveMatchCount = 3;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var activePosts = allTestPosts.Where(p => p.IsActive).ToList();
            var mockQueryable = activePosts.AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(mockQueryable);

            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, ExpectedActiveMatchCount);

            // Act
            var result = await _postService.SearchActivePostsPagedAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Empty(data.Items);
            Assert.Equal(ExpectedActiveMatchCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            _mockSnippetGenerator.DidNotReceive().CreateSnippet(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>());

            _mockRepository.Received(1).GetActive();
        }

        [Fact]
        public async Task GetActivePostsByCategoryPagedAsync_ShouldReturnNotFound_IfCategoryDoesNotExist()
        {
            // Arrange
            const string slug = "non-existent-category";
            var ct = CancellationToken.None;

            _mockCategoryService.ExistsBySlugAsync(slug, ct)
                .Returns(false);

            // Act
            var result = await _postService.GetActivePostsByCategoryPagedAsync(slug, 1, 10, ct);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);

            _mockRepository.DidNotReceive().GetActive();

            await _mockCategoryService.Received(1).ExistsBySlugAsync(slug, ct);
        }

        [Fact]
        public async Task GetActivePostsByCategoryPagedAsync_ShouldReturnListOfActivePosts_IfCategoryExists()
        {
            // Arrange
            var myCategory = new Category { Id = 7, Name = "Breakfast", Slug = "breakfast" };
            var categories = TestDataHelper.GetCulinaryCategories();

            var targetActivePosts = TestDataHelper.GetPostsWithComments(3, categories, forcedCategory: myCategory);
            targetActivePosts.ForEach(p => p.IsActive = true);

            var otherActivePosts = TestDataHelper.GetPostsWithComments(2, categories);
            otherActivePosts.ForEach(p => p.IsActive = true);

            var allActivePosts = targetActivePosts.Concat(otherActivePosts).ToList();

            _mockCategoryService.ExistsBySlugAsync(myCategory.Slug, Arg.Any<CancellationToken>())
                .Returns(true);

            _mockRepository.GetActive().Returns(allActivePosts.AsQueryable().BuildMock());

            // Act
            var result = await _postService.GetActivePostsByCategoryPagedAsync(myCategory.Slug, 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            var data = result.Value!;

            Assert.Equal(targetActivePosts.Count, data.TotalCount);
            Assert.All(data.Items, item => Assert.Equal(myCategory.Slug, item.CategorySlug));

            _mockRepository.Received(1).GetActive();

            await _mockCategoryService.Received(1).ExistsBySlugAsync(
                myCategory.Slug, Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(1, 2, 2)]
        [InlineData(2, 2, 1)]
        public async Task GetActivePostsByCategoryPagedAsync_ShouldPaginateCorrectlyWithinCategory(
            int pageNumber, int pageSize, int expectedCount)
        {
            // Arrange
            var myCategory = new Category { Id = 7, Name = "Breakfast", Slug = "breakfast" };
            var categories = TestDataHelper.GetCulinaryCategories();

            var targetActivePosts = TestDataHelper.GetPostsWithComments(3, categories, forcedCategory: myCategory);
            targetActivePosts.ForEach(p => p.IsActive = true);

            var otherActivePosts = TestDataHelper.GetPostsWithComments(2, categories);
            otherActivePosts.ForEach(p => p.IsActive = true);

            var allActivePosts = targetActivePosts.Concat(otherActivePosts).ToList();

            _mockCategoryService.ExistsBySlugAsync(myCategory.Slug, Arg.Any<CancellationToken>())
                .Returns(true);

            _mockRepository.GetActive().Returns(allActivePosts.AsQueryable().BuildMock());

            // Act
            var result = await _postService.GetActivePostsByCategoryPagedAsync(myCategory.Slug, pageNumber, pageSize);

            // Assert
            Assert.True(result.IsSuccess);
            var data = result.Value!;

            Assert.Equal(3, data.TotalCount);
            Assert.Equal(expectedCount, data.Items.Count());

            Assert.All(data.Items, item => Assert.Equal(myCategory.Slug, item.CategorySlug));

            _mockRepository.Received(1).GetActive();
            await _mockCategoryService.Received(1).ExistsBySlugAsync(
                myCategory.Slug, Arg.Any<CancellationToken>());
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
        public async Task GetActivePostBySlugAsync_ShouldReturnInvalid_WhenInputsAreEmptyAfterHtmlStriping(string category, string slug)
        {
            // Act
            var dto = new PostRequestBySlug { Category = category, Slug = slug };

            var result = await _postService.GetActivePostBySlugAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(PostM.Errors.SlugAndCategoryRequired, result.Message);
            Assert.Equal(PostM.Errors.SlugAndCategoryRequiredCode, result.ErrorCode);

            _mockRepository.DidNotReceive().GetActive();
        }

        [Fact]
        public async Task GetActivePostBySlugAsync_ShouldReturnNotFound_WhenPostDoesNotExistOrIsInactive()
        {
            // Arrange
            var dto = new PostRequestBySlug { Category = "any-category", Slug = "unknown-slug" };
            var ct = CancellationToken.None;

            var emptyData = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.GetActive().Returns(emptyData);

            // Act
            var result = await _postService.GetActivePostBySlugAsync(dto, ct);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFoundByPath, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundByPathCode, result.ErrorCode);

            _mockRepository.Received(1).GetActive();
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

            _mockRepository.GetActive().Returns(testPosts);

            // Act            
            var result = await _postService.GetActivePostBySlugAsync(requestDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFoundByPath, result.Message);

            _mockRepository.Received(1).GetActive();
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
            _mockRepository.GetActive().Returns(mockData);

            // Act
            var result = await _postService.GetActivePostBySlugAsync(requestDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var dto = result.Value!;
            Assert.Equal(expectedSlug, dto.Slug);
            Assert.Equal(expectedCategory, dto.CategorySlug);
            Assert.Contains("Carbonara", dto.Title);

            _mockRepository.Received(1).GetActive();
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