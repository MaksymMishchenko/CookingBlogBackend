using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        public ApplicationDbContext GetContext { get; private set; }

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

        public Task DisposeAsync() => Task.CompletedTask;

        public Task InitializeAsync() => Task.CompletedTask;
    }
}
