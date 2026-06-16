using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Facility : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FacilityType Type { get; set; }
    public string? DivisionName { get; set; }
    public string? DistrictName { get; set; }
    public string? BlockName { get; set; }
    public string? Address { get; set; }
    public string? InchargeName { get; set; }
    public string? ContactPhone { get; set; }
    public string? MvuVehicleRegistration { get; set; }
    public bool IsActive { get; set; } = true;
}
