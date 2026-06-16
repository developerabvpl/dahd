namespace Dahd.Domain.Enums;

public enum WarehouseType
{
    Central = 1,
    Divisional = 2,
    District = 3,
    Facility = 4
}

public enum FacilityType
{
    VeterinaryHospital = 1,
    Polyclinic = 2,
    RuralDispensary = 3,
    AiSubCentre = 4,
    MobileVeterinaryUnit = 5,
    Gaushala = 6
}

public enum BatchStatus
{
    InTransit = 1,
    InStore = 2,
    Issued = 3,
    Expired = 4,
    Wasted = 5,
    Recalled = 6
}

public enum IndentStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Issued = 4,
    Received = 5,
    Closed = 6,
    Rejected = 7
}

public enum FormularyClass
{
    Antibiotic = 1,
    Antiparasitic = 2,
    Vaccine = 3,
    Vitamin = 4,
    Hormone = 5,
    Mineral = 6,
    Analgesic = 7,
    Anaesthetic = 8,
    Antiseptic = 9,
    Other = 10
}

public enum AnimalSpecies
{
    Cattle = 1,
    Buffalo = 2,
    Sheep = 3,
    Goat = 4,
    Pig = 5,
    Poultry = 6,
    Equine = 7,
    Other = 99
}
