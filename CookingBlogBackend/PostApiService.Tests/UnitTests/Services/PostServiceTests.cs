using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IRepository<Post> _mockRepository;
        private readonly ISnippetGeneratorService _mockSnippetGenerator;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IRepository<Post>>();
            _mockSnippetGenerator = Substitute.For<ISnippetGeneratorService>();
            _postService = new PostService(_mockRepository, _mockSnippetGenerator);
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

            Assert.Equal(PageSize, pagedData.Items.Count);
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

            Assert.Equal(PageSize, pagedData.Items.Count);
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

            Assert.Equal(ExpectedCountOnPage, pagedData.Items.Count);
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

            Assert.Equal(ExpectedTotalPostCount, pagedData.Items.Count);
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
            const string Expected_Title = "Ultimate Classic Chili Cheeseburger Recipe";
            const int PageNumber = 2;
            const int PageSize = 2;

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var categories = TestDataHelper.GetCulinaryCategories();

            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert            
            Assert.Equal(3, result.SearchTotalPosts);

            Assert.Single(result.SearchPostList);

            Assert.Equal(1, result.SearchPostList.First().Id);
            Assert.Equal(Expected_Title, result.SearchPostList.First().Title);

            Assert.Equal(ExpectedSnippet, result.SearchPostList.First().SearchSnippet);

            var expectedPostModel = allTestPosts.First(p => p.Id == 1);
            var actualDto = result.SearchPostList.First();

            TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPostModel, actualDto);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyPostsList_WithZeroTotalCount()
        {
            // Arrange
            const string Query = "Not Found Query";
            const int ExpectedPostsCount = 0;
            const int PageNumber = 2;
            const int PageSize = 2;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.NotNull(result.SearchPostList);
            Assert.Empty(result.SearchPostList);
            Assert.Equal(ExpectedPostsCount, result.SearchTotalPosts);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyList_WhenPageNumberIsOutOfRange()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 10;
            const int PageSize = 2;

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.Equal(3, result.SearchTotalPosts);
            Assert.Empty(result.SearchPostList);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_FirstPage_WithDefaultParameters()
        {
            // Arrange
            const string Query = "Chili";

            var categories = TestDataHelper.GetCulinaryCategories();
            var allTestPosts = TestDataHelper.GetSearchedPost(categories);

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            var expectedSortedPosts = filteredPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            // Act
            var result = await _postService.SearchPostsWithTotalCountAsync(Query);

            // Assert
            Assert.Equal(3, result.SearchTotalPosts);
            Assert.Equal(3, result.SearchPostList.Count);
            Assert.Equal(2, result.SearchPostList.First().Id);

            Assert.All(result.SearchPostList, (searchPostDto, index) =>
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
        public async Task GetPostByIdAsync_ShouldReturnPost_WithComments()
        {
            // Arrange
            var postId = 2;

            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, categories, commentCount: 3, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var post = await _postService.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithoutComments()
        {
            // Arrange
            var postId = 2;
            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, categories, generateComments: false, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var post = await _postService.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var newPost = TestDataHelper.GetSinglePost(categories);

            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            _mockRepository.AddAsync(newPost, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(newPost));

            // Act
            var result = await _postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);

            await _mockRepository.Received(1)
                .AnyAsync(Arg.Is<Expression<Func<Post, bool>>>(p =>
                    p.Compile()(newPost)), Arg.Any<CancellationToken>());

            await _mockRepository.Received(1).AddAsync(newPost, Arg.Any<CancellationToken>());

            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var originalPost = TestDataHelper.GetSinglePost(categories);
            int postId = originalPost.Id;

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            var inputPostData = new Post
            {
                Id = postId,
                Title = "New Title",
                Description = originalPost.Description,
                Content = originalPost.Content,
                Author = originalPost.Author,
                ImageUrl = originalPost.ImageUrl,
                MetaTitle = originalPost.MetaTitle,
                MetaDescription = originalPost.MetaDescription,
                Slug = "new-slug-value"
            };

            _mockRepository.GetByIdAsync(postId, token)!
                .Returns(originalPost);

            _mockRepository.UpdateAsync(Arg.Any<Post>(), token)
                .Returns(Task.CompletedTask);

            // Act
            var resultPost = await _postService.UpdatePostAsync(postId, inputPostData, token);

            // Assert

            Assert.NotNull(resultPost);
            Assert.Equal(inputPostData.Title, resultPost.Title);
            Assert.Equal(inputPostData.Slug, resultPost.Slug);

            await _mockRepository.Received(1)
                .GetByIdAsync(originalPost.Id, token);

            await _mockRepository.Received(1)
                .UpdateAsync(Arg.Is<Post>(p =>
                    p.Title == "New Title" &&
            p.Slug == "new-slug-value" &&
            p.Content == originalPost.Content), token);

            await _mockRepository.Received(1)
                .SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePost_WhenSaveChangesSucceeds()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            var post = TestDataHelper.GetSinglePost(categories);

            _mockRepository.GetByIdAsync(post.Id, Arg.Any<CancellationToken>())!
                .Returns(post);

            _mockRepository.DeleteAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _postService.DeletePostAsync(post.Id);

            // Assert
            await _mockRepository.Received(1).GetByIdAsync(post.Id, Arg.Any<CancellationToken>());

            await _mockRepository.Received(1).DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == post.Id), Arg.Any<CancellationToken>());
        }
    }
}