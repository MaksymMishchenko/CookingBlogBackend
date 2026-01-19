using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IRepository<Post> _mockRepository;
        private readonly IWebContext _mockWebContext;
        private readonly IHtmlSanitizationService _mockSanitizationService;
        private readonly ICategoryService _mockCategoryService;
        private readonly ISnippetGeneratorService _mockSnippetGenerator;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IRepository<Post>>();
            _mockWebContext = Substitute.For<IWebContext>();
            _mockSanitizationService = Substitute.For<IHtmlSanitizationService>();
            _mockCategoryService = Substitute.For<ICategoryService>();
            _mockSnippetGenerator = Substitute.For<ISnippetGeneratorService>();
            _postService = new PostService(_mockRepository, _mockWebContext, _mockSanitizationService, _mockCategoryService, _mockSnippetGenerator);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnCorrectPageSortedByDateDescending()
        {
            // Arrange            
            const int PageNumber = 1;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 11;

            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, categories, commentCount: ExpectedCommentCountPerPost);

            _mockRepository.GetTotalCountAsync(token)
                .Returns(ExpectedTotalPostCount);

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(PageSize, pagedData.Items.Count());
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            var expectedPosts = testPosts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Assert.All(pagedData.Items, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = expectedPosts[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping
                (expectedPost, postDto, ExpectedCommentCountPerPost);
            });

            await _mockRepository.Received(1).GetTotalCountAsync(token);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnSecondPage_WhenMultiplePagesExist()
        {
            // Arrange
            const int ExpectedTotalPostCount = 23;
            const int ExpectedCommentCountPerPost = 7;
            const int PageNumber = 2;
            const int PageSize = 10;

            var categories = TestDataHelper.GetCulinaryCategories();

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);

            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
               .Returns(ExpectedTotalPostCount);

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(PageSize, pagedData.Items.Count());
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            var expectedFirstPostOnSecondPage = testPosts
                .OrderByDescending(p => p.CreatedAt)
                .Skip(PageSize)
                .First();

            Assert.Equal(expectedFirstPostOnSecondPage.Id, pagedData.Items.First().Id);

            var unexpectedPostFromFirstPage = testPosts
                .OrderByDescending(p => p.CreatedAt)
                .First();

            Assert.NotEqual(unexpectedPostFromFirstPage.Id, pagedData.Items.First().Id);

            await _mockRepository.Received(1).GetTotalCountAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnLastPartialPage()
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
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(ExpectedTotalPostCount);

            // Act
            var result = await _postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(ExpectedCountOnPage, pagedData.Items.Count());
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            var expectedPostsOnPage = testPosts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Assert.Equal(expectedPostsOnPage.First().Id, pagedData.Items.First().Id);
            Assert.Equal(expectedPostsOnPage.Last().Id, pagedData.Items.Last().Id);

            Assert.Equal(
                expectedPostsOnPage.Select(p => p.Id),
                pagedData.Items.Select(p => p.Id)
            );

            await _mockRepository.Received(1).GetTotalCountAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyList_WhenPageNumberIsTooLarge()
        {
            // Arrange
            const int PageNumber = 5;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 1;

            var categories = TestDataHelper.GetCulinaryCategories();

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(ExpectedTotalPostCount);

            // Act
            var result = await _postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Empty(pagedData.Items);
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            await _mockRepository.Received(1).GetTotalCountAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnFullPage_WhenTotalCountEqualsPageSize()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 15;
            const int ExpectedTotalPostCount = 15;
            const int ExpectedCommentCountPerPost = 5;

            var categories = TestDataHelper.GetCulinaryCategories();

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, categories, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(ExpectedTotalPostCount);

            // Act
            var result = await _postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var pagedData = result.Value!;

            Assert.Equal(ExpectedTotalPostCount, pagedData.Items.Count());
            Assert.Equal(ExpectedTotalPostCount, pagedData.TotalCount);

            var expectedSortedPosts = testPosts.OrderByDescending(p => p.CreatedAt).ToList();
            Assert.Equal(expectedSortedPosts.First().Id, pagedData.Items.First().Id);
            Assert.Equal(expectedSortedPosts.Last().Id, pagedData.Items.Last().Id);

            await _mockRepository.Received(1).GetTotalCountAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_PagedSearchPosts_WithTotalCount()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 2;
            const int PageSize = 2;

            var token = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                        p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            int expectedTotalCount = filteredPosts.Count;
            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, expectedTotalCount);

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Equal(expectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            Assert.Single(data.Items);

            var expectedPostModel = filteredPosts.Last();
            var actualDto = data.Items.First();

            Assert.Equal(expectedPostModel.Id, actualDto.Id);
            Assert.Equal(ExpectedSnippet, actualDto.SearchSnippet);

            TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPostModel, actualDto);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());

            _mockSnippetGenerator.Received(1).CreateSnippet(
                Arg.Any<string>(),
                Arg.Is(Query),
                Arg.Is(100)
            );
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_FirstPage_WithDefaultParameters()
        {
            // Arrange
            const string Query = "Chili";
            const int ExpectedTotalCount = 3;

            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, ExpectedTotalCount);

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var expectedSortedPosts = filteredPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Equal(ExpectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);
            Assert.Equal(ExpectedTotalCount, data.Items.Count());
            Assert.Equal(expectedSortedPosts.First().Id, data.Items.First().Id);

            Assert.All(data.Items, (searchPostDto, index) =>
            {
                var expectedPost = expectedSortedPosts[index];
                TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPost, searchPostDto);

                Assert.Equal(ExpectedSnippet, searchPostDto.SearchSnippet);
            });

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());

            _mockSnippetGenerator.Received(3).CreateSnippet(
                Arg.Any<string>(),
                Arg.Is(Query),
                Arg.Is(100)
            );
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyPostsList_WithZeroTotalCount()
        {
            // Arrange
            const string Query = "Not Found Query";
            const int ExpectedTotalCount = 0;
            const int PageNumber = 1;
            const int PageSize = 10;

            var expectedMessage = string.Format(PostM.Success.SearchNoResults, Query);

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var filteredPosts = allTestPosts
                    .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;
            Assert.NotNull(data);
            Assert.Empty(data.Items);

            Assert.Equal(ExpectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyList_WhenPageNumberIsOutOfRange()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 10;
            const int PageSize = 2;
            const int ExpectedTotalCount = 3;

            var expectedMessage = string.Format(PostM.Success.SearchResultsFound, Query, ExpectedTotalCount);

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)
                         || p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Empty(data.Items);
            Assert.Equal(ExpectedTotalCount, data.TotalSearchCount);
            Assert.Equal(expectedMessage, data.Message);
            Assert.Equal(Query, data.Query);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
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