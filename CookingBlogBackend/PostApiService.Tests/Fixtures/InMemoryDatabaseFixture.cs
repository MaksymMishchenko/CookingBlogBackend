using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        private DbContextOptions<ApplicationDbContext> _options;

        public InMemoryDatabaseFixture()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        public ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_options);
        }

        public async Task InitializeAsync()
        {
            using var context = CreateContext();
            
            context.Posts.Add(DataFixture.GetSinglePost());
            context.Comments.AddRange(DataFixture.GetListWithComments());
            await context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
