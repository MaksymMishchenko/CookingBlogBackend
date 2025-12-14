using Microsoft.EntityFrameworkCore;
using PostApiService.Models;

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

        public List<Post> GeneratePosts(int totalPostCount, int commentCount)
        {
            return TestDataHelper.GetPostsWithComments(
                totalPostCount,
                commentCount: commentCount,
                generateIds: true)
                .ToList();
        }

        public List<Post> GeneratePostsWithKeywords(string query, int totalPostCount)
        {
            return TestDataHelper.GeneratePostsWithKeyword(query, totalPostCount);
        }

        public List<Post> GeneratePostsWithKeywords()
        {
            return TestDataHelper.GetPostsForOrLogic();
        }        

        public async Task SeedDatabaseAsync(ApplicationDbContext context, List<Post> postsToSeed)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            context.Posts.AddRange(postsToSeed);
            await context.SaveChangesAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public Task InitializeAsync() => Task.CompletedTask;
    }
}
