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
            return new ApplicationDbContext(options);
        }
    }
}
