using Microsoft.Extensions.DependencyInjection;
using PostApiService.Infrastructure.Services;

namespace PostApiService.Tests.Fixtures
{
    public class ServiceTestFixture : BaseTestFixture
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            base.ConfigureTestServices(services);

            services.AddTestWebContext();
        }

        public (T service, ApplicationDbContext dbContext, TestWebContext webContext) GetScopedService<T>() where T : class
        {           
            var scope = Services!.CreateScope();
            var provider = scope.ServiceProvider;

            return (
                provider.GetRequiredService<T>(),
                provider.GetRequiredService<ApplicationDbContext>(),
                (TestWebContext)provider.GetRequiredService<IWebContext>()
            );
        }

        public UserManager<IdentityUser> GetUserManager()
        {
            var scope = Services!.CreateScope();
            return scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        }
    }
}
