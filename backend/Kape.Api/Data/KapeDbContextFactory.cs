using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kape.Api.Data;

public sealed class KapeDbContextFactory : IDesignTimeDbContextFactory<KapeDbContext>
{
    public KapeDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=KapeApp;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<KapeDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new KapeDbContext(options);
    }
}
