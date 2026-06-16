using Dahd.Application;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class AuditController(DahdDbContext db) : ControllerBase
{
    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> Get(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] int days = 7,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var q = db.AuditEvents.AsNoTracking().Where(a => a.OccurredAt >= since);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (entityId.HasValue) q = q.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);

        var rows = await q.OrderByDescending(a => a.OccurredAt).Take(Math.Clamp(take, 1, 1000)).ToListAsync(ct);
        return Ok(rows.Select(a => new AuditEventDto(
            a.Id, a.OccurredAt, a.EntityType, a.EntityId, a.Action,
            a.ActorUserId, a.ActorUsername, a.ActorRole,
            a.IpAddress, a.CorrelationId, a.Summary,
            a.BeforeJson, a.AfterJson)).ToList());
    }
}
