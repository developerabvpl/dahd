using Dahd.Application;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

/// <summary>
/// Stub for the INAPH / Bharat Pashudhan animal-ID integration.
/// Real DAHD API access is a Phase-5 contract dependency; this controller
/// returns a deterministic-yet-realistic response derived from the ear-tag
/// pattern, marked IsStub=true so the UI can show 'simulated' clearly.
/// Outbreak alerts are computed from live dispense events.
/// </summary>
[ApiController]
[Route("api/inaph")]
[Authorize(Roles = AppRoles.AnyAuthenticated)]
public class InaphController(DahdDbContext db) : ControllerBase
{
    private static readonly string[] CattleBreeds =
        ["Sahiwal", "Gir", "Tharparkar", "Gangatiri", "Haryana", "Hariana"];
    private static readonly string[] BuffaloBreeds = ["Murrah", "Bhadawari", "Surti"];
    private static readonly string[] GoatBreeds = ["Jamunapari", "Barbari", "Beetal"];
    private static readonly string[] Vaccines = ["FMD-VAX", "BRU-S19", "HS-VAX", "RAB-VAX"];
    private static readonly string[] Districts = ["Lucknow", "Meerut", "Varanasi", "Gorakhpur", "Agra", "Mathura", "Bareilly"];

    [HttpGet("lookup/{earTag}")]
    public async Task<ActionResult<InaphAnimalDto>> Lookup(string earTag, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(earTag) || earTag.Length < 4)
            return BadRequest("Ear-tag must be at least 4 characters.");

        // Deterministic pseudo-data driven by the tag string so the same tag
        // always returns the same animal — useful for repeat demos.
        var hash = StableHash(earTag);
        var speciesPick = hash % 5;
        var species = speciesPick switch
        {
            0 or 1 => AnimalSpecies.Cattle,
            2 => AnimalSpecies.Buffalo,
            3 => AnimalSpecies.Goat,
            _ => AnimalSpecies.Sheep
        };

        var breed = species switch
        {
            AnimalSpecies.Cattle => CattleBreeds[(int)(hash % (uint)CattleBreeds.Length)],
            AnimalSpecies.Buffalo => BuffaloBreeds[(int)(hash % (uint)BuffaloBreeds.Length)],
            AnimalSpecies.Goat => GoatBreeds[(int)(hash % (uint)GoatBreeds.Length)],
            _ => "Local"
        };

        // Look for a real prior dispense event for this ear-tag to anchor
        // the "last vaccination" field — otherwise synthesise.
        var lastEvent = await db.DispenseEvents.AsNoTracking()
            .Include(d => d.Batch).ThenInclude(b => b.Drug)
            .Where(d => d.AnimalEarTag == earTag)
            .OrderByDescending(d => d.DispensedAt)
            .FirstOrDefaultAsync(ct);

        var district = Districts[(int)(hash % (uint)Districts.Length)];
        var ageMonths = 6 + (int)(hash % 60);
        var sex = (hash % 2 == 0) ? "Female" : "Male";

        return Ok(new InaphAnimalDto(
            EarTag: earTag,
            Species: species,
            Breed: breed,
            AgeMonths: ageMonths,
            Sex: sex,
            OwnerName: lastEvent?.OwnerName,
            OwnerMobile: lastEvent?.OwnerMobile,
            Village: $"Village-{(int)(hash % 200)}",
            District: district,
            LastVaccinationDate: lastEvent is null
                ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(int)(hash % 365))
                : DateOnly.FromDateTime(lastEvent.DispensedAt),
            LastVaccineCode: lastEvent?.Batch.Drug.Code ?? Vaccines[(int)(hash % (uint)Vaccines.Length)],
            RegisteredOnBharatPashudhan: true,
            IsStub: true));
    }

    /// <summary>
    /// Real disease-cluster detection in the spirit of Section 39 PCICDA
    /// reporting on Bharat Pashudhan. Buckets dispense events by
    /// (diagnosis, species, district) and flags any cluster of 3+
    /// events affecting 2+ animals in the last `days` window.
    /// </summary>
    [HttpGet("outbreak-alerts")]
    public async Task<ActionResult<IReadOnlyList<OutbreakAlertDto>>> OutbreakAlerts(
        [FromQuery] int days = 14,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 90));

        var events = await db.DispenseEvents.AsNoTracking()
            .Include(d => d.Facility)
            .Where(d => d.DispensedAt >= since
                        && d.Diagnosis != null
                        && d.Diagnosis != "Routine vaccination")
            .Select(d => new
            {
                Diagnosis = d.Diagnosis!,
                d.AnimalSpecies,
                District = d.Facility.DistrictName,
                d.AnimalEarTag,
                d.DispensedAt
            })
            .ToListAsync(ct);

        var clusters = events
            .GroupBy(e => new { e.Diagnosis, e.AnimalSpecies, e.District })
            .Select(g => new
            {
                g.Key.Diagnosis,
                g.Key.AnimalSpecies,
                g.Key.District,
                Count = g.Count(),
                Distinct = g.Select(x => x.AnimalEarTag).Where(t => t is not null).Distinct().Count(),
                First = g.Min(x => x.DispensedAt),
                Last = g.Max(x => x.DispensedAt)
            })
            .Where(c => c.Count >= 3 && c.Distinct >= 2)
            .OrderByDescending(c => c.Count)
            .Select(c => new OutbreakAlertDto(
                c.Diagnosis, c.AnimalSpecies, c.District,
                c.Count, c.Distinct, c.First, c.Last,
                c.Count >= 10 ? "Critical" : c.Count >= 5 ? "Warning" : "Watch"))
            .ToList();

        return Ok(clusters);
    }

    private static uint StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            foreach (var c in s)
            {
                h ^= c;
                h *= 16777619u;
            }
            return h;
        }
    }
}
