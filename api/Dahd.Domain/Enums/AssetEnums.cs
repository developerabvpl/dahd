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
