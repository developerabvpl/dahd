using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class PurchaseOrder : Entity
{
    public string PoNumber { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    /// <summary>Free-text fallback when the supplier is not an empanelled vendor.</summary>
    public string? VendorName { get; set; }
    public Guid? RateContractId { get; set; }
    public RateContract? RateContract { get; set; }
    /// <summary>Default receiving store; GRN can override per receipt.</summary>
    public Guid DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; } = default!;

    public PoStatus Status { get; set; } = PoStatus.Draft;
    public DateOnly? ExpectedDelivery { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? FullyReceivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? Remarks { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public class PurchaseOrderLine : Entity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = default!;
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public decimal OrderedQuantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? Remarks { get; set; }
}
