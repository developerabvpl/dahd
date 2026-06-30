using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Asset : Entity
{
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public string? LocationNote { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateOnly? WarrantyUntil { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Notes { get; set; }

    public List<MaintenanceSchedule> Schedules { get; set; } = new();
    public List<MaintenanceJob> Jobs { get; set; } = new();
    public List<AmcContract> AmcContracts { get; set; } = new();
}

public class MaintenanceSchedule : Entity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = default!;
    public string TaskDescription { get; set; } = string.Empty;
    public int FrequencyDays { get; set; }
    public DateOnly? LastServiceDate { get; set; }
    public DateOnly NextDueDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MaintenanceJob : Entity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = default!;
    public Guid? ScheduleId { get; set; }
    public MaintenanceSchedule? Schedule { get; set; }

    public string JobNumber { get; set; } = string.Empty;
    public MaintenanceJobType Type { get; set; }
    public MaintenanceJobStatus Status { get; set; } = MaintenanceJobStatus.Open;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public string? ReportedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Resolution { get; set; }
    public decimal? Cost { get; set; }
}

public class AmcContract : Entity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = default!;
    public string ContractNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal AnnualCost { get; set; }
    public string? Coverage { get; set; }
    public AmcStatus Status { get; set; } = AmcStatus.Active;
}
