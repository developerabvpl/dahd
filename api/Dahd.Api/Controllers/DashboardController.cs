using Dahd.Application;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(DahdDbContext db) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardKpiDto>> Kpis(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(30);
        var yesterday = DateTime.UtcNow.AddHours(-24);
        var last30 = DateTime.UtcNow.AddDays(-30);

        var dto = new DashboardKpiDto(
            TotalDrugs: await db.Drugs.CountAsync(ct),
            TotalVaccines: await db.Drugs.CountAsync(d => d.IsVaccine, ct),
            TotalWarehouses: await db.Warehouses.CountAsync(ct),
            TotalFacilities: await db.Facilities.CountAsync(ct),
            ActiveBatches: await db.Batches.CountAsync(b => b.Status == BatchStatus.InStore, ct),
            BatchesNearExpiry30Days: await db.Batches.CountAsync(b => b.Status == BatchStatus.InStore && b.ExpiryDate <= soon && b.ExpiryDate >= today, ct),
            BatchesExpired: await db.Batches.CountAsync(b => b.ExpiryDate < today, ct),
            OpenIndents: await db.Indents.CountAsync(i => i.Status != IndentStatus.Closed && i.Status != IndentStatus.Rejected, ct),
            ColdChainBreachesLast24h: await db.ColdChainLogs.CountAsync(c => c.IsBreach && c.ReadingAt >= yesterday, ct),
            DispenseEventsLast30Days: await db.DispenseEvents.CountAsync(d => d.DispensedAt >= last30, ct)
        );
        return Ok(dto);
    }
}
