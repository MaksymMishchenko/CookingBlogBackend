using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Contexts;
using PostApiService.Infrastructure;
using PostApiService.Models;
using PostApiService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Get a connection string from appsettings.json and check for null
var connectionString = builder.Configuration.GetConnectionString
    ("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

// Register AddDbContext service to the IServiceCollection
builder.Services.AddApplicationService(connectionString);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddTransient<DataSeeder>();

// Get an identity connection string from appsettings.json and check for null
var identityConnectionString = builder.Configuration.GetValue<string>("ApiPostIdentity:ConnectionString");

if (string.IsNullOrWhiteSpace(identityConnectionString))
{
    throw new InvalidOperationException("Connection string 'ApiPostIdentity' is not configured.");
}

// Register AddIdentityDbContext service to the IServiceCollection
builder.Services.AddAppIdentityService(identityConnectionString);

// додаємо Identity з ролями
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment()) // Skip seeding in Testing environment
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var seeder = services.GetRequiredService<DataSeeder>();
            await seeder.SeedDataAsync(); // Seed the data when the app starts
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during seeding: {ex.Message}");
        }
    }

    await IdentitySeedData.EnsurePopulatedAsync(app.Services);
}

app.UseCors("AllowAllOrigins");

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
public partial class Program;