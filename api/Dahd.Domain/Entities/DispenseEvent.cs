using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class DispenseEvent : Entity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public decimal Quantity { get; set; }
    public Guid FacilityId { get; set; }
    public Facility Facility { get; set; } = default!;
    public string? AnimalEarTag { get; set; }
    public AnimalSpecies AnimalSpecies { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerMobile { get; set; }
    public string? Diagnosis { get; set; }
    public string? VetName { get; set; }
    public string? VetLicenceNo { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public string? Remarks { get; set; }
}
