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
public class ColdChainController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    private const decimal MinAllowed = 2m;
    private const decimal MaxAllowed = 8m;

    [HttpGet("logs")]
    public async Task<ActionResult<IReadOnlyList<ColdChainLogDto>>> Get(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool breachesOnly = false,
        [FromQuery] int hours = 24,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Abs(hours));
        var q = db.ColdChainLogs.AsNoTracking().Include(c => c.Warehouse).Where(c => c.ReadingAt >= since);
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId);
        if (breachesOnly) q = q.Where(c => c.IsBreach);

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
            Remarks = req.Remarks
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

    private static ColdChainLogDto ToDto(ColdChainLog c) => new(
        c.Id, c.WarehouseId, c.Warehouse.Name,
        c.DeviceId, c.DeviceName, c.ReadingAt,
        c.TemperatureCelsius, c.IsBreach, c.Remarks);
}
