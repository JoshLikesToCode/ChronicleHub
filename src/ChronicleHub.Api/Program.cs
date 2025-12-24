using ChronicleHub.Api.Middleware;
using ChronicleHub.Infrastructure.Services;
using ChronicleHub.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(new CompactJsonFormatter()));

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

// Configure graceful shutdown
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Configure OpenTelemetry
var serviceName = builder.Configuration.GetValue<string>("ServiceName") ?? "ChronicleHub";
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            // Enrich traces with additional information
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("http.request.correlation_id",
                    httpRequest.HttpContext.GetCorrelationId());
            };
            options.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("http.response.correlation_id",
                    httpResponse.HttpContext.GetCorrelationId());
            };
        })
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Run migrations on startup if enabled (default: true for dev, configurable for prod)
var runMigrations = builder.Configuration.GetValue<bool>("Database:RunMigrationsOnStartup", true);

if (runMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChronicleHubDbContext>();

        // Only run migrations for relational databases (not in-memory)
        if (db.Database.GetType().Name != "InMemoryDatabase" &&
            !(db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            db.Database.Migrate(); // for SQLite, this will create the DB file and tables if needed
        }
    }
}

// Configure the HTTP request pipeline.
// Use Swagger based on configuration (not just environment)
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection when not in container mode (DOTNET_RUNNING_IN_CONTAINER)
// This prevents noisy logs in Kubernetes environments
var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!isRunningInContainer && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Correlation ID must be first to ensure all logs have correlation context
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handling - must be early to catch all exceptions
app.UseMiddleware<ProblemDetailsExceptionMiddleware>();

// Track request duration for response metadata
app.UseMiddleware<RequestTimingMiddleware>();

// Custom API Key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

// app.UseAuthentication();
// app.UseAuthorization();

    app.MapControllers();

    Log.Information("Starting ChronicleHub API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }