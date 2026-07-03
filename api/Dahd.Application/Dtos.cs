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
    DateTime? IssuedAt, DateTime? ReceivedAt,
    DateTime? RejectedAt, DateTime? CancelledAt, string? RejectionReason,
    string? Remarks, IReadOnlyList<IndentLineDto> Lines);

public record RejectIndentRequest(string Reason);

public record ParLevelRow(
    Guid Id, Guid WarehouseId, string WarehouseCode, string WarehouseName,
    Guid DrugId, string DrugCode, string DrugName, string UnitOfMeasure,
    decimal ParQuantity, decimal? ReorderToQuantity,
    decimal CurrentStock, decimal Shortfall, bool BelowPar, bool IsActive);

public record UpsertParLevelRequest(
    Guid WarehouseId, Guid DrugId, decimal ParQuantity, decimal? ReorderToQuantity);

public record ParAutoIndentRequest(Guid RecipientWarehouseId, Guid SourceWarehouseId);

public record ParAutoIndentResponse(Guid? IndentId, string? IndentNumber, int LineCount, decimal TotalQuantity);

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

public enum RedistributionUrgency
{
    Routine = 1,
    Watch = 2,
    Urgent = 3
}

public record RedistributionSuggestionDto(
    Guid DonorBatchId, string BatchNumber,
    Guid DrugId, string DrugCode, string DrugName, bool ColdChainRequired,
    Guid SourceWarehouseId, string SourceWarehouseCode, string SourceWarehouseName,
    decimal DonorQuantity, DateOnly ExpiryDate, int DaysToExpiry,
    Guid RecipientWarehouseId, string RecipientWarehouseCode, string RecipientWarehouseName,
    decimal RecipientExistingStock, decimal SuggestedQuantity,
    RedistributionUrgency Urgency, string Rationale);

public record CreateRedistributionIndentRequest(
    Guid DonorBatchId, Guid RecipientWarehouseId, decimal Quantity);

public record CreateRedistributionIndentResponse(
    Guid IndentId, string IndentNumber);

public record ConsumptionForecastRow(
    Guid WarehouseId, string WarehouseCode, string WarehouseName,
    Guid DrugId, string DrugCode, string DrugName, string UnitOfMeasure,
    int LookbackDays, decimal LookbackConsumption,
    decimal DailyVelocity,
    int ForecastDays, decimal ProjectedNeed,
    decimal CurrentStock, decimal Shortfall, decimal SafetyStock);

public record DraftQuarterlyIndentRequest(
    Guid RecipientWarehouseId, Guid SourceWarehouseId,
    int LookbackDays, int ForecastDays, decimal SafetyMultiplier);

public record DraftQuarterlyIndentResponse(
    Guid? IndentId, string? IndentNumber, int LineCount, decimal TotalQuantity);

public record DeviceAnalyticsRow(
    Guid WarehouseId, string WarehouseCode, string WarehouseName,
    string DeviceId, string DeviceName,
    int ReadingCount, int BreachCount,
    decimal MinC, decimal MaxC, decimal MeanC, decimal MktC,
    decimal TimeOutOfSpecPct,
    DateTime? FirstReading, DateTime? LastReading);

public record BreachHourMatrixCell(
    int DayOfWeek, int Hour, int BreachCount);

public record InaphAnimalDto(
    string EarTag, AnimalSpecies Species, string? Breed, int? AgeMonths,
    string Sex, string? OwnerName, string? OwnerMobile, string? Village,
    string? District, DateOnly? LastVaccinationDate, string? LastVaccineCode,
    bool RegisteredOnBharatPashudhan, bool IsStub);

public record OutbreakAlertDto(
    string DiseaseProxy, AnimalSpecies Species,
    string? District, int EventCount, int DistinctAnimals,
    DateTime FirstSeenAt, DateTime LastSeenAt, string Severity);

public record RateContractItemDto(
    Guid Id, Guid DrugId, string DrugCode, string DrugName, string UnitOfMeasure,
    Guid? VendorId, string? VendorName,
    decimal UnitRate, string? PackSize, int? MinOrderQuantity, string? Remarks);

