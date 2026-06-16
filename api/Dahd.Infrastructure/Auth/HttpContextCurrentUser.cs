using System.Security.Claims;
using Dahd.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Dahd.Infrastructure.Auth;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
