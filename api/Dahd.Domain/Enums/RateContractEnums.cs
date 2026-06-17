namespace Dahd.Domain.Enums;

public enum RateContractStatus
{
    Draft = 1,
    Active = 2,
    Expired = 3,
    Cancelled = 4
}

public enum RateContractCategory
{
    Medicines = 1,
    Vaccines = 2,
    Equipment = 3,
    ColdChain = 4,
    LabConsumables = 5,
    AiConsumables = 6,
    Services = 7,
    Other = 99
}
