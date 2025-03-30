using Microsoft.Extensions.DependencyInjection;
using PostApiService.Contexts;

namespace PostApiService.Tests.Fixtures
{
    public class AuthFixture : BaseTestFixture
    {
        private const string _identityConnectionString = "Server=MAX\\SQLEXPRESS;Database=AuthTestDb;" +
           "Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public AuthFixture() : base("", _identityConnectionString, useDatabase: false) { }        

        public override async Task DisposeAsync()
        {
            using var scope = Services.CreateScope();
            var cntx = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            await cntx.Database.EnsureDeletedAsync();

            await base.DisposeAsync();
        }
    }
}
