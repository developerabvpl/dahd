using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Drug : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public FormularyClass FormularyClass { get; set; }
    public bool IsVaccine { get; set; }
    public bool ColdChainRequired { get; set; }
    public decimal? StorageTempMinCelsius { get; set; }
    public decimal? StorageTempMaxCelsius { get; set; }
    public string UnitOfMeasure { get; set; } = "Each";
    public string? ScheduleClass { get; set; }
    public string? Manufacturer { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
}
