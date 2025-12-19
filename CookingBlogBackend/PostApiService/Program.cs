using Microsoft.AspNetCore.Mvc;
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

var jwtConfiguration = builder.Configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>() ??
     throw new InvalidOperationException("Jwt configuration is missing in the appsettings.json file.");

builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection("JwtConfiguration"));

// Get an identity connection string from appsettings.json and check for null
var identityConnectionString = builder.Configuration.GetValue<string>
    ("ApiPostIdentity:ConnectionString") ??
    throw new InvalidOperationException
    ("Connection string 'ApiPostIdentity' is not configured.");

// Register AddIdentityDbContext service to the IServiceCollection
builder.Services.AddAppIdentityService(identityConnectionString);

builder.Services.AddApplicationIdentity();

// Register Application Jwt Bearer Authentication 
builder.Services.AddAppJwtAuthentication(jwtConfiguration);

// Register application authorization
// Adds policies and sets up authorization for protected resources.
builder.Services.AddApplicationAuthorization();

// Register the CORS service to allow cross-origin requests (Access-Control-Allow-Origin) 
builder.Services.AddAppCors();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    // Ignores circular references during JSON serialization
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowLocalhost");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.SeedDataAsync();
    await app.SeedUserAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;