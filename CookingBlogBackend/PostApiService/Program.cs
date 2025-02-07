using Microsoft.EntityFrameworkCore;
using PostApiService.Infrastructure;
using PostApiService.Models;

using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    // Get a connection string from appsettings.json and check for null
    var connectionString = builder.Configuration.GetConnectionString
        ("DefaultConnection") ??
        throw new InvalidOperationException
            ("Connection string 'DefaultConnection' is not configured.");

    // Register AddDbContext service to the IServiceCollection
    builder.Services.AddApplicationService(connectionString);

    var jwtConfiguration = builder.Configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>() ??
         throw new InvalidOperationException("Jwt configuration is missing in the appsettings.json file.");

    // Register Application Jwt Bearer Authentication 
    builder.Services.AddAppJwtAuthentication(jwtConfiguration);

    // Get an identity connection string from appsettings.json and check for null
    var identityConnectionString = builder.Configuration.GetValue<string>
        ("ApiPostIdentity:ConnectionString") ??
        throw new InvalidOperationException
        ("Connection string 'ApiPostIdentity' is not configured.");

    // Register AddIdentityDbContext service to the IServiceCollection
    builder.Services.AddAppIdentityService(identityConnectionString);

    // Register the CORS service to allow cross-origin requests (Access-Control-Allow-Origin) 
    builder.Services.AddAppCors();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAuthorization();

    var app = builder.Build();

    if (app.Environment.IsDevelopment()) // Skip seeding in Testing environment
    {
        await ServiceRegistration.SeedDataAsync(app);

        await IdentitySeedData.EnsurePopulatedAsync(app.Services);
    }

    app.UseCors("AllowLocalhost");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Critical application error");
}

public partial class Program;