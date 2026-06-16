using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Warehouse : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WarehouseType Type { get; set; }
    public Guid? ParentWarehouseId { get; set; }
    public Warehouse? ParentWarehouse { get; set; }
    public string? DivisionName { get; set; }
    public string? DistrictName { get; set; }
    public string? Address { get; set; }
    public bool ColdChainCapable { get; set; }
    public string? InchargeName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}
