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
[Route("api/rate-contracts")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class RateContractsController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RateContractDto>>> Get(
        [FromQuery] RateContractCategory? category,
        [FromQuery] RateContractStatus? status,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = db.RateContracts.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Drug)
            .Include(r => r.Items).ThenInclude(i => i.Vendor)
            .AsQueryable();
        if (category.HasValue) q = q.Where(r => r.Category == category);
        if (status.HasValue) q = q.Where(r => r.Status == status);

        var rows = await q.OrderByDescending(r => r.ValidFrom).ToListAsync(ct);
        return Ok(rows.Select(r => ToDto(r, today)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RateContractDto>> GetById(Guid id, CancellationToken ct)
    {
        var r = await db.RateContracts
            .Include(x => x.Items).ThenInclude(i => i.Drug)
            .Include(x => x.Items).ThenInclude(i => i.Vendor)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? NotFound() : Ok(ToDto(r, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<RateContractDto>> Create([FromBody] CreateRateContractRequest req, CancellationToken ct)
    {
        if (await db.RateContracts.AnyAsync(r => r.ContractNumber == req.ContractNumber, ct))
            return Conflict($"Rate-contract {req.ContractNumber} already exists.");
        if (req.ValidUntil < req.ValidFrom) return BadRequest("ValidUntil must be on or after ValidFrom.");

        var rc = new RateContract
        {
            ContractNumber = req.ContractNumber,
            Title = req.Title,
            Category = req.Category,
            LeadBody = string.IsNullOrWhiteSpace(req.LeadBody) ? "AHD" : req.LeadBody,
            ValidFrom = req.ValidFrom,
            ValidUntil = req.ValidUntil,
            Status = RateContractStatus.Active,
            SourceUrl = req.SourceUrl,
            Notes = req.Notes
        };
        db.RateContracts.Add(rc);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(RateContract), rc.Id, "Create",
            after: new { rc.ContractNumber, rc.Title, rc.Category, rc.ValidFrom, rc.ValidUntil },
            summary: $"Rate-contract {rc.ContractNumber} created", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = rc.Id }, ToDto(rc, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<RateContractItemDto>> AddItem(
        Guid id, [FromBody] AddRateContractItemRequest req, CancellationToken ct)
    {
        var rc = await db.RateContracts.FindAsync([id], ct);
        if (rc is null) return NotFound();
        if (rc.Status != RateContractStatus.Active && rc.Status != RateContractStatus.Draft)
            return Conflict("Can only add items to Draft or Active contracts.");

        var drug = await db.Drugs.FindAsync([req.DrugId], ct);
        if (drug is null) return BadRequest("Drug not found.");
        if (req.UnitRate <= 0) return BadRequest("UnitRate must be positive.");

        Vendor? vendor = null;
        if (req.VendorId.HasValue)
        {
            vendor = await db.Vendors.FindAsync([req.VendorId.Value], ct);
            if (vendor is null) return BadRequest("Vendor not found.");
        }

        var item = new RateContractItem
        {
            RateContractId = id,
            DrugId = req.DrugId,
            VendorId = req.VendorId,
            VendorName = req.VendorName ?? vendor?.LegalName,
            UnitRate = req.UnitRate,
            PackSize = req.PackSize,
            MinOrderQuantity = req.MinOrderQuantity,
            Remarks = req.Remarks
        };
        db.RateContractItems.Add(item);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(RateContractItem), item.Id, "Add",
            after: new { rc.ContractNumber, DrugCode = drug.Code, item.UnitRate, item.VendorName },
            summary: $"Added {drug.Code} @ {item.UnitRate} to {rc.ContractNumber}", ct: ct);

        item.Drug = drug;
        item.Vendor = vendor;
        return Ok(ToItemDto(item));
    }

    /// <summary>
    /// For an indent or planning view: best rate-contracted price per drug
    /// across Active contracts whose ValidUntil is in the future.
    /// </summary>
    [HttpGet("cheapest")]
    public async Task<ActionResult<IReadOnlyList<CheapestRateRow>>> Cheapest(
        [FromQuery] Guid? drugId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = db.RateContractItems.AsNoTracking()
            .Include(i => i.Drug)
            .Include(i => i.Vendor)
            .Include(i => i.RateContract)
            .Where(i => i.RateContract.Status == RateContractStatus.Active
                        && i.RateContract.ValidUntil >= today);
        if (drugId.HasValue) q = q.Where(i => i.DrugId == drugId);

        var items = await q.ToListAsync(ct);

        var cheapest = items
            .GroupBy(i => i.DrugId)
            .Select(g => g.OrderBy(x => x.UnitRate).First())
            .Select(i => new CheapestRateRow(
                i.DrugId, i.Drug.Code, i.Drug.Name,
                i.RateContractId, i.RateContract.ContractNumber, i.RateContract.Title,
                i.VendorId, i.VendorName ?? i.Vendor?.LegalName,
                i.UnitRate, i.PackSize, i.RateContract.ValidUntil))
            .OrderBy(r => r.DrugName)
            .ToList();

        return Ok(cheapest);
    }

    private static RateContractDto ToDto(RateContract r, DateOnly today) => new(
        r.Id, r.ContractNumber, r.Title, r.Category, r.LeadBody,
        r.ValidFrom, r.ValidUntil, r.Status, r.SourceUrl, r.Notes,
        r.Items.Count, r.ValidUntil.DayNumber - today.DayNumber,
        r.Items.Select(ToItemDto).ToList());

    private static RateContractItemDto ToItemDto(RateContractItem i) => new(
        i.Id, i.DrugId, i.Drug.Code, i.Drug.Name, i.Drug.UnitOfMeasure,
        i.VendorId, i.VendorName ?? i.Vendor?.LegalName,
        i.UnitRate, i.PackSize, i.MinOrderQuantity, i.Remarks);
}
