using Dahd.Application;
using Dahd.Domain.Entities;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/dispense")]
public class DispenseController(DahdDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DispenseEventDto>>> Get(
        [FromQuery] Guid? facilityId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var q = db.DispenseEvents
            .AsNoTracking()
            .Include(d => d.Batch).ThenInclude(b => b.Drug)
            .Include(d => d.Facility)
            .Where(d => d.DispensedAt >= since);
        if (facilityId.HasValue) q = q.Where(d => d.FacilityId == facilityId);

        var rows = await q.OrderByDescending(d => d.DispensedAt).Take(500).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<DispenseEventDto>> Create([FromBody] CreateDispenseRequest req, CancellationToken ct)
    {
        var batch = await db.Batches.Include(b => b.Drug).FirstOrDefaultAsync(b => b.Id == req.BatchId, ct);
        if (batch is null) return BadRequest($"Batch {req.BatchId} not found.");

        var facility = await db.Facilities.FindAsync([req.FacilityId], ct);
        if (facility is null) return BadRequest($"Facility {req.FacilityId} not found.");

        if (req.Quantity <= 0) return BadRequest("Quantity must be greater than zero.");
        if (req.Quantity > batch.Quantity) return BadRequest("Quantity exceeds available stock in batch.");

        if (batch.ExpiryDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest("Batch is expired and cannot be dispensed.");

        batch.Quantity -= req.Quantity;

        var ev = new DispenseEvent
        {
            BatchId = req.BatchId,
            Quantity = req.Quantity,
            FacilityId = req.FacilityId,
            AnimalEarTag = req.AnimalEarTag,
            AnimalSpecies = req.AnimalSpecies,
            OwnerName = req.OwnerName,
            OwnerMobile = req.OwnerMobile,
            Diagnosis = req.Diagnosis,
            VetName = req.VetName,
            VetLicenceNo = req.VetLicenceNo,
            DispensedAt = DateTime.UtcNow,
            Remarks = req.Remarks
        };
        db.DispenseEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        ev.Batch = batch;
        ev.Facility = facility;
        return Ok(ToDto(ev));
    }

    private static DispenseEventDto ToDto(DispenseEvent d) => new(
        d.Id, d.BatchId, d.Batch.Drug.Name, d.Batch.BatchNumber,
        d.Quantity, d.FacilityId, d.Facility.Name,
        d.AnimalEarTag, d.AnimalSpecies, d.OwnerName, d.OwnerMobile,
        d.Diagnosis, d.VetName, d.DispensedAt);
}
