using Testcontainers.MsSql;

namespace PostApiService.Tests.Infrastructure
{
    public static class SharedDbContainer
    {
        private static readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(true)
            .Build();

        private static readonly Lazy<Task> _initializer = new(() => _container.StartAsync());

        public static string ConnectionString => _container.GetConnectionString();

        public static Task StartAsync() => _initializer.Value;

        public static Task StopAsync() => _container.StopAsync();
    }
}
