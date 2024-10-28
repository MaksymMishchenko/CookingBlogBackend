using Microsoft.EntityFrameworkCore;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class IntegrationTestFixture : IDisposable
    {
        public ApplicationDbContext Context { get; private set; }
        public PostService PostService { get; private set; }
        public IntegrationTestFixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            Context = new ApplicationDbContext(options);
            PostService = new PostService(Context);
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
