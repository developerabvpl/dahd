using Dahd.Application.Abstractions;
using Dahd.Infrastructure.Auditing;
using Dahd.Infrastructure.Auth;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dahd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDahdInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Provider selection: SQLite by default (file-based, zero install, immune to
        // LocalDB service failures). Set Database:Provider = "SqlServer" to use the
        // ConnectionStrings:Default SQL Server connection instead.
        var provider = config["Database:Provider"] ?? "Sqlite";

        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
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
        }
        else
        {
            var conn = config.GetConnectionString("Sqlite") ?? "Data Source=dahd.db";
            conn = ResolveSqliteDataSource(conn);
            services.AddDbContext<DahdDbContext>(opts => opts.UseSqlite(conn));
        }

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IAuditLogger, EfAuditLogger>();
        services.AddScoped<IStockLedger, StockLedger>();

        return services;
    }

    // A relative SQLite "Data Source" resolves against the process working
    // directory, which under a Windows service / IIS is often a system folder
    // the app can't write to — so reads succeed but the first write (login)
    // throws a 500. Anchor a relative path to the app's own install folder
    // (AppContext.BaseDirectory) so behaviour no longer depends on how the app
    // was launched, and ensure that folder exists.
    private static string ResolveSqliteDataSource(string connectionString)
    {
        var csb = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = csb.DataSource;

        // Leave special sources (in-memory, etc.) untouched.
        if (string.IsNullOrWhiteSpace(dataSource) ||
            dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
            dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        if (!Path.IsPathRooted(dataSource))
        {
            dataSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource));
        }

        var dir = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        csb.DataSource = dataSource;
        return csb.ConnectionString;
    }
}
