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
[Route("api/warehouses")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class WarehousesController(DahdDbContext db, IAuditLogger audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> Get(CancellationToken ct)
    {
        var rows = await db.Warehouses.AsNoTracking().OrderBy(w => w.Type).ThenBy(w => w.Name).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id, CancellationToken ct)
    {
        var w = await db.Warehouses.FindAsync([id], ct);
        return w is null ? NotFound() : Ok(ToDto(w));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ManageMasterData)]
    public async Task<ActionResult<WarehouseDto>> Create([FromBody] CreateWarehouseRequest req, CancellationToken ct)
    {
        if (await db.Warehouses.AnyAsync(w => w.Code == req.Code, ct))
            return Conflict($"Warehouse code '{req.Code}' already exists.");

        var w = new Warehouse
        {
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            ParentWarehouseId = req.ParentWarehouseId,
            DivisionName = req.DivisionName,
            DistrictName = req.DistrictName,
            Address = req.Address,
            ColdChainCapable = req.ColdChainCapable,
            InchargeName = req.InchargeName,
            ContactPhone = req.ContactPhone
        };
        db.Warehouses.Add(w);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(nameof(Warehouse), w.Id, "Create", after: ToDto(w),
            summary: $"Warehouse {w.Code} created", ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = w.Id }, ToDto(w));
    }

    private static WarehouseDto ToDto(Warehouse w) => new(
        w.Id, w.Code, w.Name, w.Type, w.ParentWarehouseId,
        w.DivisionName, w.DistrictName, w.ColdChainCapable,
        w.InchargeName, w.ContactPhone, w.IsActive);
}
