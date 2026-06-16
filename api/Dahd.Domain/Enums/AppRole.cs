namespace Dahd.Domain.Enums;

public enum AppRole
{
    Admin = 1,
    Director = 2,
    Cvo = 3,
    WarehouseIncharge = 4,
    FacilityVet = 5,
    MvuVet = 6,
    Readonly = 7
}

public static class AppRoles
{
    public const string Admin = nameof(AppRole.Admin);
    public const string Director = nameof(AppRole.Director);
    public const string Cvo = nameof(AppRole.Cvo);
    public const string WarehouseIncharge = nameof(AppRole.WarehouseIncharge);
    public const string FacilityVet = nameof(AppRole.FacilityVet);
    public const string MvuVet = nameof(AppRole.MvuVet);
    public const string Readonly = nameof(AppRole.Readonly);

    public const string ManageMasterData = $"{Admin},{Director}";
    public const string ApproveIndents = $"{Admin},{Director},{Cvo}";
    public const string IssueOrReceive = $"{Admin},{Director},{Cvo},{WarehouseIncharge}";
    public const string Dispense = $"{Admin},{FacilityVet},{MvuVet}";
    public const string AnyAuthenticated = $"{Admin},{Director},{Cvo},{WarehouseIncharge},{FacilityVet},{MvuVet},{Readonly}";
}
