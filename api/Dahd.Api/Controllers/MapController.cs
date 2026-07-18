using Dahd.Application;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/map")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class MapController(DahdDbContext db) : ControllerBase
{
    /// <summary>Real warehouses with their live stock and cold-chain equipment, for the Network Map's "Live" layer.</summary>
    [HttpGet("warehouses")]
    public async Task<ActionResult<IReadOnlyList<MapWarehouseDto>>> Warehouses(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Type).ThenBy(w => w.Name)
            .ToListAsync(ct);

        var batches = await db.Batches.AsNoTracking()
            .Include(b => b.Drug)
            .Where(b => b.Status == BatchStatus.InStore && b.Quantity > 0)
            .ToListAsync(ct);

        var ccUnits = await db.Assets.AsNoTracking()
            .Where(a => a.Category == AssetCategory.ColdChainEquipment && a.WarehouseId != null)
            .ToListAsync(ct);

        var list = warehouses.Select(w =>
        {
            var stock = batches
                .Where(b => b.CurrentWarehouseId == w.Id)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new MapStockLine(
                    b.Drug.Name, b.Drug.Code, b.Drug.IsVaccine, b.Drug.ColdChainRequired,
                    b.Drug.StorageTempMinCelsius, b.Drug.StorageTempMaxCelsius,
                    b.BatchNumber, b.Quantity, b.Drug.UnitOfMeasure,
                    b.ExpiryDate, b.ExpiryDate.DayNumber - today.DayNumber))
                .ToList();

            var units = ccUnits
                .Where(a => a.WarehouseId == w.Id)
                .Select(a => new MapColdUnitDto(a.AssetTag, a.Name, a.Model, a.Status.ToString(), a.Condition.ToString()))
                .ToList();

            return new MapWarehouseDto(
                w.Id, w.Code, w.Name, w.Type.ToString(), w.DistrictName, w.DivisionName,
                w.ColdChainCapable, w.InchargeName, w.ContactPhone, w.Address,
                stock.Sum(s => s.Quantity), stock.Count, units, stock);
        }).ToList();

        return Ok(list);
    }
}
