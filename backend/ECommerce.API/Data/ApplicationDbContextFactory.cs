using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.API.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a default connection string for migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=ecommerce_db;Username=mahammadbbyv");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
