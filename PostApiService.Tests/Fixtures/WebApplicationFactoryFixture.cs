﻿using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PostApiService.Tests.Fixtures
{
    public class WebApplicationFactoryFixture : IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; }
        private WebApplicationFactory<Program> _factory;
        private const string _connectionString =
            @"Server=localhost\\SQLEXPRESS;Database=TestIntegration;Trusted_Connection=True;TrustServerCertificate=True";

        public WebApplicationFactoryFixture()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(ApplicationDbContext));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(_connectionString);
                    });
                });
            });
            HttpClient = _factory.CreateClient();
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedService = scope.ServiceProvider;
                var cntx = scopedService.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureCreatedAsync();
            }
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedService = scope.ServiceProvider;
                var cntx = scopedService.GetRequiredService<ApplicationDbContext>();
                await cntx.Database.EnsureDeletedAsync();
            }
        }
    }
}
