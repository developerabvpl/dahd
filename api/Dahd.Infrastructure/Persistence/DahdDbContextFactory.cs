using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dahd.Infrastructure.Persistence;

public class DahdDbContextFactory : IDesignTimeDbContextFactory<DahdDbContext>
{
    public DahdDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<DahdDbContext>();
        var conn = Environment.GetEnvironmentVariable("DAHD_CONNECTION")
                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=DahdDev;Trusted_Connection=True;TrustServerCertificate=True;";
        builder.UseSqlServer(conn);
        return new DahdDbContext(builder.Options);
    }
}
