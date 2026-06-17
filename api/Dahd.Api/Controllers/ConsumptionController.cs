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
[Route("api/consumption")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class ConsumptionController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    /// <summary>
    /// Per warehouse × drug: lookback consumption (from dispense events
    /// attributed to the batch's current warehouse), daily velocity,
    /// projected need over forecast window, and shortfall vs current stock.
    /// Anchored in the verified RMSC mechanic: quarterly POs based on
    /// annual demand + consumption pattern.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<IReadOnlyList<ConsumptionForecastRow>>> Forecast(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int lookbackDays = 365,
        [FromQuery] int forecastDays = 90,
        [FromQuery] decimal safetyMultiplier = 1.15m,
        CancellationToken ct = default)
    {
        lookbackDays = Math.Clamp(lookbackDays, 7, 730);
        forecastDays = Math.Clamp(forecastDays, 7, 365);
        safetyMultiplier = Math.Clamp(safetyMultiplier, 1m, 3m);

        var since = DateTime.UtcNow.AddDays(-lookbackDays);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var consumption = await db.DispenseEvents.AsNoTracking()
            .Where(d => d.DispensedAt >= since)
            .Join(db.Batches.AsNoTracking(), d => d.BatchId, b => b.Id, (d, b) => new
            {
                b.CurrentWarehouseId,
                b.DrugId,
                d.Quantity
            })
            .GroupBy(x => new { x.CurrentWarehouseId, x.DrugId })
            .Select(g => new
            {
                g.Key.CurrentWarehouseId,
                g.Key.DrugId,
                Consumed = g.Sum(x => x.Quantity)
            })
            .ToListAsync(ct);

        if (consumption.Count == 0) return Ok(Array.Empty<ConsumptionForecastRow>());

        var stocks = await db.Batches.AsNoTracking()
            .Where(b => b.Status == BatchStatus.InStore && b.ExpiryDate >= today)
            .GroupBy(b => new { b.CurrentWarehouseId, b.DrugId })
            .Select(g => new
            {
                g.Key.CurrentWarehouseId,
                g.Key.DrugId,
                Qty = g.Sum(x => x.Quantity)
            })
            .ToListAsync(ct);
        var stockLookup = stocks.ToDictionary(
            s => (s.CurrentWarehouseId, s.DrugId), s => s.Qty);

        var warehouses = await db.Warehouses.AsNoTracking()
            .ToDictionaryAsync(w => w.Id, w => w, ct);

        var drugIds = consumption.Select(c => c.DrugId).Distinct().ToList();
        var drugs = await db.Drugs.AsNoTracking()
            .Where(d => drugIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d, ct);

        var rows = new List<ConsumptionForecastRow>(consumption.Count);
        foreach (var c in consumption)
        {
            if (!warehouses.TryGetValue(c.CurrentWarehouseId, out var wh)) continue;
            if (!drugs.TryGetValue(c.DrugId, out var drug)) continue;
            if (warehouseId.HasValue && wh.Id != warehouseId) continue;

            var velocity = c.Consumed / lookbackDays;
            var projected = Math.Round(velocity * forecastDays, 2);
            var safetyStock = Math.Round(projected * (safetyMultiplier - 1m), 2);
            var currentStock = stockLookup.GetValueOrDefault((wh.Id, drug.Id), 0m);
            var shortfall = Math.Max(0m, Math.Round(projected + safetyStock - currentStock, 2));

            rows.Add(new ConsumptionForecastRow(
                wh.Id, wh.Code, wh.Name,
                drug.Id, drug.Code, drug.Name, drug.UnitOfMeasure,
                lookbackDays, c.Consumed,
                Math.Round(velocity, 4),
                forecastDays, projected,
                currentStock, shortfall, safetyStock));
        }

        return Ok(rows
            .OrderByDescending(r => r.Shortfall)
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.DrugName)
            .ToList());
    }

    [HttpPost("draft-quarterly-indent")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<DraftQuarterlyIndentResponse>> DraftQuarterlyIndent(
        [FromBody] DraftQuarterlyIndentRequest req,
        CancellationToken ct)
    {
        if (req.RecipientWarehouseId == req.SourceWarehouseId)
            return BadRequest("Recipient and source warehouses must differ.");

        var forecast = await Forecast(
            req.RecipientWarehouseId, req.LookbackDays, req.ForecastDays, req.SafetyMultiplier, ct);
        if (forecast.Result is not OkObjectResult ok || ok.Value is not IReadOnlyList<ConsumptionForecastRow> rows)
            return Problem("Could not compute forecast for recipient.");

        var withShortfall = rows.Where(r => r.Shortfall > 0).ToList();
        if (withShortfall.Count == 0)
            return Ok(new DraftQuarterlyIndentResponse(null, null, 0, 0m));

        var indent = new Indent
        {
            IndentNumber = $"QTR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            RaisedByWarehouseId = req.RecipientWarehouseId,
            FulfilledByWarehouseId = req.SourceWarehouseId,
            Status = IndentStatus.Draft,
            Remarks = $"Consumption-driven quarterly indent (lookback {req.LookbackDays}d, forecast {req.ForecastDays}d, safety x{req.SafetyMultiplier:0.00})",
            Lines = withShortfall.Select(r => new IndentLine
            {
                DrugId = r.DrugId,
                RequestedQuantity = r.Shortfall,
                Remarks = $"velocity {r.DailyVelocity}/d × {r.ForecastDays}d = {r.ProjectedNeed}; current {r.CurrentStock}"
            }).ToList()
        };
        db.Indents.Add(indent);
        await db.SaveChangesAsync(ct);

        var totalQty = indent.Lines.Sum(l => l.RequestedQuantity);
        await audit.LogAsync(nameof(Indent), indent.Id, "QuarterlyDraft",
            after: new
            {
                indent.IndentNumber,
                req.LookbackDays,
                req.ForecastDays,
                req.SafetyMultiplier,
                LineCount = indent.Lines.Count,
                TotalQuantity = totalQty
            },
            summary: $"Quarterly Draft {indent.IndentNumber} ({indent.Lines.Count} lines, {totalQty} units) for recipient {req.RecipientWarehouseId}",
            ct: ct);

        return Ok(new DraftQuarterlyIndentResponse(indent.Id, indent.IndentNumber, indent.Lines.Count, totalQty));
    }
}
