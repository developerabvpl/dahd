using System.Text;
using Dahd.Domain.Enums;
using Dahd.Infrastructure;
using Dahd.Infrastructure.Auth;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// One-shot backup: `dotnet run -- backup [destPath]`.
// Uses SQLite VACUUM INTO for a consistent single-file snapshot even while the
// server is running (no torn writes). Only valid on the SQLite provider.
if (args.Length >= 1 && string.Equals(args[0], "backup", StringComparison.OrdinalIgnoreCase))
{
    var cfg = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var provider = cfg["Database:Provider"] ?? "Sqlite";
    if (!provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("backup: this command supports the SQLite provider only. For SQL Server use BACKUP DATABASE / SSMS.");
        return 1;
    }

    var srcConn = cfg.GetConnectionString("Sqlite") ?? "Data Source=dahd.db";
    var dest = args.Length >= 2
        ? args[1]
        : $"dahd-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db";

    await using var conn = new Microsoft.Data.Sqlite.SqliteConnection(srcConn);
    await conn.OpenAsync();
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = $"VACUUM INTO '{dest.Replace("'", "''")}'";
        await cmd.ExecuteNonQueryAsync();
    }
    var full = Path.GetFullPath(dest);
    Console.WriteLine($"Backup OK -> {full} ({new FileInfo(full).Length / 1024} KB)");
    return 0;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DAHD API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token here. Get one from POST /api/auth/login."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddDahdInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Migrate + seed with a short retry loop: a sleeping LocalDB can drop the very
// first connection during startup migration (before the app is listening), which
// would otherwise kill the whole process. Give it a few attempts to wake up.
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    const int maxAttempts = 5;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await DahdSeeder.SeedAsync(scope.ServiceProvider);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Startup migrate/seed attempt {Attempt}/{Max} failed (DB may be waking); retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.Run();
return 0;
