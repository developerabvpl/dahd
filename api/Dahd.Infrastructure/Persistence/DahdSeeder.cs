using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Infrastructure.Persistence;

public static class DahdSeeder
{
    public static async Task SeedAsync(DahdDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Drugs.AnyAsync(ct))
        {
            db.Drugs.AddRange(BuildDrugs());
        }

        if (!await db.Warehouses.AnyAsync(ct))
        {
            db.Warehouses.AddRange(BuildWarehouses());
        }

        if (!await db.Facilities.AnyAsync(ct))
        {
            db.Facilities.AddRange(BuildFacilities());
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<Drug> BuildDrugs() =>
    [
        Vaccine("FMD-VAX", "Foot-and-Mouth Disease Vaccine", "Polyvalent FMD"),
        Vaccine("BRU-S19", "Brucella abortus S19 Vaccine", "Brucella S19"),
        Vaccine("HS-VAX",  "Haemorrhagic Septicaemia Vaccine", "HS oil-adjuvant"),
        Vaccine("BQ-VAX",  "Black Quarter Vaccine", "BQ formalin-killed"),
        Vaccine("CSF-VAX", "Classical Swine Fever Vaccine", "CSF lapinised"),
        Vaccine("RAB-VAX", "Anti-Rabies Vaccine", "Rabies cell culture"),
        Vaccine("NCD-VAX", "Ranikhet (NCD) Vaccine", "Newcastle Disease F/Lasota"),
        Vaccine("IBD-VAX", "Infectious Bursal Disease (Gumboro) Vaccine", "Gumboro intermediate"),
        Vaccine("FPV-VAX", "Fowl Pox Vaccine", "Fowl Pox live"),
        Drug("OXY-50", "Oxytetracycline LA Injection 50ml", "Antibiotic", FormularyClass.Antibiotic, "Vial"),
        Drug("ENRO-100", "Enrofloxacin 10% Injection 100ml", "Antibiotic", FormularyClass.Antibiotic, "Vial"),
        Drug("IVERM", "Ivermectin Injection 50ml", "Antiparasitic", FormularyClass.Antiparasitic, "Vial"),
        Drug("ALBEND-1L", "Albendazole 10% Oral Suspension 1L", "Antiparasitic", FormularyClass.Antiparasitic, "Bottle"),
        Drug("CAL-INJ", "Calcium Borogluconate Injection 450ml", "Calcium", FormularyClass.Mineral, "Bottle"),
        Drug("BCMP-100", "B-Complex Injection 100ml", "Vitamin B-complex", FormularyClass.Vitamin, "Vial"),
        Drug("OXY-20", "Oxytocin Injection 10IU", "Hormone", FormularyClass.Hormone, "Ampoule"),
        Drug("MELOX-50", "Meloxicam Injection 50ml", "NSAID", FormularyClass.Analgesic, "Vial"),
        Drug("XYL-30",  "Xylazine Injection 30ml", "Sedative", FormularyClass.Anaesthetic, "Vial"),
        Drug("POVID-1L", "Povidone Iodine 5% 1L", "Antiseptic", FormularyClass.Antiseptic, "Bottle")
    ];

    private static Drug Vaccine(string code, string name, string generic) => new()
    {
        Code = code,
        Name = name,
        GenericName = generic,
        FormularyClass = FormularyClass.Vaccine,
        IsVaccine = true,
        ColdChainRequired = true,
        StorageTempMinCelsius = 2m,
        StorageTempMaxCelsius = 8m,
        UnitOfMeasure = "Dose",
        ScheduleClass = "Biological"
    };

    private static Drug Drug(string code, string name, string generic, FormularyClass cls, string uom) => new()
    {
        Code = code,
        Name = name,
        GenericName = generic,
        FormularyClass = cls,
        IsVaccine = false,
        ColdChainRequired = false,
        UnitOfMeasure = uom,
        ScheduleClass = "H"
    };

    private static List<Warehouse> BuildWarehouses()
    {
        var central = new Warehouse
        {
            Code = "WH-CMS",
            Name = "Central Medical Store - Lucknow",
            Type = WarehouseType.Central,
            DivisionName = "Lucknow",
            DistrictName = "Lucknow",
            Address = "AHD Directorate, Lucknow",
            ColdChainCapable = true,
            InchargeName = "CMS In-Charge",
            ContactPhone = "0522-2740482"
        };

        var divisions = new[]
        {
            ("WH-DIV-LKO", "Divisional Store - Lucknow", "Lucknow"),
            ("WH-DIV-MRT", "Divisional Store - Meerut", "Meerut"),
            ("WH-DIV-VNS", "Divisional Store - Varanasi", "Varanasi"),
            ("WH-DIV-GKP", "Divisional Store - Gorakhpur", "Gorakhpur"),
            ("WH-DIV-AGR", "Divisional Store - Agra", "Agra")
        }.Select(d => new Warehouse
        {
            Code = d.Item1,
            Name = d.Item2,
            Type = WarehouseType.Divisional,
            DivisionName = d.Item3,
            ParentWarehouseId = central.Id,
            ColdChainCapable = true
        }).ToList();

        return new List<Warehouse> { central }.Concat(divisions).ToList();
    }

    private static List<Facility> BuildFacilities() =>
    [
        new() { Code = "HOSP-LKO-01", Name = "Veterinary Hospital - Lucknow Central", Type = FacilityType.VeterinaryHospital, DivisionName = "Lucknow", DistrictName = "Lucknow" },
        new() { Code = "DISP-LKO-01", Name = "Rural Dispensary - Mohanlalganj", Type = FacilityType.RuralDispensary, DivisionName = "Lucknow", DistrictName = "Lucknow", BlockName = "Mohanlalganj" },
        new() { Code = "MVU-LKO-01", Name = "MVU - Lucknow Zone 1", Type = FacilityType.MobileVeterinaryUnit, DivisionName = "Lucknow", DistrictName = "Lucknow", MvuVehicleRegistration = "UP32-MV-0001" },
        new() { Code = "MVU-MRT-01", Name = "MVU - Meerut Zone 1", Type = FacilityType.MobileVeterinaryUnit, DivisionName = "Meerut", DistrictName = "Meerut", MvuVehicleRegistration = "UP15-MV-0001" },
        new() { Code = "AISC-LKO-01", Name = "AI Sub-Centre - Bakshi Ka Talab", Type = FacilityType.AiSubCentre, DivisionName = "Lucknow", DistrictName = "Lucknow" },
        new() { Code = "GSL-MTR-01", Name = "Kanha Gaushala - Mathura", Type = FacilityType.Gaushala, DivisionName = "Agra", DistrictName = "Mathura" }
    ];
}
