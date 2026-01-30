using PostApiService.Controllers.Filters;
using PostApiService.Infrastructure;
using PostApiService.Infrastructure.Configuration;
using PostApiService.Infrastructure.Constants;
using PostApiService.Middlewares;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.AddAppLogging();

// Get a connection string from appsettings.json and check for null
var connectionString = builder.Configuration.GetConnectionString
    (ConfigConstants.DefaultConnection);    

// Register AddDbContext service to the IServiceCollection
builder.Services.AddApplicationService(builder.Configuration, connectionString!);

var jwtConfiguration = builder.Configuration.GetSection(ConfigConstants.JwtSection).Get<JwtConfiguration>();

builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection(ConfigConstants.JwtSection));

builder.Services.AddApplicationIdentity();

// Register Application Jwt Bearer Authentication 
builder.Services.AddAppJwtAuthentication(jwtConfiguration!);

// Register application authorization
// Adds policies and sets up authorization for protected resources.
builder.Services.AddApplicationAuthorization();

// Register the CORS service to allow cross-origin requests (Access-Control-Allow-Origin) 
builder.Services.AddAppCors();

builder.Services.AddControllers(options =>
{
    // Allow using full method names with the "Async" suffix in routing.
    options.SuppressAsyncSuffixInActionNames = false;

    // Use EmptyBodyBehavior.Allow as the default behavior for the entire application.
    options.AllowEmptyInputInBodyModelBinding = true;
    options.Filters.Add<AutoValidationFilter>();
})
    .AddJsonOptions(options =>
    {
        // Ignores circular references during JSON serialization
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // / Hand over validation control to custom Action Filters.
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

    await app.SeedUserAsync();
    await app.SeedDataAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;