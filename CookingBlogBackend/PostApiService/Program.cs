using Microsoft.EntityFrameworkCore;
using PostApiService.Infrastructure;
using PostApiService.Middlewares;
using PostApiService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Get a connection string from appsettings.json and check for null
var connectionString = builder.Configuration.GetConnectionString
    ("DefaultConnection") ??
    throw new InvalidOperationException
        ("Connection string 'DefaultConnection' is not configured.");

// Register AddDbContext service to the IServiceCollection
builder.Services.AddApplicationService(connectionString);

// Get an identity connection string from appsettings.json and check for null
var identityConnectionString = builder.Configuration.GetValue<string>
    ("ApiPostIdentity:ConnectionString") ??
    throw new InvalidOperationException
    ("Connection string 'ApiPostIdentity' is not configured.");

// Register AddIdentityDbContext service to the IServiceCollection
builder.Services.AppIdentityService(identityConnectionString);

// Register IdentityBuilder service to the IServiceCollection
builder.Services.AddApplicationIdentity();

builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection("JwtConfiguration"));

var jwtConfiguration = builder.Configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>() ??
     throw new InvalidOperationException("Jwt configuration is missing in the appsettings.json file.");

// Register Application Jwt Bearer Authentication 
builder.Services.AddAppJwtAuthentication(jwtConfiguration);

// Register the CORS service to allow cross-origin requests (Access-Control-Allow-Origin) 
builder.Services.AddAppCors();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment()) 
{
    await app.SeedUserAsync();
    await app.SeedDataAsync();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;