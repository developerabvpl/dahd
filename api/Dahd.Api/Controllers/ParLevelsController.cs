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
[Route("api/par-levels")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class ParLevelsController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ParLevelRow>>> Get(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool belowParOnly = false,
        CancellationToken ct = default)
    {
        var q = db.ParLevels.AsNoTracking()
            .Include(p => p.Warehouse).Include(p => p.Drug)
            .Where(p => p.IsActive);
        if (warehouseId.HasValue) q = q.Where(p => p.WarehouseId == warehouseId);

        var pars = await q.ToListAsync(ct);
        var stock = await CurrentStockMap(pars, ct);

        var rows = pars.Select(p =>
        {
            var cur = stock.GetValueOrDefault((p.DrugId, p.WarehouseId), 0m);
            var shortfall = Math.Max(0, (p.ReorderToQuantity ?? p.ParQuantity) - cur);
            return new ParLevelRow(
                p.Id, p.WarehouseId, p.Warehouse.Code, p.Warehouse.Name,
                p.DrugId, p.Drug.Code, p.Drug.Name, p.Drug.UnitOfMeasure,
                p.ParQuantity, p.ReorderToQuantity, cur, shortfall, cur < p.ParQuantity, p.IsActive);
        });

        if (belowParOnly) rows = rows.Where(r => r.BelowPar);
        return Ok(rows.OrderBy(r => r.BelowPar ? 0 : 1).ThenBy(r => r.DrugName).ToList());
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<ParLevelRow>> Upsert([FromBody] UpsertParLevelRequest req, CancellationToken ct)
    {
        if (req.ParQuantity < 0) return BadRequest("ParQuantity cannot be negative.");
        var drug = await db.Drugs.FindAsync([req.DrugId], ct);
        if (drug is null) return BadRequest("Drug not found.");
        var wh = await db.Warehouses.FindAsync([req.WarehouseId], ct);
        if (wh is null) return BadRequest("Warehouse not found.");

        var existing = await db.ParLevels.FirstOrDefaultAsync(p => p.WarehouseId == req.WarehouseId && p.DrugId == req.DrugId, ct);
        if (existing is null)
        {
            existing = new ParLevel { WarehouseId = req.WarehouseId, DrugId = req.DrugId };
            db.ParLevels.Add(existing);
        }
        existing.ParQuantity = req.ParQuantity;
        existing.ReorderToQuantity = req.ReorderToQuantity;
        existing.IsActive = true;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(ParLevel), existing.Id, "Upsert",
            after: new { Warehouse = wh.Code, Drug = drug.Code, req.ParQuantity, req.ReorderToQuantity },
            summary: $"Par level for {drug.Code} @ {wh.Code} set to {req.ParQuantity}", ct: ct);

        var cur = (await CurrentStockMap([existing], ct)).GetValueOrDefault((existing.DrugId, existing.WarehouseId), 0m);
        var shortfall = Math.Max(0, (existing.ReorderToQuantity ?? existing.ParQuantity) - cur);
        return Ok(new ParLevelRow(
            existing.Id, wh.Id, wh.Code, wh.Name, drug.Id, drug.Code, drug.Name, drug.UnitOfMeasure,
            existing.ParQuantity, existing.ReorderToQuantity, cur, shortfall, cur < existing.ParQuantity, true));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var p = await db.ParLevels.FindAsync([id], ct);
        if (p is null) return NotFound();
        p.IsActive = false;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(ParLevel), id, "Deactivate", summary: "Par level deactivated", ct: ct);
        return NoContent();
    }

    /// <summary>
    /// Create one Draft indent (recipient ← source) with a line per below-par
    /// drug at the recipient, qty = (ReorderTo ?? Par) − current stock.
    /// </summary>
    [HttpPost("auto-indent")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<ParAutoIndentResponse>> AutoIndent([FromBody] ParAutoIndentRequest req, CancellationToken ct)
    {
        if (req.RecipientWarehouseId == req.SourceWarehouseId) return BadRequest("Recipient and source must differ.");
        var recipient = await db.Warehouses.FindAsync([req.RecipientWarehouseId], ct);
        if (recipient is null) return BadRequest("Recipient warehouse not found.");
        var source = await db.Warehouses.FindAsync([req.SourceWarehouseId], ct);
        if (source is null) return BadRequest("Source warehouse not found.");

        var pars = await db.ParLevels.AsNoTracking().Include(p => p.Drug)
            .Where(p => p.IsActive && p.WarehouseId == req.RecipientWarehouseId).ToListAsync(ct);
        var stock = await CurrentStockMap(pars, ct);

        var lines = new List<IndentLine>();
        decimal total = 0;
        foreach (var p in pars)
        {
            var cur = stock.GetValueOrDefault((p.DrugId, p.WarehouseId), 0m);
            if (cur >= p.ParQuantity) continue; // not below par
            var qty = (p.ReorderToQuantity ?? p.ParQuantity) - cur;
            if (qty <= 0) continue;
            lines.Add(new IndentLine { DrugId = p.DrugId, RequestedQuantity = qty, Remarks = $"Auto-reorder (par {p.ParQuantity})" });
            total += qty;
        }

        if (lines.Count == 0) return Ok(new ParAutoIndentResponse(null, null, 0, 0));

        var indent = new Indent
        {
            IndentNumber = $"PAR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            RaisedByWarehouseId = recipient.Id,
            FulfilledByWarehouseId = source.Id,
            Status = IndentStatus.Draft,
            Remarks = $"Auto-indent for below-par items at {recipient.Code}",
            Lines = lines
        };
        db.Indents.Add(indent);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), indent.Id, "ParAutoIndent",
            after: new { indent.IndentNumber, LineCount = lines.Count, Total = total },
            summary: $"Par auto-indent {indent.IndentNumber}: {lines.Count} below-par lines from {source.Code}", ct: ct);
        return Ok(new ParAutoIndentResponse(indent.Id, indent.IndentNumber, lines.Count, total));
    }

    private async Task<Dictionary<(Guid DrugId, Guid WarehouseId), decimal>> CurrentStockMap(
        IReadOnlyCollection<ParLevel> pars, CancellationToken ct)
    {
        if (pars.Count == 0) return new();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var drugIds = pars.Select(p => p.DrugId).Distinct().ToList();
        var whIds = pars.Select(p => p.WarehouseId).Distinct().ToList();
        var stock = await db.Batches.AsNoTracking()
            .Where(b => b.Status == BatchStatus.InStore && b.ExpiryDate >= today
                        && drugIds.Contains(b.DrugId) && whIds.Contains(b.CurrentWarehouseId))
            .GroupBy(b => new { b.DrugId, b.CurrentWarehouseId })
            .Select(g => new { g.Key.DrugId, g.Key.CurrentWarehouseId, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        return stock.ToDictionary(s => (s.DrugId, s.CurrentWarehouseId), s => s.Qty);
    }
}
