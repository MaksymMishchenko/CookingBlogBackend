using Microsoft.EntityFrameworkCore;

namespace PostApiService.Tests.IntegrationTests
{
    public class IntegrationTestFixture
    {
        public ApplicationDbContext CreateContext()
        {

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
