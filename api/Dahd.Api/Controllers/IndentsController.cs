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
[Route("api/indents")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class IndentsController(DahdDbContext db, IAuditLogger audit, IStockLedger ledger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IndentDto>>> Get(
        [FromQuery] IndentStatus? status,
        CancellationToken ct)
    {
        var q = db.Indents
            .AsNoTracking()
            .Include(i => i.RaisedByWarehouse)
            .Include(i => i.FulfilledByWarehouse)
            .Include(i => i.Lines).ThenInclude(l => l.Drug)
            .AsQueryable();

        if (status.HasValue) q = q.Where(i => i.Status == status);

        var rows = await q.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IndentDto>> GetById(Guid id, CancellationToken ct)
    {
        var i = await db.Indents
            .Include(x => x.RaisedByWarehouse)
            .Include(x => x.FulfilledByWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Drug)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return i is null ? NotFound() : Ok(ToDto(i));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Create([FromBody] CreateIndentRequest req, CancellationToken ct)
    {
        if (req.Lines is null || req.Lines.Count == 0)
            return BadRequest("Indent must have at least one line.");

        var indent = new Indent
        {
            IndentNumber = $"IND-{DateTime.UtcNow:yyyyMMddHHmmss}",
            RaisedByWarehouseId = req.RaisedByWarehouseId,
            FulfilledByWarehouseId = req.FulfilledByWarehouseId,
            Status = IndentStatus.Draft,
            Remarks = req.Remarks,
            Lines = req.Lines.Select(l => new IndentLine
            {
                DrugId = l.DrugId,
                RequestedQuantity = l.RequestedQuantity,
                Remarks = l.Remarks
            }).ToList()
        };
        db.Indents.Add(indent);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(Indent), indent.Id, "Create",
            after: new { indent.IndentNumber, LineCount = indent.Lines.Count },
            summary: $"Indent {indent.IndentNumber} drafted ({indent.Lines.Count} lines)", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = indent.Id }, await Reload(indent.Id, ct));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Update(Guid id, [FromBody] UpdateIndentRequest req, CancellationToken ct)
    {
        var indent = await db.Indents.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Draft)
            return Conflict($"Only a Draft indent can be edited (status is '{indent.Status}').");
        if (req.Lines is null || req.Lines.Count == 0)
            return BadRequest("Indent must have at least one line.");
        if (req.RaisedByWarehouseId == req.FulfilledByWarehouseId)
            return BadRequest("Raising and source warehouse must differ.");

        indent.RaisedByWarehouseId = req.RaisedByWarehouseId;
        indent.FulfilledByWarehouseId = req.FulfilledByWarehouseId;
        indent.Remarks = req.Remarks;

        db.IndentLines.RemoveRange(indent.Lines);
        indent.Lines = req.Lines.Select(l => new IndentLine
        {
            IndentId = indent.Id,
            DrugId = l.DrugId,
            RequestedQuantity = l.RequestedQuantity,
            Remarks = l.Remarks
        }).ToList();

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(Indent), id, "Update",
            after: new { LineCount = indent.Lines.Count },
            summary: $"Draft indent {indent.IndentNumber} edited ({indent.Lines.Count} lines)", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Submit(Guid id, CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Draft) return Conflict($"Indent status is '{indent.Status}', expected 'Draft'.");

        indent.Status = IndentStatus.Submitted;
        indent.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Draft->Submitted",
            summary: $"Indent {indent.IndentNumber} submitted", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Cancel(Guid id, CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Draft)
            return Conflict($"Only a Draft indent can be cancelled (status is '{indent.Status}').");

        indent.Status = IndentStatus.Cancelled;
        indent.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Draft->Cancelled",
            summary: $"Indent {indent.IndentNumber} cancelled", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = AppRoles.ApproveIndents)]
    public async Task<ActionResult<IndentDto>> Reject(
        Guid id,
        [FromBody] RejectIndentRequest req,
        CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Submitted)
            return Conflict($"Only a Submitted indent can be rejected (status is '{indent.Status}').");
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest("A rejection reason is required.");

        indent.Status = IndentStatus.Rejected;
        indent.RejectedAt = DateTime.UtcNow;
        indent.RejectionReason = req.Reason.Trim();
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Submitted->Rejected",
            after: new { indent.RejectionReason },
            summary: $"Indent {indent.IndentNumber} rejected: {indent.RejectionReason}", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = AppRoles.ApproveIndents)]
    public async Task<ActionResult<IndentDto>> Approve(
        Guid id,
        [FromBody] ApproveIndentRequest? req,
        CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Submitted) return Conflict($"Indent status is '{indent.Status}', expected 'Submitted'.");

        var overrides = req?.LineApprovals?.ToDictionary(x => x.LineId, x => x.ApprovedQuantity) ?? new();
        foreach (var line in indent.Lines)
        {
            line.ApprovedQuantity = overrides.TryGetValue(line.Id, out var qty) ? qty : line.RequestedQuantity;
        }

        indent.Status = IndentStatus.Approved;
        indent.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Submitted->Approved",
            after: indent.Lines.Select(l => new { l.Id, l.ApprovedQuantity }),
            summary: $"Indent {indent.IndentNumber} approved ({indent.Lines.Count} lines)", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Issue(Guid id, CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Approved) return Conflict($"Indent status is '{indent.Status}', expected 'Approved'.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sourceWarehouseId = indent.FulfilledByWarehouseId;

        foreach (var line in indent.Lines)
        {
            var qty = line.ApprovedQuantity ?? line.RequestedQuantity;
            if (qty <= 0) continue;

            var fefoBatch = await db.Batches
                .Where(b => b.DrugId == line.DrugId
                            && b.CurrentWarehouseId == sourceWarehouseId
                            && b.Status == BatchStatus.InStore
                            && b.ExpiryDate >= today
                            && b.Quantity >= qty)
                .OrderBy(b => b.ExpiryDate)
                .ThenBy(b => b.ManufactureDate)
                .FirstOrDefaultAsync(ct);

            if (fefoBatch is null)
                return BadRequest($"No in-stock batch at source warehouse with {qty} of drug {line.DrugId}.");

            fefoBatch.Quantity -= qty;
            line.IssuedBatchId = fefoBatch.Id;
            line.IssuedQuantity = qty;
            ledger.Record(StockMovementType.IssueOut, line.DrugId, sourceWarehouseId,
                fefoBatch.Id, fefoBatch.BatchNumber, -qty,
                reference: indent.IndentNumber, note: $"Issued to {indent.RaisedByWarehouseId}");
        }

        indent.Status = IndentStatus.Issued;
        indent.IssuedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Approved->Issued",
            after: indent.Lines.Select(l => new { l.Id, l.IssuedBatchId, l.IssuedQuantity }),
            summary: $"Indent {indent.IndentNumber} issued (FEFO allocated)", ct: ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<IndentDto>> Receive(Guid id, CancellationToken ct)
    {
        var indent = await Load(id, ct);
        if (indent is null) return NotFound();
        if (indent.Status != IndentStatus.Issued) return Conflict($"Indent status is '{indent.Status}', expected 'Issued'.");

        var destWarehouseId = indent.RaisedByWarehouseId;

        foreach (var line in indent.Lines)
        {
            if (line.IssuedBatchId is null || line.IssuedQuantity is null) continue;
            var qty = line.IssuedQuantity.Value;
            var sourceBatch = await db.Batches.AsNoTracking().FirstAsync(b => b.Id == line.IssuedBatchId, ct);

            var existing = await db.Batches.FirstOrDefaultAsync(b =>
                b.DrugId == sourceBatch.DrugId
                && b.BatchNumber == sourceBatch.BatchNumber
                && b.CurrentWarehouseId == destWarehouseId, ct);

            Guid destBatchId;
            if (existing is not null)
            {
                existing.Quantity += qty;
                destBatchId = existing.Id;
            }
            else
            {
                var newBatch = new Batch
                {
                    DrugId = sourceBatch.DrugId,
                    BatchNumber = sourceBatch.BatchNumber,
                    ManufactureDate = sourceBatch.ManufactureDate,
                    ExpiryDate = sourceBatch.ExpiryDate,
                    Manufacturer = sourceBatch.Manufacturer,
                    Quantity = qty,
                    UnitCost = sourceBatch.UnitCost,
                    CurrentWarehouseId = destWarehouseId,
                    Status = BatchStatus.InStore,
                    PurchaseOrderRef = sourceBatch.PurchaseOrderRef,
                    Remarks = $"Received from indent {indent.IndentNumber}"
                };
                db.Batches.Add(newBatch);
                destBatchId = newBatch.Id;
            }

            ledger.Record(StockMovementType.ReceiveIn, line.DrugId, destWarehouseId,
                destBatchId, sourceBatch.BatchNumber, qty,
                reference: indent.IndentNumber, note: "Received into store");
            line.ReceivedQuantity = qty;
        }

        indent.Status = IndentStatus.Received;
        indent.ReceivedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), id, "Transition:Issued->Received",
            after: indent.Lines.Select(l => new { l.Id, l.ReceivedQuantity }),
            summary: $"Indent {indent.IndentNumber} received at destination", ct: ct);
        return Ok(await Reload(id, ct));
    }

    private Task<Indent?> Load(Guid id, CancellationToken ct) =>
        db.Indents.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);

    private async Task<IndentDto> Reload(Guid id, CancellationToken ct)
    {
        var i = await db.Indents
            .Include(x => x.RaisedByWarehouse)
            .Include(x => x.FulfilledByWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Drug)
            .FirstAsync(x => x.Id == id, ct);
        return ToDto(i);
    }

    private static IndentDto ToDto(Indent i) => new(
        i.Id, i.IndentNumber,
        i.RaisedByWarehouseId, i.RaisedByWarehouse.Name,
        i.FulfilledByWarehouseId, i.FulfilledByWarehouse.Name,
        i.Status, i.SubmittedAt, i.ApprovedAt, i.IssuedAt, i.ReceivedAt,
        i.RejectedAt, i.CancelledAt, i.RejectionReason, i.Remarks,
        i.Lines.Select(l => new IndentLineDto(
            l.Id, l.DrugId, l.Drug.Code, l.Drug.Name,
            l.RequestedQuantity, l.ApprovedQuantity, l.IssuedQuantity,
            l.ReceivedQuantity, l.IssuedBatchId, l.Remarks)).ToList());
}
