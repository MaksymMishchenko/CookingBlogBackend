namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        public ApplicationDbContext CreateUniqueContext()
        {
            var databaseName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ApplicationDbContext(options);
        }

        public List<Post> GeneratePosts(int totalPostCount, ICollection<Category> categories, int commentCount)
        {
            return TestDataHelper.GetPostsWithComments(
                totalPostCount,
                categories,
                commentCount: commentCount,
                generateIds: true)
                .ToList();
        }

        public List<Post> GeneratePostsWithKeywords(string query, ICollection<Category> categories, int totalPostCount)
        {
            return TestDataHelper.GeneratePostsWithKeyword(query, categories, totalPostCount);
        }

        public List<Post> GeneratePostsWithKeywords(ICollection<Category> categories)
        {
            return TestDataHelper.GetPostsForOrLogic(categories);
        }

        public async Task SeedCategoryAsync(ApplicationDbContext context, List<Category> categoryToSeed)
        {
            await context.Database.EnsureCreatedAsync();

            context.Categories.AddRange(categoryToSeed);
            await context.SaveChangesAsync();
        }

        public async Task SeedDatabaseAsync(ApplicationDbContext context, List<Post> postsToSeed)
        {
            await context.Database.EnsureCreatedAsync();

            context.Posts.AddRange(postsToSeed);
            await context.SaveChangesAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public Task InitializeAsync() => Task.CompletedTask;
    }
}
