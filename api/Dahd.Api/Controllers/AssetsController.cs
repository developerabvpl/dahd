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
[Route("api/assets")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class AssetsController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> Get(
        [FromQuery] AssetStatus? status,
        [FromQuery] AssetCategory? category,
        CancellationToken ct)
    {
        var q = AssetQuery();
        if (status.HasValue) q = q.Where(a => a.Status == status);
        if (category.HasValue) q = q.Where(a => a.Category == category);
        var rows = await q.OrderBy(a => a.AssetTag).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(rows.Select(a => ToDto(a, today)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDto>> GetById(Guid id, CancellationToken ct)
    {
        var a = await AssetQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? NotFound() : Ok(ToDto(a, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<AssetKpiDto>> Kpis(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(60);
        var dto = new AssetKpiDto(
            TotalAssets: await db.Assets.CountAsync(ct),
            ActiveAssets: await db.Assets.CountAsync(a => a.Status == AssetStatus.Active, ct),
            UnderMaintenance: await db.Assets.CountAsync(a => a.Status == AssetStatus.UnderMaintenance, ct),
            InBreakdown: await db.Assets.CountAsync(a => a.Status == AssetStatus.BreakdownReported, ct),
            Condemned: await db.Assets.CountAsync(a => a.Status == AssetStatus.Condemned, ct),
            OpenJobs: await db.MaintenanceJobs.CountAsync(j => j.Status == MaintenanceJobStatus.Open || j.Status == MaintenanceJobStatus.InProgress, ct),
            OverduePpm: await db.MaintenanceSchedules.CountAsync(s => s.IsActive && s.NextDueDate < today, ct),
            AmcExpiring60Days: await db.AmcContracts.CountAsync(c => c.Status == AmcStatus.Active && c.EndDate >= today && c.EndDate <= soon, ct));
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetRequest req, CancellationToken ct)
    {
        if (await db.Assets.AnyAsync(a => a.AssetTag == req.AssetTag, ct))
            return Conflict($"Asset tag '{req.AssetTag}' already exists.");

        var asset = new Asset
        {
            AssetTag = req.AssetTag,
            Name = req.Name,
            Category = req.Category,
            Model = req.Model,
            SerialNumber = req.SerialNumber,
            Manufacturer = req.Manufacturer,
            WarehouseId = req.WarehouseId,
            FacilityId = req.FacilityId,
            LocationNote = req.LocationNote,
            PurchaseDate = req.PurchaseDate,
            PurchaseCost = req.PurchaseCost,
            WarrantyUntil = req.WarrantyUntil,
            Status = AssetStatus.Active,
            Condition = req.Condition,
            Notes = req.Notes
        };
        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(Asset), asset.Id, "Create",
            after: new { asset.AssetTag, asset.Name, asset.Category },
            summary: $"Asset {asset.AssetTag} created", ct: ct);

        var reloaded = await AssetQuery().FirstAsync(x => x.Id == asset.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, ToDto(reloaded, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<AssetDto>> UpdateStatus(Guid id, [FromBody] UpdateAssetStatusRequest req, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();
        var from = asset.Status;
        asset.Status = req.Status;
        if (req.Condition.HasValue) asset.Condition = req.Condition.Value;
        if (req.Notes is not null) asset.Notes = req.Notes;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(Asset), id, $"Status:{from}->{req.Status}",
            summary: $"Asset {asset.AssetTag} status {from} -> {req.Status}", ct: ct);
        var reloaded = await AssetQuery().FirstAsync(x => x.Id == id, ct);
        return Ok(ToDto(reloaded, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:guid}/schedules")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<AssetDto>> AddSchedule(Guid id, [FromBody] CreateScheduleRequest req, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();
        if (req.FrequencyDays <= 0) return BadRequest("FrequencyDays must be positive.");

        var last = req.LastServiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            AssetId = id,
            TaskDescription = req.TaskDescription,
            FrequencyDays = req.FrequencyDays,
            LastServiceDate = req.LastServiceDate,
            NextDueDate = last.AddDays(req.FrequencyDays),
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(MaintenanceSchedule), id, "AddSchedule",
            summary: $"PPM schedule added to {asset.AssetTag}: {req.TaskDescription}", ct: ct);
        var reloaded = await AssetQuery().FirstAsync(x => x.Id == id, ct);
        return Ok(ToDto(reloaded, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:guid}/amc")]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<AssetDto>> AddAmc(Guid id, [FromBody] CreateAmcRequest req, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();
        if (await db.AmcContracts.AnyAsync(c => c.ContractNumber == req.ContractNumber, ct))
            return Conflict($"AMC contract '{req.ContractNumber}' already exists.");
        if (req.EndDate < req.StartDate) return BadRequest("EndDate must be on or after StartDate.");

        db.AmcContracts.Add(new AmcContract
        {
            AssetId = id,
            ContractNumber = req.ContractNumber,
            VendorName = req.VendorName,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            AnnualCost = req.AnnualCost,
            Coverage = req.Coverage,
            Status = AmcStatus.Active
        });
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(AmcContract), id, "AddAmc",
            summary: $"AMC {req.ContractNumber} ({req.VendorName}) added to {asset.AssetTag}", ct: ct);
        var reloaded = await AssetQuery().FirstAsync(x => x.Id == id, ct);
        return Ok(ToDto(reloaded, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    private IQueryable<Asset> AssetQuery() => db.Assets.AsNoTracking()
        .Include(a => a.Warehouse).Include(a => a.Facility)
        .Include(a => a.Schedules).Include(a => a.Jobs).Include(a => a.AmcContracts);

    internal static AssetDto ToDto(Asset a, DateOnly today)
    {
        return new AssetDto(
            a.Id, a.AssetTag, a.Name, a.Category,
            a.Model, a.SerialNumber, a.Manufacturer,
            a.WarehouseId, a.Warehouse?.Name,
            a.FacilityId, a.Facility?.Name, a.LocationNote,
            a.PurchaseDate, a.PurchaseCost, a.WarrantyUntil,
            a.Status, a.Condition, a.Notes,
            a.Jobs.Count(j => j.Status == MaintenanceJobStatus.Open || j.Status == MaintenanceJobStatus.InProgress),
            a.Schedules.Count(s => s.IsActive && s.NextDueDate < today),
            a.Schedules.OrderBy(s => s.NextDueDate).Select(s => new AssetScheduleDto(
                s.Id, s.TaskDescription, s.FrequencyDays, s.LastServiceDate, s.NextDueDate, s.IsActive,
                s.NextDueDate.DayNumber - today.DayNumber)).ToList(),
            a.Jobs.OrderByDescending(j => j.ReportedAt).Select(j => new AssetJobDto(
                j.Id, j.JobNumber, j.Type, j.Status, j.ReportedAt, j.ReportedBy, j.Description,
                j.AssignedTo, j.StartedAt, j.CompletedAt, j.Resolution, j.Cost)).ToList(),
            a.AmcContracts.OrderByDescending(c => c.EndDate).Select(c => new AssetAmcDto(
                c.Id, c.ContractNumber, c.VendorName, c.StartDate, c.EndDate, c.AnnualCost, c.Coverage, c.Status,
                c.EndDate.DayNumber - today.DayNumber)).ToList());
    }
}
