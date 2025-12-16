using ChronicleHub.Api.Middleware;
using ChronicleHub.Infrastructure.Services;
using ChronicleHub.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // or CamelCase if you prefer
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add application services
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Add Swagger based on configuration
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ChronicleHub API",
            Version = "v1",
            Description = "Cloud-native analytics API for user activity events"
        });

        // Add API Key authentication to Swagger
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key authentication using X-Api-Key header. Enter your API key below.",
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

// Configure database based on connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=chroniclehub.db";

builder.Services.AddDbContext<ChronicleHubDbContext>(options =>
{
    // Use PostgreSQL if connection string contains "Host=" or "Server="
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChronicleHubDbContext>();

    // Only run migrations for relational databases (not in-memory)
    if (db.Database.GetType().Name != "InMemoryDatabase" &&
        !db.Database.ProviderName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.Migrate(); // for SQLite, this will create the DB file and tables if needed
    }
}

// Configure the HTTP request pipeline.
// Use Swagger based on configuration (not just environment)
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Global exception handling - must be first to catch all exceptions
app.UseMiddleware<ProblemDetailsExceptionMiddleware>();

// Track request duration for response metadata
app.UseMiddleware<RequestTimingMiddleware>();

// Custom API Key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }