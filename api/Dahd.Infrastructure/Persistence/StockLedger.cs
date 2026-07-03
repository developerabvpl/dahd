using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;

namespace Dahd.Infrastructure.Persistence;

public sealed class StockLedger(DahdDbContext db, ICurrentUser current) : IStockLedger
{
    public void Record(
        StockMovementType type,
        Guid drugId,
        Guid warehouseId,
        Guid? batchId,
        string? batchNumber,
        decimal quantityDelta,
        string? reference = null,
        string? note = null)
    {
        db.StockMovements.Add(new StockMovement
        {
            OccurredAt = DateTime.UtcNow,
            Type = type,
            DrugId = drugId,
            WarehouseId = warehouseId,
            BatchId = batchId,
            BatchNumber = batchNumber,
            QuantityDelta = quantityDelta,
            Reference = reference,
            Note = note,
            ActorUsername = current.Username
        });
    }
}
