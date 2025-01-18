using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private string _databaseName;

        public InMemoryDatabaseFixture()
        {
            _databaseName = Guid.NewGuid().ToString();
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;
        }

        public ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_options);
        }

        public async Task InitializeAsync()
        {
            using var context = CreateContext();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            context.Posts.Add(TestDataHelper.GetSinglePost());
            context.Comments.AddRange(TestDataHelper.GetListWithComments());
            await context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
