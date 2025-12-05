using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChronicleHub.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory 
    : IDesignTimeDbContextFactory<ChronicleHubDbContext>
{
    public ChronicleHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChronicleHubDbContext>();

        // This should match the dev connection string
        const string connectionString =
            "Server=localhost;Database=ChronicleHub;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new ChronicleHubDbContext(optionsBuilder.Options);
    }
}