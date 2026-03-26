using Testcontainers.PostgreSql;

namespace PostApiService.Tests.Infrastructure
{
    public static class SharedDbContainer
    {
        private static readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:latest")
            .WithReuse(true)
            .Build();

        private static readonly Lazy<Task> _initializer = new(() => _container.StartAsync());

        public static string ConnectionString => _container.GetConnectionString();

        public static Task StartAsync() => _initializer.Value;

        public static Task StopAsync() => _container.StopAsync();
    }
}
