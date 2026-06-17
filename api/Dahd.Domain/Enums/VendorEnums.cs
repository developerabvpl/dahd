namespace Dahd.Domain.Enums;

public enum VendorStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Blacklisted = 6
}

[Flags]
public enum VendorCategory
{
    None        = 0,
    Drugs       = 1 << 0,
    Vaccines    = 1 << 1,
    Equipment   = 1 << 2,
    ColdChain   = 1 << 3,
    LabConsumables = 1 << 4,
    AiConsumables  = 1 << 5,
    MvuServices    = 1 << 6,
    GaushalaCapex  = 1 << 7,
    BiogasEpc      = 1 << 8,
    ItServices     = 1 << 9
}

public enum VendorDocumentType
{
    DrugLicence = 1,
    Gmp = 2,
    Iso9001 = 3,
    Iso13485 = 4,
    Bis = 5,
    Cdsco = 6,
    ManufacturerAuthorization = 7,
    PastPerformance = 8,
    Gst = 9,
    Pan = 10,
    Udyam = 11,
    Other = 99
}
