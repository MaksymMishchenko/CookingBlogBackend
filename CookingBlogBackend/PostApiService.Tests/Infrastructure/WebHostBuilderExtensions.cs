using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace PostApiService.Tests.Infrastructure
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder ConfigureTestConfig(this IWebHostBuilder builder)
        {
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            });
        }
    }
}
