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
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorDocument> VendorDocuments => Set<VendorDocument>();
    public DbSet<ProcurementCampaign> ProcurementCampaigns => Set<ProcurementCampaign>();
    public DbSet<RateContract> RateContracts => Set<RateContract>();
    public DbSet<RateContractItem> RateContractItems => Set<RateContractItem>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<MaintenanceJob> MaintenanceJobs => Set<MaintenanceJob>();
    public DbSet<AmcContract> AmcContracts => Set<AmcContract>();
    public DbSet<ParLevel> ParLevels => Set<ParLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(80).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(200);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.HasOne(u => u.Warehouse).WithMany().HasForeignKey(u => u.WarehouseId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(u => u.Facility).WithMany().HasForeignKey(u => u.FacilityId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(t => t.IsActive);
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.Property(a => a.EntityType).HasMaxLength(80).IsRequired();
            e.Property(a => a.Action).HasMaxLength(80).IsRequired();
            e.Property(a => a.ActorUsername).HasMaxLength(80);
            e.Property(a => a.ActorRole).HasMaxLength(40);
            e.Property(a => a.IpAddress).HasMaxLength(64);
            e.Property(a => a.CorrelationId).HasMaxLength(128);
            e.Property(a => a.Summary).HasMaxLength(500);
            e.HasIndex(a => a.OccurredAt);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.Property(v => v.LegalName).HasMaxLength(200).IsRequired();
            e.Property(v => v.TradeName).HasMaxLength(200);
            e.Property(v => v.ContactPerson).HasMaxLength(200).IsRequired();
            e.Property(v => v.ContactEmail).HasMaxLength(200).IsRequired();
            e.Property(v => v.ContactPhone).HasMaxLength(40).IsRequired();
            e.Property(v => v.City).HasMaxLength(120);
            e.Property(v => v.State).HasMaxLength(120);
            e.Property(v => v.Pincode).HasMaxLength(16);
            e.Property(v => v.Gstin).HasMaxLength(40);
            e.Property(v => v.Pan).HasMaxLength(20);
            e.Property(v => v.UdyamRegNumber).HasMaxLength(40);
            e.Property(v => v.ReviewedBy).HasMaxLength(80);
            e.Property(v => v.ReviewRemarks).HasMaxLength(1000);
            e.Property(v => v.InspectionRemarks).HasMaxLength(1000);
            e.Property(v => v.BlacklistReason).HasMaxLength(1000);
            e.HasIndex(v => v.UserId).IsUnique();
            e.HasIndex(v => v.Status);
            e.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VendorDocument>(e =>
        {
            e.Property(d => d.FileName).HasMaxLength(300).IsRequired();
            e.Property(d => d.StorageRef).HasMaxLength(500);
            e.Property(d => d.IssuingAuthority).HasMaxLength(200);
            e.Property(d => d.CertificateNumber).HasMaxLength(120);
            e.Property(d => d.Notes).HasMaxLength(500);
            e.Property(d => d.UploadedBy).HasMaxLength(80);
            e.HasOne(d => d.Vendor).WithMany(v => v.Documents).HasForeignKey(d => d.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => new { d.VendorId, d.DocumentType });
        });

        modelBuilder.Entity<ProcurementCampaign>(e =>
        {
            e.Property(c => c.Code).HasMaxLength(40).IsRequired();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.TargetCohortDescription).HasMaxLength(500);
            e.Property(c => c.Notes).HasMaxLength(1000);
            e.Property(c => c.TargetDoseCount).HasPrecision(18, 2);
            e.HasIndex(c => c.Code).IsUnique();
            e.HasIndex(c => c.WindowStart);
            e.HasOne(c => c.Drug).WithMany().HasForeignKey(c => c.DrugId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RateContract>(e =>
        {
            e.Property(r => r.ContractNumber).HasMaxLength(60).IsRequired();
            e.Property(r => r.Title).HasMaxLength(300).IsRequired();
            e.Property(r => r.LeadBody).HasMaxLength(40);
            e.Property(r => r.SourceUrl).HasMaxLength(500);
            e.Property(r => r.Notes).HasMaxLength(1000);
            e.HasIndex(r => r.ContractNumber).IsUnique();
            e.HasIndex(r => r.Status);
        });

        modelBuilder.Entity<RateContractItem>(e =>
        {
            e.Property(i => i.UnitRate).HasPrecision(18, 4);
            e.Property(i => i.VendorName).HasMaxLength(200);
            e.Property(i => i.PackSize).HasMaxLength(80);
            e.Property(i => i.Remarks).HasMaxLength(500);
            e.HasOne(i => i.RateContract).WithMany(r => r.Items).HasForeignKey(i => i.RateContractId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Drug).WithMany().HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Vendor).WithMany().HasForeignKey(i => i.VendorId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(i => new { i.RateContractId, i.DrugId });
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.Property(a => a.AssetTag).HasMaxLength(40).IsRequired();
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.Model).HasMaxLength(120);
            e.Property(a => a.SerialNumber).HasMaxLength(120);
            e.Property(a => a.Manufacturer).HasMaxLength(200);
            e.Property(a => a.LocationNote).HasMaxLength(300);
            e.Property(a => a.Notes).HasMaxLength(1000);
            e.Property(a => a.PurchaseCost).HasPrecision(18, 2);
            e.HasIndex(a => a.AssetTag).IsUnique();
            e.HasIndex(a => a.Status);
            e.HasOne(a => a.Warehouse).WithMany().HasForeignKey(a => a.WarehouseId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(a => a.Facility).WithMany().HasForeignKey(a => a.FacilityId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MaintenanceSchedule>(e =>
        {
            e.Property(s => s.TaskDescription).HasMaxLength(300).IsRequired();
            e.HasIndex(s => s.NextDueDate);
            e.HasOne(s => s.Asset).WithMany(a => a.Schedules).HasForeignKey(s => s.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaintenanceJob>(e =>
        {
            e.Property(j => j.JobNumber).HasMaxLength(40).IsRequired();
            e.Property(j => j.Description).HasMaxLength(1000).IsRequired();
            e.Property(j => j.ReportedBy).HasMaxLength(80);
            e.Property(j => j.AssignedTo).HasMaxLength(120);
            e.Property(j => j.Resolution).HasMaxLength(1000);
            e.Property(j => j.Cost).HasPrecision(18, 2);
            e.HasIndex(j => j.JobNumber).IsUnique();
            e.HasIndex(j => new { j.AssetId, j.Status });
            e.HasOne(j => j.Asset).WithMany(a => a.Jobs).HasForeignKey(j => j.AssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(j => j.Schedule).WithMany().HasForeignKey(j => j.ScheduleId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AmcContract>(e =>
        {
            e.Property(a => a.ContractNumber).HasMaxLength(60).IsRequired();
            e.Property(a => a.VendorName).HasMaxLength(200).IsRequired();
            e.Property(a => a.Coverage).HasMaxLength(500);
            e.Property(a => a.AnnualCost).HasPrecision(18, 2);
            e.HasIndex(a => a.ContractNumber).IsUnique();
            e.HasOne(a => a.Asset).WithMany(x => x.AmcContracts).HasForeignKey(a => a.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParLevel>(e =>
        {
            e.Property(p => p.ParQuantity).HasPrecision(18, 4);
            e.Property(p => p.ReorderToQuantity).HasPrecision(18, 4);
            e.HasIndex(p => new { p.WarehouseId, p.DrugId }).IsUnique();
            e.HasOne(p => p.Warehouse).WithMany().HasForeignKey(p => p.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Drug).WithMany().HasForeignKey(p => p.DrugId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(m => m.QuantityDelta).HasPrecision(18, 4);
            e.Property(m => m.BatchNumber).HasMaxLength(80);
            e.Property(m => m.Reference).HasMaxLength(120);
            e.Property(m => m.Note).HasMaxLength(300);
            e.Property(m => m.ActorUsername).HasMaxLength(80);
            e.HasIndex(m => new { m.DrugId, m.WarehouseId, m.OccurredAt });
            e.HasIndex(m => m.BatchId);
            e.HasOne(m => m.Drug).WithMany().HasForeignKey(m => m.DrugId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Warehouse).WithMany().HasForeignKey(m => m.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        // SQLite stores decimal as TEXT, which breaks server-side comparison,
        // ordering and aggregation. Map every decimal to double (REAL) on SQLite
        // so all existing LINQ queries translate. Precision loss is irrelevant at
        // stock-quantity/cost magnitudes.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal))
                        property.SetProviderClrType(typeof(double));
                    else if (property.ClrType == typeof(decimal?))
                        property.SetProviderClrType(typeof(double?));
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
