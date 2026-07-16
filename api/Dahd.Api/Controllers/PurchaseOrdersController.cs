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
[Route("api/purchase-orders")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class PurchaseOrdersController(DahdDbContext db, IAuditLogger audit, IStockLedger ledger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> Get(
        [FromQuery] PoStatus? status, CancellationToken ct)
    {
        var q = PoQuery();
        if (status.HasValue) q = q.Where(p => p.Status == status);
        var rows = await q.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var p = await PoQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePoRequest req, CancellationToken ct)
    {
        if (req.Lines is null || req.Lines.Count == 0)
            return BadRequest("A purchase order needs at least one line.");
        if (req.VendorId is null && string.IsNullOrWhiteSpace(req.VendorName))
            return BadRequest("Pick an empanelled vendor or give a vendor name.");
        if (req.Lines.Any(l => l.OrderedQuantity <= 0 || l.UnitRate < 0))
            return BadRequest("Line quantities must be positive and rates non-negative.");

        var wh = await db.Warehouses.FindAsync([req.DestinationWarehouseId], ct);
        if (wh is null) return BadRequest("Destination warehouse not found.");

        Vendor? vendor = null;
        if (req.VendorId.HasValue)
        {
            vendor = await db.Vendors.FindAsync([req.VendorId.Value], ct);
            if (vendor is null) return BadRequest("Vendor not found.");
            if (vendor.Status == VendorStatus.Blacklisted) return BadRequest("Vendor is blacklisted.");
        }

        var po = new PurchaseOrder
        {
            PoNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            VendorId = req.VendorId,
            VendorName = vendor?.LegalName ?? req.VendorName,
            RateContractId = req.RateContractId,
            DestinationWarehouseId = req.DestinationWarehouseId,
            ExpectedDelivery = req.ExpectedDelivery,
            Remarks = req.Remarks,
            Status = PoStatus.Draft,
            Lines = req.Lines.Select(l => new PurchaseOrderLine
            {
                DrugId = l.DrugId,
                OrderedQuantity = l.OrderedQuantity,
                UnitRate = l.UnitRate,
                Remarks = l.Remarks
            }).ToList()
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(PurchaseOrder), po.Id, "Create",
            after: new { po.PoNumber, po.VendorName, LineCount = po.Lines.Count },
            summary: $"PO {po.PoNumber} drafted for {po.VendorName} ({po.Lines.Count} lines)", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = po.Id }, await Reload(po.Id, ct));
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = AppRoles.ApproveIndents)]
    public async Task<ActionResult<PurchaseOrderDto>> Issue(Guid id, CancellationToken ct)
    {
        var po = await Load(id, ct);
        if (po is null) return NotFound();
        if (po.Status != PoStatus.Draft) return Conflict($"PO is '{po.Status}', expected 'Draft'.");
        if (po.Lines.Count == 0) return BadRequest("Cannot issue an empty PO.");

        po.Status = PoStatus.Issued;
        po.IssuedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(PurchaseOrder), id, "Transition:Draft->Issued",
            summary: $"PO {po.PoNumber} issued to {po.VendorName}", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<PurchaseOrderDto>> Acknowledge(Guid id, CancellationToken ct)
    {
        var po = await Load(id, ct);
        if (po is null) return NotFound();
        if (po.Status != PoStatus.Issued) return Conflict($"PO is '{po.Status}', expected 'Issued'.");

        po.Status = PoStatus.Acknowledged;
        po.AcknowledgedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(PurchaseOrder), id, "Transition:Issued->Acknowledged",
            summary: $"PO {po.PoNumber} acknowledged by vendor", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.ApproveIndents)]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(Guid id, [FromBody] CancelPoRequest req, CancellationToken ct)
    {
        var po = await Load(id, ct);
        if (po is null) return NotFound();
        if (po.Status is PoStatus.Received or PoStatus.Cancelled)
            return Conflict($"PO is '{po.Status}' and cannot be cancelled.");
        if (po.Lines.Any(l => l.ReceivedQuantity > 0))
            return Conflict("PO has received quantities — close it via GRN instead of cancelling.");
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest("A cancellation reason is required.");

        var from = po.Status;
        po.Status = PoStatus.Cancelled;
        po.CancelledAt = DateTime.UtcNow;
        po.CancelReason = req.Reason.Trim();
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(PurchaseOrder), id, $"Transition:{from}->Cancelled",
            after: new { po.CancelReason },
            summary: $"PO {po.PoNumber} cancelled: {po.CancelReason}", ct: ct);
        return Ok(await Reload(id, ct));
    }

    /// <summary>
    /// Goods receipt against the PO: each line lands as a Batch at the receiving
    /// warehouse and writes a Receipt stock-movement referencing the PO number.
    /// Over-receipt beyond the ordered quantity is rejected. Partial receipts are
    /// fine — the PO moves to PartiallyReceived until every line is complete.
    /// </summary>
    [HttpPost("{id:guid}/grn")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<PurchaseOrderDto>> Grn(Guid id, [FromBody] GrnRequest req, CancellationToken ct)
    {
        var po = await Load(id, ct);
        if (po is null) return NotFound();
        if (po.Status is not (PoStatus.Issued or PoStatus.Acknowledged or PoStatus.PartiallyReceived))
            return Conflict($"PO is '{po.Status}' — GRN needs Issued/Acknowledged/PartiallyReceived.");
        if (req.Lines is null || req.Lines.Count == 0) return BadRequest("GRN needs at least one line.");

        var whId = req.WarehouseId ?? po.DestinationWarehouseId;
        var wh = await db.Warehouses.FindAsync([whId], ct);
        if (wh is null) return BadRequest("Receiving warehouse not found.");

        var lineMap = po.Lines.ToDictionary(l => l.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var g in req.Lines)
        {
            if (!lineMap.TryGetValue(g.LineId, out var line))
                return BadRequest($"Line {g.LineId} is not on this PO.");
            if (g.Quantity <= 0) return BadRequest("GRN quantities must be positive.");
            if (line.ReceivedQuantity + g.Quantity > line.OrderedQuantity)
                return BadRequest($"Line for drug {line.DrugId}: receiving {g.Quantity} would exceed ordered {line.OrderedQuantity} (already received {line.ReceivedQuantity}).");
            if (string.IsNullOrWhiteSpace(g.BatchNumber)) return BadRequest("Batch number is required per GRN line.");
            if (g.ExpiryDate <= g.ManufactureDate) return BadRequest("Expiry must be after manufacture date.");
            if (g.ExpiryDate <= today) return BadRequest($"Batch {g.BatchNumber} is already expired.");

            var drug = await db.Drugs.FindAsync([line.DrugId], ct);
            if (drug is null) return BadRequest("Drug not found.");
            if (drug.ColdChainRequired && !wh.ColdChainCapable)
                return BadRequest($"{drug.Code} requires cold chain but {wh.Code} is not cold-chain capable.");

            // Same drug + batch number at this warehouse tops up; otherwise a new batch.
            var existing = await db.Batches.FirstOrDefaultAsync(b =>
                b.DrugId == line.DrugId && b.BatchNumber == g.BatchNumber && b.CurrentWarehouseId == whId, ct);

            Guid batchId;
            if (existing is not null)
            {
                existing.Quantity += g.Quantity;
                batchId = existing.Id;
            }
            else
            {
                var batch = new Batch
                {
                    DrugId = line.DrugId,
                    BatchNumber = g.BatchNumber,
                    ManufactureDate = g.ManufactureDate,
                    ExpiryDate = g.ExpiryDate,
                    Manufacturer = g.Manufacturer ?? po.VendorName,
                    Quantity = g.Quantity,
                    UnitCost = line.UnitRate,
                    CurrentWarehouseId = whId,
                    Status = BatchStatus.InStore,
                    PurchaseOrderRef = po.PoNumber
                };
                db.Batches.Add(batch);
                batchId = batch.Id;
            }

            ledger.Record(StockMovementType.Receipt, line.DrugId, whId,
                batchId, g.BatchNumber, g.Quantity,
                reference: po.PoNumber, note: $"GRN from {po.VendorName}");

            line.ReceivedQuantity += g.Quantity;
        }

        var fullyReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        po.Status = fullyReceived ? PoStatus.Received : PoStatus.PartiallyReceived;
        if (fullyReceived) po.FullyReceivedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(PurchaseOrder), id, "GRN",
            after: new { po.PoNumber, Warehouse = wh.Code, LineCount = req.Lines.Count, po.Status },
            summary: $"GRN on {po.PoNumber}: {req.Lines.Count} line(s) into {wh.Code} — now {po.Status}", ct: ct);
        return Ok(await Reload(id, ct));
    }

    private Task<PurchaseOrder?> Load(Guid id, CancellationToken ct) =>
        db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id, ct);

    private IQueryable<PurchaseOrder> PoQuery() => db.PurchaseOrders.AsNoTracking()
        .Include(p => p.Vendor)
        .Include(p => p.RateContract)
        .Include(p => p.DestinationWarehouse)
        .Include(p => p.Lines).ThenInclude(l => l.Drug);

    private async Task<PurchaseOrderDto> Reload(Guid id, CancellationToken ct) =>
        ToDto(await PoQuery().FirstAsync(p => p.Id == id, ct));

    private static PurchaseOrderDto ToDto(PurchaseOrder p) => new(
        p.Id, p.PoNumber,
        p.VendorId, p.VendorName ?? p.Vendor?.LegalName,
        p.RateContractId, p.RateContract?.ContractNumber,
        p.DestinationWarehouseId, p.DestinationWarehouse.Name,
        p.Status, p.ExpectedDelivery,
        p.IssuedAt, p.AcknowledgedAt, p.FullyReceivedAt,
        p.CancelledAt, p.CancelReason, p.Remarks,
        p.Lines.Sum(l => l.OrderedQuantity * l.UnitRate),
        p.Lines.Select(l => new PoLineDto(
            l.Id, l.DrugId, l.Drug.Code, l.Drug.Name, l.Drug.UnitOfMeasure,
            l.OrderedQuantity, l.UnitRate, l.ReceivedQuantity,
            l.OrderedQuantity * l.UnitRate, l.Remarks)).ToList());
}
