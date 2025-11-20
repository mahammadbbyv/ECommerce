using ECommerce.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Helpers;

public static class TestDbContext
{
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        return context;
    }
}
