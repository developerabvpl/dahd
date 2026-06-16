using Dahd.Domain.Entities;

namespace Dahd.Application.Abstractions;

public record IssuedTokens(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);

public interface ITokenService
{
    IssuedTokens Issue(AppUser user);
    string HashRefreshToken(string token);
    string GenerateRefreshToken();
}
