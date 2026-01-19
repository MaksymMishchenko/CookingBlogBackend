using Microsoft.Extensions.DependencyInjection;

namespace PostApiService.Tests.Fixtures
{
    public class ExceptionMiddlewareFixture : BaseTestFixture
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddExceptionMiddlewareMocks();
        }

        public void ClearMocks()
        {
            Services!.ClearAllMocks();
        }
    }
}