using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.IntegrationTests
{
    public class IntegrationTestFixture : IDisposable
    {
        public ApplicationDbContext Context { get; private set; }

        public IntegrationTestFixture()
        {

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\ProjectModels;Database=TestPostApiDb;Trusted_Connection=true;")
            .Options;

            Context = new ApplicationDbContext(options);
            Context.Database.Migrate();
        }
        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}
