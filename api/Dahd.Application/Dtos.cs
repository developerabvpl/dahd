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

public record LineApproval(Guid LineId, decimal ApprovedQuantity);
public record ApproveIndentRequest(List<LineApproval>? LineApprovals);

public record StockByDrugRow(
    Guid DrugId, string DrugCode, string DrugName, string UnitOfMeasure,
    Guid WarehouseId, string WarehouseCode, string WarehouseName,
    decimal TotalQuantity, int BatchCount,
    int BatchesExpired, int BatchesNearExpiry30Days);

public record ColdChainLogDto(
    Guid Id, Guid WarehouseId, string WarehouseName,
    string DeviceId, string DeviceName, DateTime ReadingAt,
    decimal TemperatureCelsius, bool IsBreach, string? Remarks,
    DateTime? AcknowledgedAt, string? AcknowledgedBy,
    string? CorrectiveAction, string? AffectedBatchIdsJson);

public record CreateColdChainLogRequest(
    Guid WarehouseId, string DeviceId, string DeviceName,
    DateTime ReadingAt, decimal TemperatureCelsius, string? Remarks);

public record AcknowledgeBreachRequest(string CorrectiveAction, List<Guid>? AffectedBatchIds);

public record ColdChainDailyRollupRow(
    Guid WarehouseId, string WarehouseName, string DeviceId, string DeviceName,
    DateOnly Date, int ReadingCount, int BreachCount,
    decimal MinC, decimal MaxC, decimal MeanC);

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

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(
    string AccessToken, DateTime AccessExpiresAt,
    string RefreshToken, DateTime RefreshExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id, string Username, string DisplayName, string? Email,
    AppRole Role, Guid? WarehouseId, Guid? FacilityId);

public record AuditEventDto(
    Guid Id, DateTime OccurredAt, string EntityType, Guid EntityId, string Action,
    Guid? ActorUserId, string? ActorUsername, string? ActorRole,
    string? IpAddress, string? CorrelationId, string? Summary,
    string? BeforeJson, string? AfterJson);

public record VendorDocumentDto(
    Guid Id, VendorDocumentType DocumentType, string FileName, string? StorageRef,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly? IssuedDate, DateOnly? ExpiryDate, string? Notes,
    DateTime UploadedAt, string? UploadedBy);

public record VendorDto(
    Guid Id, Guid UserId, string Username,
    string LegalName, string? TradeName,
    string ContactPerson, string ContactEmail, string ContactPhone,
    string? City, string? State, string? Pincode,
    string? Gstin, string? Pan, string? UdyamRegNumber,
    bool IsManufacturer, bool IsMsme, VendorCategory Categories,
    VendorStatus Status,
    DateTime? SubmittedAt, DateTime? UnderReviewAt,
    DateTime? ApprovedAt, DateTime? RejectedAt, DateTime? BlacklistedAt,
    string? ReviewedBy, string? ReviewRemarks,
    DateTime? ScheduledInspectionAt, string? InspectionRemarks,
    string? BlacklistReason, DateOnly? EmpanelmentValidUntil,
    IReadOnlyList<VendorDocumentDto> Documents);

public record VendorRegistrationRequest(
    string Username, string Password,
    string LegalName, string? TradeName,
    string ContactPerson, string ContactEmail, string ContactPhone,
    string? Address, string? City, string? State, string? Pincode,
    string? Gstin, string? Pan, string? UdyamRegNumber,
    bool IsManufacturer, bool IsMsme,
    VendorCategory Categories);

public record UploadVendorDocumentRequest(
    VendorDocumentType DocumentType, string FileName, string? StorageRef,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly? IssuedDate, DateOnly? ExpiryDate, string? Notes);

public record VendorReviewActionRequest(
    string? Remarks, DateTime? ScheduledInspectionAt,
    DateOnly? EmpanelmentValidUntil, string? BlacklistReason);

public record ProcurementCampaignDto(
    Guid Id, string Code, string Name, SchemeBucket Scheme,
    Guid DrugId, string DrugCode, string DrugName,
    DateOnly WindowStart, DateOnly WindowEnd, int LeadDays,
    decimal TargetDoseCount, string? TargetCohortDescription,
    CampaignStatus Status, string? Notes,
    DateTime? IndentsDraftedAt, int IndentsDraftedCount,
    int DaysToWindowStart, int DaysToProcurementStart);

public record CreateCampaignRequest(
    string Code, string Name, SchemeBucket Scheme,
    Guid DrugId, DateOnly WindowStart, DateOnly WindowEnd,
    int LeadDays, decimal TargetDoseCount,
    string? TargetCohortDescription, string? Notes);

public record DraftCampaignIndentsRequest(
    Guid SourceWarehouseId, decimal QuantityPerDestination);

public record DraftCampaignIndentsResponse(
    int IndentsCreated, decimal TotalQuantityRequested);
