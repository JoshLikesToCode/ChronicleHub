using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChronicleHub.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory 
    : IDesignTimeDbContextFactory<ChronicleHubDbContext>
{
    public ChronicleHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChronicleHubDbContext>();

        const string connectionString = "Data Source=chroniclehub.db";

        optionsBuilder.UseSqlite(connectionString);

        return new ChronicleHubDbContext(optionsBuilder.Options);
    }
}