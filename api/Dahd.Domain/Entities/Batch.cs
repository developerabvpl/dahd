using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Batch : Entity
{
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ManufactureDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public Guid CurrentWarehouseId { get; set; }
    public Warehouse CurrentWarehouse { get; set; } = default!;
    public BatchStatus Status { get; set; } = BatchStatus.InStore;
    public string? PurchaseOrderRef { get; set; }
    public string? Remarks { get; set; }
}
