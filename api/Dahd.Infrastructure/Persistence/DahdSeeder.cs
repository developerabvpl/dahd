using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dahd.Infrastructure.Persistence;

public static class DahdSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var db = sp.GetRequiredService<DahdDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

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

        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.AddRange(BuildUsers(hasher));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ProcurementCampaigns.AnyAsync(ct))
        {
            var fmd = await db.Drugs.FirstOrDefaultAsync(d => d.Code == "FMD-VAX", ct);
            var bru = await db.Drugs.FirstOrDefaultAsync(d => d.Code == "BRU-S19", ct);
            if (fmd is not null && bru is not null)
            {
                var thisYear = DateOnly.FromDateTime(DateTime.UtcNow).Year;
                db.ProcurementCampaigns.AddRange(BuildCampaigns(fmd.Id, bru.Id, thisYear));
                await db.SaveChangesAsync(ct);
            }
        }

        if (!await db.Batches.AnyAsync(ct))
        {
            await SeedOperationalDataAsync(db, ct);
        }

        if (!await db.RateContracts.AnyAsync(ct))
        {
            await SeedRateContractsAsync(db, ct);
        }
    }

    private static async Task SeedRateContractsAsync(DahdDbContext db, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var drugs = await db.Drugs.ToListAsync(ct);

        var medsRc = new RateContract
        {
            ContractNumber = $"AHD-MED-{today.Year}",
            Title = "Veterinary Medicines / Vitamins / Hormones / Minerals — UP AHD Rate Contract",
            Category = RateContractCategory.Medicines,
            LeadBody = "AHD",
            ValidFrom = today.AddMonths(-3),
            ValidUntil = today.AddMonths(9),
            Status = RateContractStatus.Active,
            SourceUrl = "https://animalhusb.upsdc.gov.in/en/approved-rate-list",
            Notes = "Imported from the published AHD Approved Rate List. Re-fetch annually."
        };
        var vacRc = new RateContract
        {
            ContractNumber = $"AHD-VAC-{today.Year}",
            Title = "Veterinary Vaccines — ASCAD + State Share Rate Contract",
            Category = RateContractCategory.Vaccines,
            LeadBody = "AHD",
            ValidFrom = today.AddMonths(-3),
            ValidUntil = today.AddMonths(9),
            Status = RateContractStatus.Active,
            SourceUrl = "https://animalhusb.upsdc.gov.in/en/approved-rate-list",
            Notes = "Covers ASCAD basket (HS, BQ, CSF, anti-rabies, Ranikhet/NCD, Gumboro, Fowl Pox)."
        };
        var equipRc = new RateContract
        {
            ContractNumber = $"AHD-EQP-{today.Year}",
            Title = "Veterinary Equipments / Machineries & Instruments — UP AHD Rate Contract",
            Category = RateContractCategory.Equipment,
            LeadBody = "AHD",
            ValidFrom = today.AddMonths(-2),
            ValidUntil = today.AddMonths(10),
            Status = RateContractStatus.Active,
            SourceUrl = "https://animalhusb.upsdc.gov.in/en/approved-rate-list",
            Notes = "AI guns, LN2 containers, autoclaves, microscopes, surgical instruments, cold-chain cabinets."
        };
        db.RateContracts.AddRange(medsRc, vacRc, equipRc);
        await db.SaveChangesAsync(ct);

        // Item lines for medicines RC
        (string Code, decimal Rate, string Pack, string Vendor)[] medsLines =
        {
            ("OXY-50", 145m, "50ml vial",   "Cipla Vet"),
            ("ENRO-100", 220m, "100ml vial","Pfizer Animal Health"),
            ("IVERM", 165m, "50ml vial",    "Virbac India"),
            ("ALBEND-1L", 320m, "1L bottle","Indian Immunologicals Ltd"),
            ("CAL-INJ", 95m, "450ml bottle","Cipla Vet"),
            ("BCMP-100", 105m, "100ml vial","Cipla Vet"),
            ("OXY-20", 65m, "10IU ampoule", "Pfizer Animal Health"),
            ("MELOX-50", 285m, "50ml vial", "Virbac India"),
            ("XYL-30", 410m, "30ml vial",   "Pfizer Animal Health"),
            ("POVID-1L", 175m, "1L bottle", "Cipla Vet")
        };
        foreach (var (code, rate, pack, vendor) in medsLines)
        {
            var d = drugs.FirstOrDefault(x => x.Code == code);
            if (d is null) continue;
            db.RateContractItems.Add(new RateContractItem
            {
                RateContractId = medsRc.Id, DrugId = d.Id,
                VendorName = vendor, UnitRate = rate, PackSize = pack,
                MinOrderQuantity = 100
            });
        }

        (string Code, decimal Rate, string Pack, string Vendor)[] vacLines =
        {
            ("FMD-VAX",  10.5m, "10-dose vial", "Indian Immunologicals Ltd"),
            ("BRU-S19",  18.0m, "10-dose vial", "Indian Immunologicals Ltd"),
            ("HS-VAX",    8.5m, "20-dose vial", "Hester Biosciences"),
            ("BQ-VAX",    7.8m, "20-dose vial", "Hester Biosciences"),
            ("CSF-VAX",  12.0m, "10-dose vial", "Indian Immunologicals Ltd"),
            ("RAB-VAX",  24.0m, "1ml vial",     "Indian Immunologicals Ltd"),
            ("NCD-VAX",   3.2m, "100-dose vial","Hester Biosciences"),
            ("IBD-VAX",   3.5m, "100-dose vial","Hester Biosciences"),
            ("FPV-VAX",   4.0m, "100-dose vial","Hester Biosciences")
        };
        foreach (var (code, rate, pack, vendor) in vacLines)
        {
            var d = drugs.FirstOrDefault(x => x.Code == code);
            if (d is null) continue;
            db.RateContractItems.Add(new RateContractItem
            {
                RateContractId = vacRc.Id, DrugId = d.Id,
                VendorName = vendor, UnitRate = rate, PackSize = pack,
                MinOrderQuantity = 1000,
                Remarks = "ASCAD / NADCP eligible"
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedOperationalDataAsync(DahdDbContext db, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var central = await db.Warehouses.FirstAsync(w => w.Code == "WH-CMS", ct);
        var divisions = await db.Warehouses.Where(w => w.Type == WarehouseType.Divisional).ToListAsync(ct);
        var facilities = await db.Facilities.ToListAsync(ct);
        var drugs = await db.Drugs.ToListAsync(ct);

        // 1) Stock batches at the central + each divisional warehouse for the main vaccines + 2 antibiotics
        var seedDrugs = drugs.Where(d =>
            d.Code is "FMD-VAX" or "BRU-S19" or "HS-VAX" or "RAB-VAX" or "OXY-50" or "IVERM" or "CAL-INJ" or "BCMP-100").ToList();

        var batchSeed = new List<Batch>();
        var rngLike = 0;
        foreach (var drug in seedDrugs)
        {
            // Central warehouse: large batch with comfortable expiry
            batchSeed.Add(new Batch
            {
                DrugId = drug.Id,
                BatchNumber = $"{drug.Code}-CMS-{today:yyyyMM}",
                ManufactureDate = today.AddMonths(-6),
                ExpiryDate = today.AddMonths(18),
                Manufacturer = drug.IsVaccine ? "Indian Immunologicals Ltd" : "Cipla Vet",
                Quantity = drug.IsVaccine ? 50_000m : 2_000m,
                UnitCost = drug.IsVaccine ? 12m : 145m,
                CurrentWarehouseId = central.Id,
                Status = BatchStatus.InStore,
                PurchaseOrderRef = "PO-DEMO-2026-01"
            });

            // Divisional warehouses: smaller batches; throw in some near-expiry to power Redistribution suggestions
            for (int i = 0; i < divisions.Count; i++)
            {
                var div = divisions[i];
                var isNearExpiry = (rngLike + i) % 3 == 0 && drug.IsVaccine;
                batchSeed.Add(new Batch
                {
                    DrugId = drug.Id,
                    BatchNumber = $"{drug.Code}-{div.Code.Replace("WH-DIV-", string.Empty)}-{today:yyyyMM}",
                    ManufactureDate = today.AddMonths(-8),
                    ExpiryDate = isNearExpiry ? today.AddDays(25 + (i * 7)) : today.AddMonths(12),
                    Manufacturer = drug.IsVaccine ? "Indian Immunologicals Ltd" : "Cipla Vet",
                    Quantity = drug.IsVaccine ? 4_000m + (i * 500m) : 250m,
                    UnitCost = drug.IsVaccine ? 12m : 145m,
                    CurrentWarehouseId = div.Id,
                    Status = BatchStatus.InStore,
                    PurchaseOrderRef = "PO-DEMO-2026-01"
                });
            }
            rngLike++;
        }
        db.Batches.AddRange(batchSeed);
        await db.SaveChangesAsync(ct);

        // 2) Dispense events over the last 30 days so consumption + dashboards have data
        var mvuOrHospital = facilities
            .Where(f => f.Type is FacilityType.MobileVeterinaryUnit
                          or FacilityType.VeterinaryHospital
                          or FacilityType.RuralDispensary)
            .ToList();

        if (mvuOrHospital.Count > 0)
        {
            var dispensableBatches = batchSeed.Where(b => !drugs.First(d => d.Id == b.DrugId).IsVaccine
                                                     || b.CurrentWarehouseId != central.Id)
                                              .ToList();

            var dispenses = new List<DispenseEvent>();
            var batchCursor = 0;
            for (int day = 30; day >= 1; day--)
            {
                for (int e = 0; e < 4; e++)
                {
                    var batch = dispensableBatches[(batchCursor++) % dispensableBatches.Count];
                    var facility = mvuOrHospital[(batchCursor + day) % mvuOrHospital.Count];
                    var qty = 1 + ((batchCursor + day) % 5);
                    if (batch.Quantity < qty) continue;
                    batch.Quantity -= qty;
                    dispenses.Add(new DispenseEvent
                    {
                        BatchId = batch.Id,
                        Quantity = qty,
                        FacilityId = facility.Id,
                        AnimalEarTag = $"UP-{(day * 100 + e):00000}",
                        AnimalSpecies = (AnimalSpecies)((batchCursor % 6) + 1),
                        OwnerName = $"Farmer {day}-{e}",
                        OwnerMobile = $"9{(700_000_000 + batchCursor):D9}",
                        Diagnosis = batchCursor % 3 == 0 ? "Routine vaccination"
                                  : batchCursor % 3 == 1 ? "Tick infestation"
                                                         : "Lameness",
                        VetName = facility.Type == FacilityType.MobileVeterinaryUnit ? "MVU Vet" : "Facility Vet",
                        DispensedAt = now.AddDays(-day).AddHours((e * 3) % 12)
                    });
                }
            }
            db.DispenseEvents.AddRange(dispenses);
        }

        // 3) Cold-chain readings: 7 days × 4 readings/day per cold-chain warehouse, with a few breaches
        var coldDevices = new[]
        {
            (Warehouse: central, DeviceId: "ILR-01", DeviceName: "Walk-in ILR (Central)"),
        }.Concat(divisions.Select((d, i) => (Warehouse: d, DeviceId: $"ILR-D{i + 1:00}", DeviceName: $"ILR {d.Code}")))
         .ToList();

        var ccLogs = new List<ColdChainLog>();
        for (int day = 7; day >= 1; day--)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                var hour = slot * 6;
                foreach (var (wh, devId, devName) in coldDevices)
                {
                    var baseT = 4.5m + ((slot - 1) * 0.5m);
                    var isBreach = (day == 3 && slot == 0 && wh.Code == "WH-DIV-GKP")
                                || (day == 1 && slot == 3 && wh.Code == "WH-DIV-MRT");
                    var t = isBreach ? 11.2m : baseT;
                    ccLogs.Add(new ColdChainLog
                    {
                        WarehouseId = wh.Id,
                        DeviceId = devId,
                        DeviceName = devName,
                        ReadingAt = now.AddDays(-day).AddHours(hour),
                        TemperatureCelsius = t,
                        IsBreach = t < 2m || t > 8m,
                        Remarks = isBreach ? "Auto-detected — pending acknowledgement" : null
                    });
                }
            }
        }
        db.ColdChainLogs.AddRange(ccLogs);

        // 4) One Draft indent illustrating the supply-chain flow
        var firstFacility = facilities.FirstOrDefault();
        var firstDivision = divisions.FirstOrDefault();
        var fmdDrug = drugs.FirstOrDefault(d => d.Code == "FMD-VAX");
        if (firstFacility is not null && firstDivision is not null && fmdDrug is not null)
        {
            db.Indents.Add(new Indent
            {
                IndentNumber = $"IND-DEMO-{now:yyyyMMddHHmm}",
                RaisedByWarehouseId = firstDivision.Id,
                FulfilledByWarehouseId = central.Id,
                Status = IndentStatus.Submitted,
                SubmittedAt = now.AddDays(-1),
                Remarks = "Demo indent — ready for CVO approval.",
                Lines = new List<IndentLine>
                {
                    new() { DrugId = fmdDrug.Id, RequestedQuantity = 2_000m, Remarks = "Top-up for upcoming round" }
                }
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<ProcurementCampaign> BuildCampaigns(Guid fmdId, Guid bruId, int baseYear)
    {
        // NADCP demand map anchored in research (docs/02): UP runs ~520.36 lakh
        // FMD doses per round, twice a year — Sept-Oct and Mar-Apr 45-day windows.
        // Brucella: annual cohort, 4-8 month female calves, once-in-a-lifetime.
        var fmdDoses = 52_036_000m;
        var bruCohort = 1_800_000m;

        ProcurementCampaign fmd(int year, bool fall, string code) => new()
        {
            Code = code,
            Name = $"FMD vaccination round {(fall ? "Sept–Oct" : "Mar–Apr")} {year}",
            Scheme = SchemeBucket.NadcpFmd,
            DrugId = fmdId,
            WindowStart = new DateOnly(year, fall ? 9 : 3, 1),
            WindowEnd = new DateOnly(year, fall ? 10 : 4, 15),
            LeadDays = 90,
            TargetDoseCount = fmdDoses,
            TargetCohortDescription = "100% of cattle, buffalo, sheep, goat and pig populations",
            Status = CampaignStatus.Planned,
            Notes = "NADCP biannual; central-funded; cold-chain cabinets procured by state alongside vaccine."
        };

        ProcurementCampaign brucella(int year) => new()
        {
            Code = $"BRU-{year}",
            Name = $"Brucellosis annual cohort {year}",
            Scheme = SchemeBucket.NadcpBrucellosis,
            DrugId = bruId,
            WindowStart = new DateOnly(year, 1, 1),
            WindowEnd = new DateOnly(year, 12, 31),
            LeadDays = 60,
            TargetDoseCount = bruCohort,
            TargetCohortDescription = "100% female bovine calves aged 4–8 months (Brucella abortus S19), once-in-lifetime.",
            Status = CampaignStatus.Planned,
            Notes = "Cohort-based annual renewal under NADCP."
        };

        return
        [
            fmd(baseYear, fall: false, $"FMD-{baseYear}-SPRING"),
            fmd(baseYear, fall: true,  $"FMD-{baseYear}-FALL"),
            fmd(baseYear + 1, fall: false, $"FMD-{baseYear + 1}-SPRING"),
            fmd(baseYear + 1, fall: true,  $"FMD-{baseYear + 1}-FALL"),
            brucella(baseYear),
            brucella(baseYear + 1)
        ];
    }

    private static List<AppUser> BuildUsers(IPasswordHasher hasher) =>
    [
        new() { Username = "admin",     DisplayName = "System Administrator", Role = AppRole.Admin,             PasswordHash = hasher.Hash("admin123") },
        new() { Username = "director",  DisplayName = "Director AHD (Demo)",   Role = AppRole.Director,          PasswordHash = hasher.Hash("director123") },
        new() { Username = "cvo",       DisplayName = "CVO Lucknow (Demo)",    Role = AppRole.Cvo,               PasswordHash = hasher.Hash("cvo123") },
        new() { Username = "wh",        DisplayName = "Warehouse In-Charge",   Role = AppRole.WarehouseIncharge, PasswordHash = hasher.Hash("wh123") },
        new() { Username = "vet",       DisplayName = "Facility Veterinarian", Role = AppRole.FacilityVet,       PasswordHash = hasher.Hash("vet123") },
        new() { Username = "mvuvet",    DisplayName = "MVU Veterinarian",      Role = AppRole.MvuVet,            PasswordHash = hasher.Hash("mvu123") },
        new() { Username = "vendor1",   DisplayName = "Demo Vendor (Pre-empanelled)", Role = AppRole.Vendor,     PasswordHash = hasher.Hash("vendor123") }
    ];

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
