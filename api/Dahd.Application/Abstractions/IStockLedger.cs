using Dahd.Domain.Enums;

namespace Dahd.Application.Abstractions;

/// <summary>
/// Appends a stock-movement row to the current unit of work. The caller's
/// SaveChanges persists it in the same transaction as the stock change, so the
/// ledger can never drift from the batch quantities it records.
/// </summary>
public interface IStockLedger
{
    void Record(
        StockMovementType type,
        Guid drugId,
        Guid warehouseId,
        Guid? batchId,
        string? batchNumber,
        decimal quantityDelta,
        string? reference = null,
        string? note = null);
}
