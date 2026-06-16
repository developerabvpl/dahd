using Dahd.Domain.Common;

namespace Dahd.Domain.Entities;

public class AuditEvent : Entity
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public string? ActorRole { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Summary { get; set; }
}
