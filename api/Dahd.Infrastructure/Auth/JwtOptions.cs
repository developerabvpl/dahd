namespace Dahd.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "DahdApi";
    public string Audience { get; set; } = "DahdClient";
    public string Key { get; set; } = "REPLACE-WITH-32+CHAR-DEV-ONLY-KEY-________";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}
