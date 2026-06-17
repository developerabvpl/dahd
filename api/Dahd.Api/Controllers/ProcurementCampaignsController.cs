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
[Route("api/procurement-campaigns")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class ProcurementCampaignsController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProcurementCampaignDto>>> Get(
        [FromQuery] CampaignStatus? status,
        [FromQuery] int? upcomingDays,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = db.ProcurementCampaigns.AsNoTracking().Include(c => c.Drug).AsQueryable();
        if (status.HasValue) q = q.Where(c => c.Status == status);
        if (upcomingDays.HasValue)
        {
            var cutoff = today.AddDays(upcomingDays.Value);
            q = q.Where(c => c.WindowStart <= cutoff && c.WindowEnd >= today);
        }

        var rows = await q.OrderBy(c => c.WindowStart).ToListAsync(ct);
        return Ok(rows.Select(c => ToDto(c, today)).ToList());
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<ProcurementCampaignDto>>> Upcoming(
        [FromQuery] int take = 5, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await db.ProcurementCampaigns.AsNoTracking().Include(c => c.Drug)
            .Where(c => c.Status != CampaignStatus.Cancelled && c.WindowEnd >= today)
            .OrderBy(c => c.WindowStart)
            .Take(Math.Clamp(take, 1, 20))
            .ToListAsync(ct);
        return Ok(rows.Select(c => ToDto(c, today)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcurementCampaignDto>> GetById(Guid id, CancellationToken ct)
    {
        var c = await db.ProcurementCampaigns.Include(x => x.Drug).FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(ToDto(c, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<ProcurementCampaignDto>> Create(
        [FromBody] CreateCampaignRequest req, CancellationToken ct)
    {
        if (await db.ProcurementCampaigns.AnyAsync(c => c.Code == req.Code, ct))
            return Conflict($"Campaign '{req.Code}' already exists.");
        if (req.WindowEnd < req.WindowStart) return BadRequest("WindowEnd must be on or after WindowStart.");

        var drug = await db.Drugs.FindAsync([req.DrugId], ct);
        if (drug is null) return BadRequest($"Drug {req.DrugId} not found.");

        var c = new ProcurementCampaign
        {
            Code = req.Code,
            Name = req.Name,
            Scheme = req.Scheme,
            DrugId = req.DrugId,
            WindowStart = req.WindowStart,
            WindowEnd = req.WindowEnd,
            LeadDays = req.LeadDays,
            TargetDoseCount = req.TargetDoseCount,
            TargetCohortDescription = req.TargetCohortDescription,
            Notes = req.Notes,
            Status = CampaignStatus.Planned
        };
        db.ProcurementCampaigns.Add(c);
        await db.SaveChangesAsync(ct);
        c.Drug = drug;

        await audit.LogAsync(nameof(ProcurementCampaign), c.Id, "Create",
            after: new { c.Code, c.Scheme, c.WindowStart, c.WindowEnd, c.TargetDoseCount },
            summary: $"Campaign {c.Code} created for {drug.Code}", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, ToDto(c, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:guid}/draft-indents")]
    [Authorize(Roles = AppRoles.ApproveIndents)]
    public async Task<ActionResult<DraftCampaignIndentsResponse>> DraftIndents(
        Guid id,
        [FromBody] DraftCampaignIndentsRequest req,
        CancellationToken ct)
    {
        var c = await db.ProcurementCampaigns.Include(x => x.Drug).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        if (c.Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
            return Conflict($"Cannot draft indents for {c.Status} campaign.");

        var source = await db.Warehouses.FindAsync([req.SourceWarehouseId], ct);
        if (source is null) return BadRequest("Source warehouse not found.");
        if (req.QuantityPerDestination <= 0) return BadRequest("QuantityPerDestination must be > 0.");

        var destinations = await db.Warehouses
            .Where(w => w.Id != source.Id && w.IsActive
                        && (w.Type == WarehouseType.Divisional
                            || w.Type == WarehouseType.District
                            || w.Type == WarehouseType.Facility))
            .ToListAsync(ct);

        if (destinations.Count == 0) return BadRequest("No destination warehouses found.");

        var created = 0;
        var totalQty = 0m;
        var stamp = DateTime.UtcNow;
        foreach (var dest in destinations)
        {
            var indent = new Indent
            {
                IndentNumber = $"IND-{stamp:yyyyMMddHHmmss}-{created + 1:000}",
                RaisedByWarehouseId = dest.Id,
                FulfilledByWarehouseId = source.Id,
                Status = IndentStatus.Draft,
                Remarks = $"Auto-draft for campaign {c.Code}",
                Lines = new List<IndentLine>
                {
                    new()
                    {
                        DrugId = c.DrugId,
                        RequestedQuantity = req.QuantityPerDestination,
                        Remarks = c.Code
                    }
                }
            };
            db.Indents.Add(indent);
            created++;
            totalQty += req.QuantityPerDestination;
        }

        c.IndentsDraftedAt = stamp;
        c.IndentsDraftedCount += created;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(ProcurementCampaign), c.Id, "DraftIndents",
            after: new { IndentsCreated = created, TotalQuantity = totalQty, SourceWarehouseId = source.Id },
            summary: $"Drafted {created} indents for campaign {c.Code} (qty {totalQty} each from {source.Code})",
            ct: ct);

        return Ok(new DraftCampaignIndentsResponse(created, totalQty));
    }

    private static ProcurementCampaignDto ToDto(ProcurementCampaign c, DateOnly today)
    {
        var procurementStart = c.WindowStart.AddDays(-c.LeadDays);
        return new(
            c.Id, c.Code, c.Name, c.Scheme,
            c.DrugId, c.Drug.Code, c.Drug.Name,
            c.WindowStart, c.WindowEnd, c.LeadDays,
            c.TargetDoseCount, c.TargetCohortDescription,
            c.Status, c.Notes,
            c.IndentsDraftedAt, c.IndentsDraftedCount,
            c.WindowStart.DayNumber - today.DayNumber,
            procurementStart.DayNumber - today.DayNumber);
    }
}
