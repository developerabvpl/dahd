using System.Text.Json;
using Dahd.Application;
using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/coldchain")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class ColdChainController(DahdDbContext db, IAuditLogger audit, ICurrentUser current) : ControllerBase
{
    private const decimal MinAllowed = 2m;
    private const decimal MaxAllowed = 8m;

    [HttpGet("logs")]
    public async Task<ActionResult<IReadOnlyList<ColdChainLogDto>>> Get(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool breachesOnly = false,
        [FromQuery] bool unacknowledgedOnly = false,
        [FromQuery] int hours = 24,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Abs(hours));
        var q = db.ColdChainLogs.AsNoTracking().Include(c => c.Warehouse).Where(c => c.ReadingAt >= since);
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId);
        if (breachesOnly) q = q.Where(c => c.IsBreach);
        if (unacknowledgedOnly) q = q.Where(c => c.IsBreach && c.AcknowledgedAt == null);

        var rows = await q.OrderByDescending(c => c.ReadingAt).Take(500).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpPost("logs")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<ColdChainLogDto>> CreateLog([FromBody] CreateColdChainLogRequest req, CancellationToken ct)
    {
        var wh = await db.Warehouses.FindAsync([req.WarehouseId], ct);
        if (wh is null) return BadRequest($"Warehouse {req.WarehouseId} not found.");

        var log = new ColdChainLog
        {
            WarehouseId = req.WarehouseId,
            DeviceId = req.DeviceId,
            DeviceName = req.DeviceName,
            ReadingAt = req.ReadingAt,
            TemperatureCelsius = req.TemperatureCelsius,
            IsBreach = req.TemperatureCelsius < MinAllowed || req.TemperatureCelsius > MaxAllowed,
            Remarks = req.Remarks,
            RecordedBy = current.Username
        };
        db.ColdChainLogs.Add(log);
        await db.SaveChangesAsync(ct);
        log.Warehouse = wh;
        if (log.IsBreach)
        {
            await audit.LogAsync(nameof(ColdChainLog), log.Id, "Breach",
                after: new { log.TemperatureCelsius, log.DeviceId, log.WarehouseId },
                summary: $"Cold-chain breach at {wh.Code} device {log.DeviceId}: {log.TemperatureCelsius} °C",
                ct: ct);
        }
        return Ok(ToDto(log));
    }

    [HttpPost("logs/{id:guid}/acknowledge")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<ColdChainLogDto>> Acknowledge(
        Guid id,
        [FromBody] AcknowledgeBreachRequest req,
        CancellationToken ct)
    {
        var log = await db.ColdChainLogs.Include(c => c.Warehouse).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (log is null) return NotFound();
        if (!log.IsBreach) return BadRequest("Cannot acknowledge a non-breach reading.");
        if (log.AcknowledgedAt is not null) return Conflict("Breach already acknowledged.");
        if (string.IsNullOrWhiteSpace(req.CorrectiveAction)) return BadRequest("Corrective action is required.");

        log.AcknowledgedAt = DateTime.UtcNow;
        log.AcknowledgedBy = current.Username;
        log.CorrectiveAction = req.CorrectiveAction;
        log.AffectedBatchIdsJson = req.AffectedBatchIds is { Count: > 0 }
            ? JsonSerializer.Serialize(req.AffectedBatchIds)
            : null;

        if (req.AffectedBatchIds is { Count: > 0 })
        {
            var affected = await db.Batches.Where(b => req.AffectedBatchIds.Contains(b.Id)).ToListAsync(ct);
            foreach (var b in affected) b.Status = BatchStatus.Recalled;
        }

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(ColdChainLog), log.Id, "BreachAcknowledged",
            after: new { log.AcknowledgedBy, log.CorrectiveAction, AffectedBatches = req.AffectedBatchIds?.Count ?? 0 },
            summary: $"Breach at {log.Warehouse.Code}/{log.DeviceId} acknowledged: {req.CorrectiveAction}",
            ct: ct);
        return Ok(ToDto(log));
    }

    [HttpGet("rollup/daily")]
    public async Task<ActionResult<IReadOnlyList<ColdChainDailyRollupRow>>> DailyRollup(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days)).Date;
        var q = db.ColdChainLogs.AsNoTracking().Include(c => c.Warehouse).Where(c => c.ReadingAt >= since);
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId);

        var rows = await q.ToListAsync(ct);
        var rollup = rows
            .GroupBy(c => new { c.WarehouseId, c.Warehouse.Name, c.DeviceId, c.DeviceName, Date = DateOnly.FromDateTime(c.ReadingAt) })
            .Select(g => new ColdChainDailyRollupRow(
                g.Key.WarehouseId, g.Key.Name, g.Key.DeviceId, g.Key.DeviceName, g.Key.Date,
                g.Count(),
                g.Count(x => x.IsBreach),
                g.Min(x => x.TemperatureCelsius),
                g.Max(x => x.TemperatureCelsius),
                Math.Round(g.Average(x => x.TemperatureCelsius), 2)))
            .OrderByDescending(r => r.Date).ThenBy(r => r.WarehouseName).ThenBy(r => r.DeviceId)
            .ToList();

        return Ok(rollup);
    }

    private static ColdChainLogDto ToDto(ColdChainLog c) => new(
        c.Id, c.WarehouseId, c.Warehouse.Name,
        c.DeviceId, c.DeviceName, c.ReadingAt,
        c.TemperatureCelsius, c.IsBreach, c.Remarks,
        c.AcknowledgedAt, c.AcknowledgedBy, c.CorrectiveAction, c.AffectedBatchIdsJson);
}
