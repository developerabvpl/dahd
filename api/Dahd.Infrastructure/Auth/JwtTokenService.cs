using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dahd.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opt = options.Value;

    public IssuedTokens Issue(AppUser user)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_opt.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_opt.RefreshTokenDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("displayName", user.DisplayName ?? user.Username)
        };
        if (user.WarehouseId.HasValue) claims.Add(new("warehouseId", user.WarehouseId.Value.ToString()));
        if (user.FacilityId.HasValue) claims.Add(new("facilityId", user.FacilityId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refresh = GenerateRefreshToken();

        return new IssuedTokens(access, accessExpires, refresh, refreshExpires);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
