namespace Dahd.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
    bool IsAuthenticated { get; }
}