public record RateContractDto(
    Guid Id, string ContractNumber, string Title,
    RateContractCategory Category, string LeadBody,
    DateOnly ValidFrom, DateOnly ValidUntil, RateContractStatus Status,
    string? SourceUrl, string? Notes,
    int ItemCount, int DaysToExpiry,
    IReadOnlyList<RateContractItemDto> Items);

public record CreateRateContractRequest(
    string ContractNumber, string Title,
    RateContractCategory Category, string LeadBody,
    DateOnly ValidFrom, DateOnly ValidUntil,
    string? SourceUrl, string? Notes);

public record AddRateContractItemRequest(
    Guid DrugId, Guid? VendorId, string? VendorName,
    decimal UnitRate, string? PackSize, int? MinOrderQuantity, string? Remarks);

public record CheapestRateRow(
    Guid DrugId, string DrugCode, string DrugName,
    Guid RateContractId, string ContractNumber, string ContractTitle,
    Guid? VendorId, string? VendorName,
    decimal UnitRate, string? PackSize, DateOnly ContractValidUntil);

// ---- Assets & maintenance ----

public record AssetScheduleDto(
    Guid Id, string TaskDescription, int FrequencyDays,
    DateOnly? LastServiceDate, DateOnly NextDueDate, bool IsActive, int DaysToDue);

public record AssetJobDto(
    Guid Id, string JobNumber, MaintenanceJobType Type, MaintenanceJobStatus Status,
    DateTime ReportedAt, string? ReportedBy, string Description,
    string? AssignedTo, DateTime? StartedAt, DateTime? CompletedAt,
    string? Resolution, decimal? Cost);

public record AssetAmcDto(
    Guid Id, string ContractNumber, string VendorName,
    DateOnly StartDate, DateOnly EndDate, decimal AnnualCost,
    string? Coverage, AmcStatus Status, int DaysToExpiry);

public record AssetDto(
    Guid Id, string AssetTag, string Name, AssetCategory Category,
    string? Model, string? SerialNumber, string? Manufacturer,
    Guid? WarehouseId, string? WarehouseName,
    Guid? FacilityId, string? FacilityName, string? LocationNote,
    DateOnly? PurchaseDate, decimal? PurchaseCost, DateOnly? WarrantyUntil,
    AssetStatus Status, AssetCondition Condition, string? Notes,
    int OpenJobs, int OverdueSchedules,
    IReadOnlyList<AssetScheduleDto> Schedules,
    IReadOnlyList<AssetJobDto> Jobs,
    IReadOnlyList<AssetAmcDto> AmcContracts);

public record CreateAssetRequest(
    string AssetTag, string Name, AssetCategory Category,
    string? Model, string? SerialNumber, string? Manufacturer,
    Guid? WarehouseId, Guid? FacilityId, string? LocationNote,
    DateOnly? PurchaseDate, decimal? PurchaseCost, DateOnly? WarrantyUntil,
    AssetCondition Condition, string? Notes);

public record UpdateAssetStatusRequest(AssetStatus Status, AssetCondition? Condition, string? Notes);

public record CreateScheduleRequest(string TaskDescription, int FrequencyDays, DateOnly? LastServiceDate);

public record LogBreakdownRequest(string Description, string? AssignedTo);

public record CreatePpmJobRequest(Guid? ScheduleId, string Description, string? AssignedTo);

public record CompleteJobRequest(string Resolution, decimal? Cost);

public record CreateAmcRequest(
    string ContractNumber, string VendorName,
    DateOnly StartDate, DateOnly EndDate, decimal AnnualCost, string? Coverage);

public record MaintenanceDueRow(
    Guid AssetId, string AssetTag, string AssetName, AssetCategory Category,
    Guid ScheduleId, string TaskDescription, DateOnly NextDueDate, int DaysToDue,
    string? LocationName);

public record AssetKpiDto(
    int TotalAssets, int ActiveAssets, int UnderMaintenance, int InBreakdown,
    int Condemned, int OpenJobs, int OverduePpm, int AmcExpiring60Days);
