using Dahd.Application;
using Dahd.Domain.Entities;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/drugs")]
public class DrugsController(DahdDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DrugDto>>> Get(
        [FromQuery] bool? vaccinesOnly,
        [FromQuery] bool? coldChainOnly,
        CancellationToken ct)
    {
        var q = db.Drugs.AsNoTracking().AsQueryable();
        if (vaccinesOnly == true) q = q.Where(d => d.IsVaccine);
        if (coldChainOnly == true) q = q.Where(d => d.ColdChainRequired);

        var rows = await q.OrderBy(d => d.Name).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DrugDto>> GetById(Guid id, CancellationToken ct)
    {
        var d = await db.Drugs.FindAsync([id], ct);
        return d is null ? NotFound() : Ok(ToDto(d));
    }

    [HttpPost]
    public async Task<ActionResult<DrugDto>> Create([FromBody] CreateDrugRequest req, CancellationToken ct)
    {
        if (await db.Drugs.AnyAsync(d => d.Code == req.Code, ct))
            return Conflict($"Drug code '{req.Code}' already exists.");

        var drug = new Drug
        {
            Code = req.Code,
            Name = req.Name,
            GenericName = req.GenericName,
            FormularyClass = req.FormularyClass,
            IsVaccine = req.IsVaccine,
            ColdChainRequired = req.ColdChainRequired,
            StorageTempMinCelsius = req.StorageTempMinCelsius,
            StorageTempMaxCelsius = req.StorageTempMaxCelsius,
            UnitOfMeasure = req.UnitOfMeasure,
            ScheduleClass = req.ScheduleClass,
            Manufacturer = req.Manufacturer
        };
        db.Drugs.Add(drug);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = drug.Id }, ToDto(drug));
    }

    private static DrugDto ToDto(Drug d) => new(
        d.Id, d.Code, d.Name, d.GenericName, d.FormularyClass, d.IsVaccine, d.ColdChainRequired,
        d.StorageTempMinCelsius, d.StorageTempMaxCelsius, d.UnitOfMeasure, d.ScheduleClass,
        d.Manufacturer, d.IsActive);
}
