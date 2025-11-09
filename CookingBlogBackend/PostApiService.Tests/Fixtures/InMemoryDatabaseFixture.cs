using Microsoft.EntityFrameworkCore;
using PostApiService.Models;

namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        private const int DefaultPostCount = 25;
        private const int DefaultCommentCount = 5;

        public ApplicationDbContext CreateUniqueContext()
        {            
            var databaseName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ApplicationDbContext(options);
        }

        public List<Post> GeneratePosts()
        {            
            return TestDataHelper.GetPostsWithComments(
                DefaultPostCount,
                commentCount: DefaultCommentCount,
                generateIds: true)
                .ToList();
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
