using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.Fixtures
{
    public class InMemoryDatabaseFixture : IAsyncLifetime
    {
        public ApplicationDbContext GetContext { get; private set; }        

        public InMemoryDatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            GetContext = new ApplicationDbContext(options);
        }

        public async Task DisposeAsync()
        {
            await GetContext.Database.EnsureDeletedAsync();
        }

        public async Task InitializeAsync()
        {
            await GetContext.Database.EnsureCreatedAsync();            
        }
    }
}
