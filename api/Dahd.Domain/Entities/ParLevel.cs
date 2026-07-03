using Dahd.Domain.Common;

namespace Dahd.Domain.Entities;

/// <summary>
/// Minimum stock a warehouse should hold for a drug. When on-hand falls below
/// ParQuantity the item is "below par" and can be auto-indented back up to
/// ReorderToQuantity (or ParQuantity if not set). Mirrors D11 par-level mgmt.
/// </summary>
public class ParLevel : Entity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public decimal ParQuantity { get; set; }
    public decimal? ReorderToQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
