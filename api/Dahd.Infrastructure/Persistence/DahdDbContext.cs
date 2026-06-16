using Dahd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Infrastructure.Persistence;

public class DahdDbContext : DbContext
{
    public DahdDbContext(DbContextOptions<DahdDbContext> options) : base(options) { }

    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Indent> Indents => Set<Indent>();
    public DbSet<IndentLine> IndentLines => Set<IndentLine>();
    public DbSet<ColdChainLog> ColdChainLogs => Set<ColdChainLog>();
    public DbSet<DispenseEvent> DispenseEvents => Set<DispenseEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Drug>(e =>
        {
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(40).IsRequired();
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.Property(d => d.UnitOfMeasure).HasMaxLength(40);
            e.Property(d => d.ScheduleClass).HasMaxLength(40);
            e.Property(d => d.StorageTempMinCelsius).HasPrecision(5, 2);
            e.Property(d => d.StorageTempMaxCelsius).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Warehouse>(e =>
        {
            e.HasIndex(w => w.Code).IsUnique();
            e.Property(w => w.Code).HasMaxLength(40).IsRequired();
            e.Property(w => w.Name).HasMaxLength(200).IsRequired();
            e.HasOne(w => w.ParentWarehouse)
                .WithMany()
                .HasForeignKey(w => w.ParentWarehouseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Facility>(e =>
        {
            e.HasIndex(f => f.Code).IsUnique();
            e.Property(f => f.Code).HasMaxLength(40).IsRequired();
            e.Property(f => f.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Batch>(e =>
        {
            e.Property(b => b.BatchNumber).HasMaxLength(80).IsRequired();
            e.Property(b => b.Quantity).HasPrecision(18, 4);
            e.Property(b => b.UnitCost).HasPrecision(18, 4);
            e.HasIndex(b => new { b.DrugId, b.BatchNumber }).IsUnique();
            e.HasOne(b => b.Drug).WithMany().HasForeignKey(b => b.DrugId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.CurrentWarehouse).WithMany().HasForeignKey(b => b.CurrentWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Indent>(e =>
        {
            e.HasIndex(i => i.IndentNumber).IsUnique();
            e.Property(i => i.IndentNumber).HasMaxLength(40).IsRequired();
            e.HasOne(i => i.RaisedByWarehouse).WithMany().HasForeignKey(i => i.RaisedByWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.FulfilledByWarehouse).WithMany().HasForeignKey(i => i.FulfilledByWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IndentLine>(e =>
        {
            e.Property(l => l.RequestedQuantity).HasPrecision(18, 4);
            e.Property(l => l.ApprovedQuantity).HasPrecision(18, 4);
            e.Property(l => l.IssuedQuantity).HasPrecision(18, 4);
            e.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
            e.HasOne(l => l.Indent).WithMany(i => i.Lines).HasForeignKey(l => l.IndentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Drug).WithMany().HasForeignKey(l => l.DrugId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.IssuedBatch).WithMany().HasForeignKey(l => l.IssuedBatchId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ColdChainLog>(e =>
        {
            e.Property(c => c.DeviceId).HasMaxLength(80).IsRequired();
            e.Property(c => c.DeviceName).HasMaxLength(200);
            e.Property(c => c.TemperatureCelsius).HasPrecision(5, 2);
            e.HasIndex(c => new { c.WarehouseId, c.ReadingAt });
            e.HasOne(c => c.Warehouse).WithMany().HasForeignKey(c => c.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DispenseEvent>(e =>
        {
            e.Property(d => d.Quantity).HasPrecision(18, 4);
            e.Property(d => d.AnimalEarTag).HasMaxLength(40);
            e.Property(d => d.OwnerName).HasMaxLength(200);
            e.Property(d => d.OwnerMobile).HasMaxLength(20);
            e.HasIndex(d => d.DispensedAt);
            e.HasOne(d => d.Batch).WithMany().HasForeignKey(d => d.BatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Facility).WithMany().HasForeignKey(d => d.FacilityId).OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
