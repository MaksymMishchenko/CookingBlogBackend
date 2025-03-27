using Microsoft.Extensions.DependencyInjection;

namespace PostApiService.Tests.Fixtures
{
    public class PostFixture : BaseTestFixture
    {
        private const string _connectionString = "Server=MAX\\SQLEXPRESS;Database=TestPost;Trusted_Connection=True;" +
            "MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public PostFixture() : base(_connectionString, useDatabase: true) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }
}
