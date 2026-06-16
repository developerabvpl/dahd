using System.Text.Json;
using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Infrastructure.Persistence;

namespace Dahd.Infrastructure.Auditing;

public sealed class EfAuditLogger(DahdDbContext db, ICurrentUser current) : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        object? before = null,
        object? after = null,
        string? summary = null,
        CancellationToken ct = default)
    {
        var ev = new AuditEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorUserId = current.UserId,
            ActorUsername = current.Username,
            ActorRole = current.Role,
            IpAddress = current.IpAddress,
            CorrelationId = current.CorrelationId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOpts),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOpts),
            Summary = summary
        };
        db.AuditEvents.Add(ev);
        await db.SaveChangesAsync(ct);
    }
}
