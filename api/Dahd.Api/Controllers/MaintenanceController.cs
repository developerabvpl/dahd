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
[Route("api/maintenance")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class MaintenanceController(DahdDbContext db, IAuditLogger audit, ICurrentUser current) : ControllerBase
{
    /// <summary>PPM schedules due within the window (default 30 days), plus overdue.</summary>
    [HttpGet("due")]
    public async Task<ActionResult<IReadOnlyList<MaintenanceDueRow>>> Due(
        [FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(Math.Clamp(withinDays, 1, 365));

        var rows = await db.MaintenanceSchedules.AsNoTracking()
            .Include(s => s.Asset).ThenInclude(a => a.Warehouse)
            .Include(s => s.Asset).ThenInclude(a => a.Facility)
            .Where(s => s.IsActive && s.NextDueDate <= cutoff)
            .OrderBy(s => s.NextDueDate)
            .ToListAsync(ct);

        return Ok(rows.Select(s => new MaintenanceDueRow(
            s.AssetId, s.Asset.AssetTag, s.Asset.Name, s.Asset.Category,
            s.Id, s.TaskDescription, s.NextDueDate, s.NextDueDate.DayNumber - today.DayNumber,
            s.Asset.Warehouse != null ? s.Asset.Warehouse.Name : s.Asset.Facility != null ? s.Asset.Facility.Name : s.Asset.LocationNote)).ToList());
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyList<AssetJobDto>>> Jobs(
        [FromQuery] MaintenanceJobStatus? status,
        [FromQuery] MaintenanceJobType? type,
        CancellationToken ct = default)
    {
        var q = db.MaintenanceJobs.AsNoTracking().Include(j => j.Asset).AsQueryable();
        if (status.HasValue) q = q.Where(j => j.Status == status);
        if (type.HasValue) q = q.Where(j => j.Type == type);
        var rows = await q.OrderByDescending(j => j.ReportedAt).Take(500).ToListAsync(ct);
        return Ok(rows.Select(AssetsController.JobToDto).ToList());
    }

    [HttpPost("assets/{assetId:guid}/breakdown")]
    [Authorize(Roles = AppRoles.AnyAuthenticated)]
    public async Task<ActionResult<AssetJobDto>> LogBreakdown(Guid assetId, [FromBody] LogBreakdownRequest req, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Description)) return BadRequest("Description is required.");

        // ITIL triage: Impact × Urgency → Priority → SLA deadline. Impact defaults
        // from the asset's criticality (A→High, B→Medium, C→Low) when not supplied.
        var impact = req.Impact ?? asset.Criticality switch
        {
            AssetCriticality.A => IncidentImpact.High,
            AssetCriticality.C => IncidentImpact.Low,
            _ => IncidentImpact.Medium
        };
        var urgency = req.Urgency ?? IncidentUrgency.Medium;
        var priority = IncidentPolicy.Prioritise(impact, urgency);
        var reportedAt = DateTime.UtcNow;

        var job = new MaintenanceJob
        {
            AssetId = assetId,
            JobNumber = $"BRK-{reportedAt:yyyyMMddHHmmss}",
            Type = MaintenanceJobType.Breakdown,
            Status = MaintenanceJobStatus.Open,
            ReportedAt = reportedAt,
            ReportedBy = current.Username,
            Description = req.Description,
            AssignedTo = req.AssignedTo,
            Impact = impact,
            Urgency = urgency,
            Priority = priority,
            ProblemType = req.ProblemType,
            Deadline = IncidentPolicy.Deadline(reportedAt, priority)
        };
        db.MaintenanceJobs.Add(job);
        asset.Status = AssetStatus.BreakdownReported;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(MaintenanceJob), job.Id, "LogBreakdown",
            after: new { job.JobNumber, asset.AssetTag, req.Description, Priority = priority.ToString() },
            summary: $"Breakdown {job.JobNumber} ({priority}) logged for {asset.AssetTag}", ct: ct);
        return Ok(ToDto(job));
    }

    [HttpPost("assets/{assetId:guid}/ppm-job")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<AssetJobDto>> RaisePpmJob(Guid assetId, [FromBody] CreatePpmJobRequest req, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return NotFound();

        var job = new MaintenanceJob
        {
            AssetId = assetId,
            ScheduleId = req.ScheduleId,
            JobNumber = $"PPM-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Type = MaintenanceJobType.Preventive,
            Status = MaintenanceJobStatus.Open,
            ReportedAt = DateTime.UtcNow,
            ReportedBy = current.Username,
            Description = req.Description,
            AssignedTo = req.AssignedTo
        };
        db.MaintenanceJobs.Add(job);
        if (asset.Status == AssetStatus.Active) asset.Status = AssetStatus.UnderMaintenance;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(MaintenanceJob), job.Id, "RaisePpmJob",
            summary: $"PPM job {job.JobNumber} raised for {asset.AssetTag}", ct: ct);
        return Ok(ToDto(job));
    }

    [HttpPost("jobs/{jobId:guid}/start")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<AssetJobDto>> StartJob(Guid jobId, CancellationToken ct)
    {
        var job = await db.MaintenanceJobs.Include(j => j.Asset).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return NotFound();
        if (job.Status != MaintenanceJobStatus.Open) return Conflict($"Job is '{job.Status}', expected 'Open'.");
        job.Status = MaintenanceJobStatus.InProgress;
        job.StartedAt = DateTime.UtcNow;
        if (job.Asset.Status == AssetStatus.BreakdownReported || job.Asset.Status == AssetStatus.Active)
            job.Asset.Status = AssetStatus.UnderMaintenance;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(MaintenanceJob), jobId, "StartJob",
            summary: $"Job {job.JobNumber} started", ct: ct);
        return Ok(ToDto(job));
    }

    [HttpPost("jobs/{jobId:guid}/complete")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<AssetJobDto>> CompleteJob(Guid jobId, [FromBody] CompleteJobRequest req, CancellationToken ct)
    {
        var job = await db.MaintenanceJobs.Include(j => j.Asset).Include(j => j.Schedule).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return NotFound();
        if (job.Status == MaintenanceJobStatus.Completed || job.Status == MaintenanceJobStatus.Cancelled)
            return Conflict($"Job is already '{job.Status}'.");
        if (string.IsNullOrWhiteSpace(req.Resolution)) return BadRequest("Resolution is required.");

        job.Status = MaintenanceJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.Resolution = req.Resolution;
        job.Cost = req.Cost;
        job.Asset.Status = AssetStatus.Active;

        // Roll the linked PPM schedule forward
        if (job.Schedule is not null && job.Schedule.IsActive)
        {
            var done = DateOnly.FromDateTime(DateTime.UtcNow);
            job.Schedule.LastServiceDate = done;
            job.Schedule.NextDueDate = done.AddDays(job.Schedule.FrequencyDays);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(MaintenanceJob), jobId, "CompleteJob",
            after: new { job.JobNumber, job.Resolution, job.Cost },
            summary: $"Job {job.JobNumber} completed for {job.Asset.AssetTag}", ct: ct);
        return Ok(ToDto(job));
    }

    /// <summary>Assets whose calibration is due within the window (default 60 days), plus overdue.</summary>
    [HttpGet("calibration-due")]
    public async Task<ActionResult<IReadOnlyList<CalibrationDueRow>>> CalibrationDue(
        [FromQuery] int withinDays = 60, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(Math.Clamp(withinDays, 1, 365));

        var rows = await db.Assets.AsNoTracking()
            .Include(a => a.Warehouse).Include(a => a.Facility)
            .Where(a => a.CalibrationDueDate != null && a.CalibrationDueDate <= cutoff)
            .OrderBy(a => a.CalibrationDueDate)
            .ToListAsync(ct);

        return Ok(rows.Select(a => new CalibrationDueRow(
            a.Id, a.AssetTag, a.Name, a.Category, a.Criticality,
            a.CalibrationDate, a.CalibrationDueDate!.Value,
            a.CalibrationDueDate!.Value.DayNumber - today.DayNumber,
            a.Warehouse != null ? a.Warehouse.Name : a.Facility != null ? a.Facility.Name : a.LocationNote)).ToList());
    }

    private static AssetJobDto ToDto(MaintenanceJob j) => AssetsController.JobToDto(j);
}
