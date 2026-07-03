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
[Route("api/batches")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class BatchesController(DahdDbContext db, IAuditLogger audit, IStockLedger ledger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BatchDto>>> Get(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? drugId,
        [FromQuery] BatchStatus? status,
        [FromQuery] int? expiringWithinDays,
        CancellationToken ct)
    {
        var q = db.Batches.AsNoTracking().Include(b => b.Drug).Include(b => b.CurrentWarehouse).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(b => b.CurrentWarehouseId == warehouseId);
        if (drugId.HasValue) q = q.Where(b => b.DrugId == drugId);
        if (status.HasValue) q = q.Where(b => b.Status == status);

        if (expiringWithinDays.HasValue)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(expiringWithinDays.Value));
            q = q.Where(b => b.ExpiryDate <= cutoff);
        }

        var rows = await q.OrderBy(b => b.ExpiryDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(rows.Select(b => ToDto(b, today)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BatchDto>> GetById(Guid id, CancellationToken ct)
    {
        var b = await db.Batches.Include(x => x.Drug).Include(x => x.CurrentWarehouse).FirstOrDefaultAsync(x => x.Id == id, ct);
        return b is null ? NotFound() : Ok(ToDto(b, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<BatchDto>> Create([FromBody] CreateBatchRequest req, CancellationToken ct)
    {
        var drug = await db.Drugs.FindAsync([req.DrugId], ct);
        if (drug is null) return BadRequest($"Drug {req.DrugId} not found.");

        var wh = await db.Warehouses.FindAsync([req.CurrentWarehouseId], ct);
        if (wh is null) return BadRequest($"Warehouse {req.CurrentWarehouseId} not found.");

        if (drug.ColdChainRequired && !wh.ColdChainCapable)
            return BadRequest("Drug requires cold-chain but warehouse is not cold-chain capable.");

        if (req.ExpiryDate <= req.ManufactureDate)
            return BadRequest("ExpiryDate must be after ManufactureDate.");

        var batch = new Batch
        {
            DrugId = req.DrugId,
            BatchNumber = req.BatchNumber,
            ManufactureDate = req.ManufactureDate,
            ExpiryDate = req.ExpiryDate,
            Manufacturer = req.Manufacturer,
            Quantity = req.Quantity,
            UnitCost = req.UnitCost,
            CurrentWarehouseId = req.CurrentWarehouseId,
            Status = BatchStatus.InStore,
            PurchaseOrderRef = req.PurchaseOrderRef
        };
        db.Batches.Add(batch);
        ledger.Record(StockMovementType.Receipt, batch.DrugId, batch.CurrentWarehouseId,
            batch.Id, batch.BatchNumber, batch.Quantity,
            reference: req.PurchaseOrderRef, note: "Goods receipt");
        await db.SaveChangesAsync(ct);

        batch.Drug = drug;
        batch.CurrentWarehouse = wh;
        var dto = ToDto(batch, DateOnly.FromDateTime(DateTime.UtcNow));
        await audit.LogAsync(nameof(Batch), batch.Id, "Create", after: dto,
            summary: $"Batch {batch.BatchNumber} of {drug.Code} ({batch.Quantity} @ {wh.Code})", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, dto);
    }

    [HttpGet("stock-by-drug")]
    public async Task<ActionResult<IReadOnlyList<StockByDrugRow>>> StockByDrug(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? drugId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(30);

        var q = db.Batches.AsNoTracking()
            .Include(b => b.Drug)
            .Include(b => b.CurrentWarehouse)
            .Where(b => b.Status == BatchStatus.InStore);

        if (warehouseId.HasValue) q = q.Where(b => b.CurrentWarehouseId == warehouseId);
        if (drugId.HasValue) q = q.Where(b => b.DrugId == drugId);

        var rows = await q.ToListAsync(ct);

        var summary = rows
            .GroupBy(b => new
            {
                b.DrugId, b.Drug.Code, b.Drug.Name, b.Drug.UnitOfMeasure,
                WarehouseId = b.CurrentWarehouseId, WarehouseCode = b.CurrentWarehouse.Code, WarehouseName = b.CurrentWarehouse.Name
            })
            .Select(g => new StockByDrugRow(
                g.Key.DrugId, g.Key.Code, g.Key.Name, g.Key.UnitOfMeasure,
                g.Key.WarehouseId, g.Key.WarehouseCode, g.Key.WarehouseName,
                g.Sum(x => x.Quantity), g.Count(),
                g.Count(x => x.ExpiryDate < today),
                g.Count(x => x.ExpiryDate >= today && x.ExpiryDate <= soon)))
            .OrderBy(r => r.DrugName).ThenBy(r => r.WarehouseName)
            .ToList();

        return Ok(summary);
    }

    /// <summary>
    /// Append-only stock ledger. Running balance is cumulated per (drug, warehouse)
    /// in chronological order across the FULL history, then the requested slice is
    /// returned newest-first. Requires a drug or warehouse filter to stay bounded.
    /// </summary>
    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<StockLedgerRow>>> Ledger(
        [FromQuery] Guid? drugId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? batchId,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
    {
        if (drugId is null && warehouseId is null && batchId is null)
            return BadRequest("Provide at least one of drugId, warehouseId or batchId.");

        var q = db.StockMovements.AsNoTracking()
            .Include(m => m.Drug).Include(m => m.Warehouse).AsQueryable();
        if (drugId.HasValue) q = q.Where(m => m.DrugId == drugId);
        if (warehouseId.HasValue) q = q.Where(m => m.WarehouseId == warehouseId);
        if (batchId.HasValue) q = q.Where(m => m.BatchId == batchId);

        var all = await q.OrderBy(m => m.OccurredAt).ThenBy(m => m.CreatedAt).ToListAsync(ct);

        // running balance per (drug, warehouse)
        var running = new Dictionary<(Guid, Guid), decimal>();
        var rows = new List<StockLedgerRow>(all.Count);
        foreach (var m in all)
        {
            var key = (m.DrugId, m.WarehouseId);
            var bal = running.GetValueOrDefault(key, 0m) + m.QuantityDelta;
            running[key] = bal;
            rows.Add(new StockLedgerRow(
                m.Id, m.OccurredAt, m.Type,
                m.DrugId, m.Drug.Code, m.Drug.Name,
                m.WarehouseId, m.Warehouse.Code,
                m.BatchNumber, m.QuantityDelta, bal,
                m.Reference, m.Note, m.ActorUsername));
        }

        rows.Reverse(); // newest first for display
        return Ok(rows.Take(Math.Clamp(take, 1, 1000)).ToList());
    }

    private static BatchDto ToDto(Batch b, DateOnly today) => new(
        b.Id, b.DrugId, b.Drug.Name, b.BatchNumber,
        b.ManufactureDate, b.ExpiryDate, b.Manufacturer,
        b.Quantity, b.UnitCost, b.CurrentWarehouseId, b.CurrentWarehouse.Name,
        b.Status, b.ExpiryDate.DayNumber - today.DayNumber);
}
