using Dahd.Application;
using Dahd.Infrastructure.Persistence;
using Dahd.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/coldchain/analytics")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class ColdChainAnalyticsController(DahdDbContext db) : ControllerBase
{
    private const decimal MinAllowed = 2m;
    private const decimal MaxAllowed = 8m;

    // Haynes / Arrhenius MKT — vaccines use ΔH ≈ 83.144 kJ/mol
    private const double ActivationEnergyJ = 83_144d;
    private const double GasConstant = 8.314d;

    [HttpGet("by-device")]
    public async Task<ActionResult<IReadOnlyList<DeviceAnalyticsRow>>> ByDevice(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
        var q = db.ColdChainLogs.AsNoTracking().Include(c => c.Warehouse)
            .Where(c => c.ReadingAt >= since);
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId);

        var rows = await q.ToListAsync(ct);
        if (rows.Count == 0) return Ok(Array.Empty<DeviceAnalyticsRow>());

        var grouped = rows
            .GroupBy(c => new { c.WarehouseId, WhCode = c.Warehouse.Code, WhName = c.Warehouse.Name, c.DeviceId, c.DeviceName })
            .Select(g =>
            {
                var temps = g.Select(x => (double)x.TemperatureCelsius).ToList();
                var min = (decimal)temps.Min();
                var max = (decimal)temps.Max();
                var mean = Math.Round((decimal)temps.Average(), 2);
                var breachCount = g.Count(x => x.IsBreach);
                var oos = g.Count() == 0 ? 0m : Math.Round((decimal)breachCount * 100m / g.Count(), 2);
                var mkt = (decimal)ComputeMkt(temps);

                return new DeviceAnalyticsRow(
                    g.Key.WarehouseId, g.Key.WhCode, g.Key.WhName,
                    g.Key.DeviceId, g.Key.DeviceName,
                    g.Count(), breachCount,
                    min, max, mean, Math.Round(mkt, 2),
                    oos,
                    g.Min(x => x.ReadingAt), g.Max(x => x.ReadingAt));
            })
            .OrderByDescending(r => r.TimeOutOfSpecPct)
            .ThenByDescending(r => r.BreachCount)
            .ToList();

        return Ok(grouped);
    }

    /// <summary>
    /// Returns breach counts bucketed by ISO day-of-week (1=Mon..7=Sun) and hour (0..23).
    /// Powers a 7×24 heatmap.
    /// </summary>
    [HttpGet("breach-heatmap")]
    public async Task<ActionResult<IReadOnlyList<BreachHourMatrixCell>>> BreachHeatmap(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
        var q = db.ColdChainLogs.AsNoTracking()
            .Where(c => c.ReadingAt >= since && c.IsBreach);
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId);

        var rows = await q.Select(c => c.ReadingAt).ToListAsync(ct);

        var matrix = rows
            .GroupBy(t => new { Dow = (int)t.DayOfWeek == 0 ? 7 : (int)t.DayOfWeek, t.Hour })
            .Select(g => new BreachHourMatrixCell(g.Key.Dow, g.Key.Hour, g.Count()))
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Hour)
            .ToList();

        return Ok(matrix);
    }

    private static double ComputeMkt(IReadOnlyList<double> tempsCelsius)
    {
        if (tempsCelsius.Count == 0) return 0;
        // Haynes equation: MKT = -E/R / ln( (1/n) Σ exp(-E / (R · T_i)) )
        // T_i must be in Kelvin.
        var sumExp = 0d;
        var n = 0;
        foreach (var t in tempsCelsius)
        {
            var kelvin = t + 273.15d;
            if (kelvin <= 0) continue;
            sumExp += Math.Exp(-ActivationEnergyJ / (GasConstant * kelvin));
            n++;
        }
        if (n == 0 || sumExp <= 0) return 0;
        var mean = sumExp / n;
        var mktK = -ActivationEnergyJ / (GasConstant * Math.Log(mean));
        return mktK - 273.15d;
    }
}
