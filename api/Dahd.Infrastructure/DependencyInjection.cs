using Dahd.Application.Abstractions;
using Dahd.Infrastructure.Auditing;
using Dahd.Infrastructure.Auth;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
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

        services.AddDbContext<DahdDbContext>(opts =>
            opts.UseSqlServer(conn, sql =>
                // LocalDB sleeps when idle and drops the first connection after wake-up;
                // retry transparently so the first request after idle no longer fails.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(3),
                    errorNumbersToAdd: null)));

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IAuditLogger, EfAuditLogger>();
        services.AddScoped<IStockLedger, StockLedger>();

        return services;
    }
}
