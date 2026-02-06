using PostApiService.Repositories;

namespace PostApiService.Tests.IntegrationTests.RepoTests
{
    public class PostsRepositoryTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;

        public PostsRepositoryTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<(ApplicationDbContext context, PostRepository repo)> SetupRepoAsync(List<Post> postsToSeed)
        {
            var context = _fixture.CreateUniqueContext();
            var categories = TestDataHelper.GetCulinaryCategories();

            await _fixture.SeedCategoryAsync(context, categories);

            if (postsToSeed != null && postsToSeed.Any())
            {
                await _fixture.SeedDatabaseAsync(context, postsToSeed);
            }

            return (context, new PostRepository(context));
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
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = _fixture.GeneratePosts(5, categories, 0);

            posts[0].Title = "SpecialQuery Item";
            posts[0].IsActive = true;
            posts[0].Category = categories.First(c => c.Slug == "beverages");

            posts[1].IsActive = false;

            posts[2].Category = categories.First(c => c.Slug == "desserts");
            posts[2].IsActive = true;

            var (context, repo) = await SetupRepoAsync(posts);

            // Act & Assert
            using (context)
            {
                var result = repo.GetFilteredPosts(search, active, slug).ToList();

                Assert.Equal(expectedCount, result.Count);
            }
        }

        [Fact]
        public async Task GetFilteredPosts_ShouldSearchInAllFields()
        {
            // Arrange
            const string Query = "soups";
            var (context, repo) = await SetupRepoAsync(new List<Post>());
            var categories = context.Categories.ToList();

            var posts = _fixture.GeneratePosts(3, categories, 0);

            posts[0].Title = $"Something {Query}";
            posts[1].Description = $"Contains {Query} inside";
            posts[2].Content = $"End with {Query}";

            await _fixture.SeedDatabaseAsync(context, posts);

            // Act
            var result = repo.GetFilteredPosts(Query, null, null).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, p =>
            {
                Assert.True(p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query));
            });
        }
    }
}
