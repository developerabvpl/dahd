using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Indent : Entity
{
    public string IndentNumber { get; set; } = string.Empty;
    public Guid RaisedByWarehouseId { get; set; }
    public Warehouse RaisedByWarehouse { get; set; } = default!;
    public Guid FulfilledByWarehouseId { get; set; }
    public Warehouse FulfilledByWarehouse { get; set; } = default!;
    public IndentStatus Status { get; set; } = IndentStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Remarks { get; set; }
    public List<IndentLine> Lines { get; set; } = new();
}

public class IndentLine : Entity
{
    public Guid IndentId { get; set; }
    public Indent Indent { get; set; } = default!;
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public decimal RequestedQuantity { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public decimal? IssuedQuantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public Guid? IssuedBatchId { get; set; }
    public Batch? IssuedBatch { get; set; }
    public string? Remarks { get; set; }
}
