namespace Dahd.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        object? before = null,
        object? after = null,
        string? summary = null,
        CancellationToken ct = default);
}
