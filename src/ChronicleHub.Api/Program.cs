using ChronicleHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // or CamelCase if you prefer
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use SQLite for dev
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=chroniclehub.db";

builder.Services.AddDbContext<ChronicleHubDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();