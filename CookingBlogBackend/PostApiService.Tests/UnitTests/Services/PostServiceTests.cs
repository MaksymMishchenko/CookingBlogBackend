using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IPostRepository _mockRepository;
        private readonly ICategoryRepository _mockCategoryRepository;
        private readonly ISnippetGeneratorService _mockSnippetGenerator;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IPostRepository>();
            _mockCategoryRepository = Substitute.For<ICategoryRepository>();
            _mockSnippetGenerator = Substitute.For<ISnippetGeneratorService>();
            _postService = new PostService(_mockRepository, _mockCategoryRepository, _mockSnippetGenerator);
        }

        [Theory]
        [InlineData(1, 2, 2, 5)]
        [InlineData(3, 2, 1, 5)]
        public async Task GetPostsPagedAsync_Pagination_ShouldReturnCorrectSubsets(
        int page, int size, int expectedItemsCount, int totalInDb)
        {
            // Arrange
            var dto = new PostQueryDto(
                SearchTerm: null,
                CategorySlug: null,
                PageNumber: page,
                PageSize: size
            );

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(totalInDb, categories, generateIds: true);
            var mockQuery = posts.AsQueryable().BuildMock();

            _mockRepository.GetFilteredPosts(null, true, null).Returns(mockQuery);

            // Act
            var result = await _postService.GetPostsPagedAsync(dto);

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
            var dto = new PostQueryDto(
                SearchTerm: SearchTerm,
                CategorySlug: null,
                PageNumber: 1,
                PageSize: 10
            );

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories);
            var mockQuery = posts.AsQueryable().BuildMock();

            _mockRepository.GetFilteredPosts(SearchTerm, true, null).Returns(mockQuery);
            _mockSnippetGenerator.CreateSnippet(Arg.Any<string>(), SearchTerm, 100).Returns("...pizza...");

            // Act
            var result = await _postService.GetPostsPagedAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedSearchResult<SearchPostListDto>>(result.Value);
            Assert.Equal(SearchTerm, data.AppliedFilters.Search);
            Assert.All(data.Items, item => Assert.NotEmpty(item.SearchSnippet!));
        }

        [Fact]
        public async Task GetPostsPagedAsync_CategoryMode_ShouldCheckExistenceAndReturnPosts()
        {
            // Arrange
            const string CategorySlug = "cooking";
            const string CategoryName = "Cooking Recipes";

            var dto = new PostQueryDto(
                SearchTerm: null,
                CategorySlug: CategorySlug,
                PageNumber: 1,
                PageSize: 10
            );

            _mockCategoryRepository.GetNameBySlugAsync(CategorySlug, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>(CategoryName));

            var mockQuery = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.GetFilteredPosts(null, true, CategorySlug).Returns(mockQuery);

            // Act
            var result = await _postService.GetPostsPagedAsync(dto);

            // Assert
            var data = Assert.IsType<PagedResult<PostListDto>>(result.Value);

            Assert.NotNull(data.AppliedFilters);
            Assert.Equal(CategoryName, data.AppliedFilters.CategoryName);
            Assert.Null(data.AppliedFilters.Search);

            Assert.True(result.IsSuccess);
            await _mockCategoryRepository.Received(1).GetNameBySlugAsync(CategorySlug, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsPagedAsync_InvalidCategory_ShouldReturnNotFound()
        {
            // Arrange
            const string FakeCategory = "fake-cat";
            var dto = new PostQueryDto(
                SearchTerm: null,
                CategorySlug: FakeCategory,
                PageNumber: 1,
                PageSize: 10
            );

            _mockCategoryRepository.GetNameBySlugAsync(FakeCategory, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>(null));

            // Act
            var result = await _postService.GetPostsPagedAsync(dto);

            // Assert            
            Assert.False(result.IsSuccess);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);
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
    }
}