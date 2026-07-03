using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

/// <summary>
/// Append-only stock ledger. One row per stock-changing event. QuantityDelta is
/// signed (+ in, − out). Running balance is derived at read time by cumulating
/// deltas per (drug, warehouse) in chronological order — an Opening row is
/// backfilled per in-store batch so balances reconcile with actual on-hand.
/// </summary>
public class StockMovement : Entity
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public StockMovementType Type { get; set; }
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public decimal QuantityDelta { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public string? ActorUsername { get; set; }
}
