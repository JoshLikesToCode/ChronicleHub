using ChronicleHub.Api.Middleware;
using ChronicleHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // or CamelCase if you prefer
    });

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

// Use SQLite for dev
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=chroniclehub.db";

builder.Services.AddDbContext<ChronicleHubDbContext>(options =>
{
    options.UseSqlite(connectionString);
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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