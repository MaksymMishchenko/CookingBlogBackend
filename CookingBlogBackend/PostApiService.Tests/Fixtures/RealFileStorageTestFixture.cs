using Microsoft.Extensions.DependencyInjection;

namespace PostApiService.Tests.Fixtures
{
    public class RealFileStorageTestFixture : ServiceTestFixture
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            base.ConfigureTestServices(services);
            services.UseRealFileStorageForTests();
        }
    }
}