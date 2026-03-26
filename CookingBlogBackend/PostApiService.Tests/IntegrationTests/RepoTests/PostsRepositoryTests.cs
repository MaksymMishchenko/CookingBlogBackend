using Microsoft.Extensions.DependencyInjection;
using PostApiService.Repositories;

namespace PostApiService.Tests.IntegrationTests.RepoTests
{
    [Collection("SharedDatabase")]
    public class PostsRepositoryTests
    {
        private readonly ServiceTestFixture _fixture;

        public PostsRepositoryTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("SpecialQuery", true, "beverages", 1)]
        [InlineData(null, false, null, 1)]
        [InlineData(null, null, "desserts", 1)]
        [InlineData("NonExistent", null, null, 0)]
        public async Task GetFilteredPosts_VarietyTests(
            string? search, bool? active, string? slug, int expectedCount)
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();

            var activePosts = TestDataHelper.GetPostsWithComments(5, categories, commentCount: 1);
            activePosts.ForEach(p => { p.IsActive = true; p.Id = 0; });

            activePosts[0].Title = "This is a SpecialQuery item";
            activePosts[0].Category = categories.First(c => c.Slug == "beverages");

            activePosts[1].Category = categories.First(c => c.Slug == "desserts");

            var inactivePosts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: 0);
            inactivePosts.ForEach(p =>
            {
                p.IsActive = false;
                p.Id = 0;
                p.Slug = $"inactive-{Guid.NewGuid()}";
            });

            var allPosts = activePosts.Concat(inactivePosts).ToList();

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            // Act       
            using var scope = _fixture.Services!.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var result = repo.GetFilteredPosts(search, active, slug).ToList();

            // Assert
            Assert.Equal(expectedCount, result.Count);

            if (expectedCount > 0 && !string.IsNullOrEmpty(search))
            {
                Assert.Contains(result, p =>
                    p.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                );
            }
        }

        [Fact]
        public async Task GetFilteredPosts_ShouldSearchInTitleDescriptionAndContent()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            const string Query = "soups";
            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(3, categories, commentCount: 0);

            posts[0].Title = $"Best {Query} for winter";
            posts[0].IsActive = true;
            posts[0].Id = 0;

            posts[1].Description = $"This article is about {Query}";
            posts[1].IsActive = true;
            posts[1].Id = 0;

            posts[2].Content = $"You should try this {Query} recipe at home";
            posts[2].IsActive = true;
            posts[2].Id = 0;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            // Act
            using var scope = _fixture.Services!.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var result = repo.GetFilteredPosts(Query, null, null).ToList();

            // Assert
            Assert.Equal(3, result.Count);

            Assert.All(result, p =>
            {
                bool existsInAnyField =
                    p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                    p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase);

                Assert.True(existsInAnyField, $"Word '{Query}' not found in any field of post {p.Id}");
            });
        }
    }
}