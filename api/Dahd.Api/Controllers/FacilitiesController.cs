using Dahd.Application;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/facilities")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class FacilitiesController(DahdDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FacilityDto>>> Get(CancellationToken ct)
    {
        var rows = await db.Facilities.AsNoTracking().OrderBy(f => f.Type).ThenBy(f => f.Name).ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FacilityDto>> GetById(Guid id, CancellationToken ct)
    {
        var f = await db.Facilities.FindAsync([id], ct);
        return f is null ? NotFound() : Ok(ToDto(f));
    }

    private static FacilityDto ToDto(Facility f) => new(
        f.Id, f.Code, f.Name, f.Type,
        f.DivisionName, f.DistrictName, f.BlockName,
        f.InchargeName, f.ContactPhone, f.MvuVehicleRegistration, f.IsActive);
}
