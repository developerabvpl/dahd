using Dahd.Domain.Enums;

namespace Dahd.Application;

public record DrugDto(
    Guid Id, string Code, string Name, string? GenericName,
    FormularyClass FormularyClass, bool IsVaccine, bool ColdChainRequired,
    decimal? StorageTempMinCelsius, decimal? StorageTempMaxCelsius,
    string UnitOfMeasure, string? ScheduleClass, string? Manufacturer, bool IsActive);

public record CreateDrugRequest(
    string Code, string Name, string? GenericName,
    FormularyClass FormularyClass, bool IsVaccine, bool ColdChainRequired,
    decimal? StorageTempMinCelsius, decimal? StorageTempMaxCelsius,
    string UnitOfMeasure, string? ScheduleClass, string? Manufacturer);

public record WarehouseDto(
    Guid Id, string Code, string Name, WarehouseType Type,
    Guid? ParentWarehouseId, string? DivisionName, string? DistrictName,
    bool ColdChainCapable, string? InchargeName, string? ContactPhone, bool IsActive);

public record CreateWarehouseRequest(
    string Code, string Name, WarehouseType Type, Guid? ParentWarehouseId,
    string? DivisionName, string? DistrictName, string? Address,
    bool ColdChainCapable, string? InchargeName, string? ContactPhone);

public record FacilityDto(
    Guid Id, string Code, string Name, FacilityType Type,
    string? DivisionName, string? DistrictName, string? BlockName,
    string? InchargeName, string? ContactPhone, string? MvuVehicleRegistration, bool IsActive);

public record BatchDto(
    Guid Id, Guid DrugId, string DrugName, string BatchNumber,
    DateOnly ManufactureDate, DateOnly ExpiryDate, string? Manufacturer,
    decimal Quantity, decimal UnitCost, Guid CurrentWarehouseId, string WarehouseName,
    BatchStatus Status, int DaysToExpiry);

public record CreateBatchRequest(
    Guid DrugId, string BatchNumber, DateOnly ManufactureDate, DateOnly ExpiryDate,
    string? Manufacturer, decimal Quantity, decimal UnitCost,
    Guid CurrentWarehouseId, string? PurchaseOrderRef);

public record IndentLineDto(
    Guid Id, Guid DrugId, string DrugCode, string DrugName,
    decimal RequestedQuantity, decimal? ApprovedQuantity, decimal? IssuedQuantity,
    decimal? ReceivedQuantity, Guid? IssuedBatchId, string? Remarks);

public record IndentDto(
    Guid Id, string IndentNumber,
    Guid RaisedByWarehouseId, string RaisedByWarehouseName,
    Guid FulfilledByWarehouseId, string FulfilledByWarehouseName,
    IndentStatus Status, DateTime? SubmittedAt, DateTime? ApprovedAt,
    DateTime? IssuedAt, DateTime? ReceivedAt, string? Remarks,
    IReadOnlyList<IndentLineDto> Lines);

public record CreateIndentLineRequest(Guid DrugId, decimal RequestedQuantity, string? Remarks);
public record CreateIndentRequest(
    Guid RaisedByWarehouseId, Guid FulfilledByWarehouseId,
    string? Remarks, List<CreateIndentLineRequest> Lines);

public record ColdChainLogDto(
    Guid Id, Guid WarehouseId, string WarehouseName,
    string DeviceId, string DeviceName, DateTime ReadingAt,
    decimal TemperatureCelsius, bool IsBreach, string? Remarks);

public record CreateColdChainLogRequest(
    Guid WarehouseId, string DeviceId, string DeviceName,
    DateTime ReadingAt, decimal TemperatureCelsius, string? Remarks);

public record DispenseEventDto(
    Guid Id, Guid BatchId, string DrugName, string BatchNumber,
    decimal Quantity, Guid FacilityId, string FacilityName,
    string? AnimalEarTag, AnimalSpecies AnimalSpecies,
    string? OwnerName, string? OwnerMobile, string? Diagnosis,
    string? VetName, DateTime DispensedAt);

public record CreateDispenseRequest(
    Guid BatchId, decimal Quantity, Guid FacilityId,
    string? AnimalEarTag, AnimalSpecies AnimalSpecies,
    string? OwnerName, string? OwnerMobile, string? Diagnosis,
    string? VetName, string? VetLicenceNo, string? Remarks);

public record DashboardKpiDto(
    int TotalDrugs, int TotalVaccines, int TotalWarehouses, int TotalFacilities,
    int ActiveBatches, int BatchesNearExpiry30Days, int BatchesExpired,
    int OpenIndents, int ColdChainBreachesLast24h, int DispenseEventsLast30Days);
