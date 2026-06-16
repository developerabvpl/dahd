using Dahd.Domain.Common;

namespace Dahd.Domain.Entities;

public class ColdChainLog : Entity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime ReadingAt { get; set; }
    public decimal TemperatureCelsius { get; set; }
    public bool IsBreach { get; set; }
    public string? Remarks { get; set; }
    public string? RecordedBy { get; set; }
}
