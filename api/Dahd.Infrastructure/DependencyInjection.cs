using Dahd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dahd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDahdInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("Default")
                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=DahdDev;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<DahdDbContext>(opts => opts.UseSqlServer(conn));
        return services;
    }
}
