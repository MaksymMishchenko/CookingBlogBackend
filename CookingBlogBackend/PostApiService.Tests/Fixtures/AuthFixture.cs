using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostApiService.Contexts;

namespace PostApiService.Tests.Fixtures
{
    public class AuthFixture : BaseTestFixture
    {
        private const string _identityConnectionString = "Server=MAX\\SQLEXPRESS;Database=IdentityTestDb;" +
            "Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        public AuthFixture() : base("", useDatabase: false) { }

        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll(typeof(DbContextOptions<AppIdentityDbContext>));
            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(_identityConnectionString);
            });
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }
}
