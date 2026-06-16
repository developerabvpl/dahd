using Dahd.Application;
using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Infrastructure.Auth;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    DahdDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IAuditLogger audit,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive, ct);
        if (user is null || !hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid username or password." });

        var issued = tokens.Issue(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(issued.RefreshToken),
            ExpiresAt = issued.RefreshExpiresAt
        });
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(AppUser), user.Id, "Login", summary: $"Login by {user.Username}", ct: ct);

        return Ok(new AuthResponse(
            issued.AccessToken, issued.AccessExpiresAt,
            issued.RefreshToken, issued.RefreshExpiresAt,
            ToDto(user)));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(req.RefreshToken);
        var rt = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (rt is null || rt.RevokedAt is not null || rt.ExpiresAt <= DateTime.UtcNow || !rt.User.IsActive)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        var issued = tokens.Issue(rt.User);
        rt.RevokedAt = DateTime.UtcNow;
        rt.ReplacedByTokenHash = tokens.HashRefreshToken(issued.RefreshToken);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = rt.UserId,
            TokenHash = rt.ReplacedByTokenHash,
            ExpiresAt = issued.RefreshExpiresAt
        });
        await db.SaveChangesAsync(ct);

        return Ok(new AuthResponse(
            issued.AccessToken, issued.AccessExpiresAt,
            issued.RefreshToken, issued.RefreshExpiresAt,
            ToDto(rt.User)));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(req.RefreshToken);
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (rt is { RevokedAt: null })
        {
            rt.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me([FromServices] ICurrentUser current, CancellationToken ct)
    {
        if (current.UserId is null) return Unauthorized();
        var user = await db.Users.FindAsync([current.UserId.Value], ct);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    private static UserDto ToDto(AppUser u) =>
        new(u.Id, u.Username, u.DisplayName, u.Email, u.Role, u.WarehouseId, u.FacilityId);
}
