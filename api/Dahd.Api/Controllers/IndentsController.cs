using Dahd.Application;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/indents")]
public class IndentsController(DahdDbContext db) : ControllerBase
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
        return CreatedAtAction(nameof(GetById), new { id = indent.Id }, await Reload(indent.Id, ct));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<IndentDto>> Submit(Guid id, CancellationToken ct)
        => await Transition(id, IndentStatus.Draft, IndentStatus.Submitted, i => i.SubmittedAt = DateTime.UtcNow, ct);

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<IndentDto>> Approve(Guid id, CancellationToken ct)
        => await Transition(id, IndentStatus.Submitted, IndentStatus.Approved, i => i.ApprovedAt = DateTime.UtcNow, ct);

    [HttpPost("{id:guid}/issue")]
    public async Task<ActionResult<IndentDto>> Issue(Guid id, CancellationToken ct)
        => await Transition(id, IndentStatus.Approved, IndentStatus.Issued, i => i.IssuedAt = DateTime.UtcNow, ct);

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<IndentDto>> Receive(Guid id, CancellationToken ct)
        => await Transition(id, IndentStatus.Issued, IndentStatus.Received, i => i.ReceivedAt = DateTime.UtcNow, ct);

    private async Task<ActionResult<IndentDto>> Transition(
        Guid id, IndentStatus from, IndentStatus to, Action<Indent> mutate, CancellationToken ct)
    {
        var i = await db.Indents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return NotFound();
        if (i.Status != from) return Conflict($"Indent status is '{i.Status}', expected '{from}'.");
        i.Status = to;
        mutate(i);
        await db.SaveChangesAsync(ct);
        return Ok(await Reload(id, ct));
    }

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
        i.Status, i.SubmittedAt, i.ApprovedAt, i.IssuedAt, i.ReceivedAt, i.Remarks,
        i.Lines.Select(l => new IndentLineDto(
            l.Id, l.DrugId, l.Drug.Code, l.Drug.Name,
            l.RequestedQuantity, l.ApprovedQuantity, l.IssuedQuantity,
            l.ReceivedQuantity, l.IssuedBatchId, l.Remarks)).ToList());
}
