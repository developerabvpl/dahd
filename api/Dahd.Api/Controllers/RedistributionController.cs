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
[Route("api/redistribution")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class RedistributionController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    /// <summary>
    /// Identify near-expiry batches and suggest the best recipient warehouse
    /// (lowest existing stock, cold-chain capable when drug requires).
    /// Mirrors the verified RMSC mechanic: near-expiry inter-warehouse redistribution.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<IReadOnlyList<RedistributionSuggestionDto>>> Suggestions(
        [FromQuery] int withinDays = 90,
        [FromQuery] int maxSuggestions = 50,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(Math.Clamp(withinDays, 1, 365));

        var donors = await db.Batches.AsNoTracking()
            .Include(b => b.Drug)
            .Include(b => b.CurrentWarehouse)
            .Where(b => b.Status == BatchStatus.InStore
                        && b.ExpiryDate >= today
                        && b.ExpiryDate <= cutoff
                        && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

        if (donors.Count == 0) return Ok(Array.Empty<RedistributionSuggestionDto>());

        var drugIds = donors.Select(d => d.DrugId).Distinct().ToList();
        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync(ct);

        var stockByDrugWarehouse = await db.Batches.AsNoTracking()
            .Where(b => drugIds.Contains(b.DrugId)
                        && b.Status == BatchStatus.InStore
                        && b.ExpiryDate >= today)
            .GroupBy(b => new { b.DrugId, b.CurrentWarehouseId })
            .Select(g => new { g.Key.DrugId, g.Key.CurrentWarehouseId, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        var stockLookup = stockByDrugWarehouse.ToDictionary(
            s => (s.DrugId, s.CurrentWarehouseId), s => s.Qty);

        var suggestions = new List<RedistributionSuggestionDto>(maxSuggestions);

        foreach (var donor in donors)
        {
            var daysToExpiry = donor.ExpiryDate.DayNumber - today.DayNumber;

            var candidates = warehouses
                .Where(w => w.Id != donor.CurrentWarehouseId)
                .Where(w => !donor.Drug.ColdChainRequired || w.ColdChainCapable);

            var best = candidates
                .Select(w => new
                {
                    Warehouse = w,
                    ExistingStock = stockLookup.GetValueOrDefault((donor.DrugId, w.Id), 0m)
                })
                .Where(x => x.ExistingStock < donor.Quantity * 2)
                .OrderBy(x => x.ExistingStock)
                .ThenBy(x => x.Warehouse.Type)
                .FirstOrDefault();

            if (best is null) continue;

            var urgency =
                daysToExpiry <= 30 ? RedistributionUrgency.Urgent :
                daysToExpiry <= 60 ? RedistributionUrgency.Watch :
                                     RedistributionUrgency.Routine;

            var rationale = best.ExistingStock == 0
                ? $"Recipient has zero in-store stock of {donor.Drug.Code}"
                : $"Recipient has only {best.ExistingStock} {donor.Drug.UnitOfMeasure} vs donor {donor.Quantity}";

            suggestions.Add(new RedistributionSuggestionDto(
                donor.Id, donor.BatchNumber,
                donor.DrugId, donor.Drug.Code, donor.Drug.Name, donor.Drug.ColdChainRequired,
                donor.CurrentWarehouseId, donor.CurrentWarehouse.Code, donor.CurrentWarehouse.Name,
                donor.Quantity, donor.ExpiryDate, daysToExpiry,
                best.Warehouse.Id, best.Warehouse.Code, best.Warehouse.Name,
                best.ExistingStock, donor.Quantity,
                urgency, rationale));

            if (suggestions.Count >= maxSuggestions) break;
        }

        return Ok(suggestions
            .OrderByDescending(s => s.Urgency)
            .ThenBy(s => s.DaysToExpiry)
            .ToList());
    }

    [HttpPost("create-indent")]
    [Authorize(Roles = AppRoles.IssueOrReceive)]
    public async Task<ActionResult<CreateRedistributionIndentResponse>> CreateIndent(
        [FromBody] CreateRedistributionIndentRequest req,
        CancellationToken ct)
    {
        var donor = await db.Batches.Include(b => b.Drug).Include(b => b.CurrentWarehouse)
            .FirstOrDefaultAsync(b => b.Id == req.DonorBatchId, ct);
        if (donor is null) return BadRequest("Donor batch not found.");
        if (donor.Status != BatchStatus.InStore) return BadRequest("Donor batch is not in stock.");
        if (donor.Quantity < req.Quantity) return BadRequest("Donor batch has less stock than requested.");

        var recipient = await db.Warehouses.FindAsync([req.RecipientWarehouseId], ct);
        if (recipient is null) return BadRequest("Recipient warehouse not found.");
        if (recipient.Id == donor.CurrentWarehouseId) return BadRequest("Recipient cannot be the source.");
        if (donor.Drug.ColdChainRequired && !recipient.ColdChainCapable)
            return BadRequest("Drug requires cold chain but recipient is not cold-chain capable.");
        if (req.Quantity <= 0) return BadRequest("Quantity must be positive.");

        var indent = new Indent
        {
            IndentNumber = $"RED-{DateTime.UtcNow:yyyyMMddHHmmss}",
            RaisedByWarehouseId = recipient.Id,
            FulfilledByWarehouseId = donor.CurrentWarehouseId,
            Status = IndentStatus.Draft,
            Remarks = $"Near-expiry redistribution of batch {donor.BatchNumber} (expires {donor.ExpiryDate})",
            Lines = new List<IndentLine>
            {
                new()
                {
                    DrugId = donor.DrugId,
                    RequestedQuantity = req.Quantity,
                    Remarks = $"Redistribution from {donor.CurrentWarehouse.Code} · batch {donor.BatchNumber}"
                }
            }
        };
        db.Indents.Add(indent);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Indent), indent.Id, "RedistributionDraft",
            after: new
            {
                indent.IndentNumber,
                DonorBatchId = donor.Id,
                donor.BatchNumber,
                DrugCode = donor.Drug.Code,
                Quantity = req.Quantity,
                ExpiryDate = donor.ExpiryDate
            },
            summary: $"Redistribution Draft {indent.IndentNumber} for {donor.Drug.Code}/{donor.BatchNumber} → {recipient.Code}",
            ct: ct);

        return Ok(new CreateRedistributionIndentResponse(indent.Id, indent.IndentNumber));
    }
}
