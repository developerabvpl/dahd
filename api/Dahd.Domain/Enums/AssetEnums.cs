namespace Dahd.Domain.Enums;

public enum AssetCategory
{
    DiagnosticEquipment = 1,
    ColdChainEquipment = 2,
    SurgicalInstrument = 3,
    LabEquipment = 4,
    AiEquipment = 5,
    Vehicle = 6,
    ItHardware = 7,
    Furniture = 8,
    Other = 99
}

public enum AssetStatus
{
    Active = 1,
    UnderMaintenance = 2,
    BreakdownReported = 3,
    Condemned = 4,
    Disposed = 5
}

public enum AssetCondition
{
    New = 1,
    Good = 2,
    Fair = 3,
    Poor = 4
}

public enum MaintenanceJobType
{
    Preventive = 1,
    Breakdown = 2,
    Calibration = 3,
    Inspection = 4
}

public enum MaintenanceJobStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum AmcStatus
{
    Active = 1,
    Expired = 2,
    Cancelled = 3
}

/// <summary>Maintenance-contract kind. AMC = labour only; CMC = comprehensive, includes spare parts.</summary>
public enum MaintenanceContractType
{
    Amc = 1,
    Cmc = 2
}

/// <summary>Asset criticality class (Work121 "Category A/B/C") — drives incident prioritisation.</summary>
public enum AssetCriticality
{
    A = 1, // mission-critical: failure halts operations
    B = 2, // important: degraded operation
    C = 3  // routine: low operational impact
}

/// <summary>ITIL incident impact — how much of the operation is affected.</summary>
public enum IncidentImpact
{
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>ITIL incident urgency — how quickly resolution is needed.</summary>
public enum IncidentUrgency
{
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>Derived from Impact × Urgency; sets the SLA deadline.</summary>
public enum IncidentPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>Coarse problem classification for a breakdown/incident.</summary>
public enum IncidentProblemType
{
    NotPoweringOn = 1,
    ErraticReadings = 2,
    PhysicalDamage = 3,
    Overheating = 4,
    Leakage = 5,
    Consumable = 6,
    SoftwareOrControl = 7,
    Other = 99
}
