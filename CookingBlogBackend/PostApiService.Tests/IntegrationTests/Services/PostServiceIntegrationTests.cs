using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class PostServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;

        public PostServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetPostsPagedAsync_InNormalMode_ShouldReturnPagedResult()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ActiveCount = 15;
            const int InactiveCount = 5;
            const int ExpectedCommentCountPerPost = 2;

            var categories = TestDataHelper.GetCulinaryCategories();
            var active = TestDataHelper.GetPostsWithComments(ActiveCount, categories, commentCount: ExpectedCommentCountPerPost);

            active.ForEach(p => { p.IsActive = true; p.Id = 0; });

            var inactive = TestDataHelper.GetPostsWithComments(InactiveCount, categories, commentCount: 0);
            inactive.ForEach(p =>
            {
                p.IsActive = false;
                p.Id = 0;
                p.Slug = $"inactive-{Guid.NewGuid()}";
            });

            var allPosts = active.Concat(inactive).ToList();

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var expectedActivePosts = allPosts
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

            var queryDto = new PublicPostQueryDto(
                    SearchTerm: null,
                    CategorySlug: null,
                    PageNumber: ExpectedPageNumber,
                    PageSize: ExpectedPageSize
            );

            var (service, _, _) = _fixture.GetScopedService<IPublicPostService>();

            // Act            
            var result = await service.GetPostsPagedAsync(queryDto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedResult<PostListDto>>(result.Value);

            Assert.Equal(ActiveCount, data.TotalCount);
            Assert.Null(data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName!);
            Assert.Equal(expectedActivePosts.Count, data.Items.Count());

            Assert.All(data.Items.Select((item, index) => new { item, index }), x =>
            {
                var expected = expectedActivePosts[x.index];

                Assert.Equal(expected.Id, x.item.Id);
                Assert.Equal(expected.Title, x.item.Title);
                Assert.Equal(expected.Category.Name, x.item.Category);
            });
        }

        [Fact]
        public async Task GetPostsPagedAsync_SearchMode_ShouldReturnCorrectSearchDtosAndSnippets()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const string SearchTerm = "pizza";
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 5;
            const int MatchCount = 3;

            var categories = TestDataHelper.GetCulinaryCategories();

            var matches = TestDataHelper.GetPostsWithComments(MatchCount, categories, commentCount: 0);
            matches.ForEach(p =>
            {
                p.Title = $"{SearchTerm} title {Guid.NewGuid()}";
                p.Content = $"This is a long content about {SearchTerm} to generate a snippet.";
                p.IsActive = true;
                p.Id = 0;
            });

            var others = TestDataHelper.GetPostsWithComments(5, categories, commentCount: 0);
            others.ForEach(p =>
            {
                p.Title = $"Regular healthy salad {Guid.NewGuid()}";
                p.Content = "Just some greens and oil.";
                p.IsActive = true;
                p.Id = 0;
                p.Slug = $"regular-salad-{Guid.NewGuid()}";
            });

            var allPosts = matches.Concat(others).ToList();
            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var queryDto = new PublicPostQueryDto(
                SearchTerm: SearchTerm,
                CategorySlug: null,
                PageNumber: ExpectedPageNumber,
                PageSize: ExpectedPageSize
            );

            var (service, _, _) = _fixture.GetScopedService<IPublicPostService>();

            // Act
            var result = await service.GetPostsPagedAsync(queryDto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedSearchResult<SearchPostListDto>>(result.Value);

            Assert.Equal(MatchCount, data.TotalCount);
            Assert.Equal(SearchTerm, data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName);

            Assert.All(data.Items, item =>
            {
                Assert.Contains(SearchTerm, item.Title.ToLower());
                Assert.NotEmpty(item.SearchSnippet!);
                Assert.Contains(SearchTerm, item.SearchSnippet!.ToLower());
            });
        }        

        [Fact]
        public async Task GetPostBySlugAsync_ShouldReturnSuccess_IfPostExistsInDbAndIsActive()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const int ExpectedCommentCount = 5;
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories, commentCount: ExpectedCommentCount);

            posts.ForEach(p =>
            {
                p.Id = 0;
                p.IsActive = true;
                p.Slug = $"slug-{Guid.NewGuid()}";
            });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var requestDto = new PostRequestBySlug
            {
                Category = targetPost.Category.Slug,
                Slug = targetPost.Slug
            };

            var (service, _, _) = _fixture.GetScopedService<IPublicPostService>();

            // Act
            var result = await service.GetPostBySlugAsync(requestDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = Assert.IsType<PostDetailsDto>(result.Value);
            Assert.NotNull(data);

            Assert.Equal(targetPost.Title, data.Title);
            Assert.Equal(targetPost.Author, data.Author);
            Assert.Equal(targetPost.Slug, data.Slug);
            Assert.Equal(targetPost.Category.Slug, data.CategorySlug);
            Assert.Equal(targetPost.Category.Name, data.Category);
            Assert.Equal(ExpectedCommentCount, data.CommentCount);
        }        
    }
}
