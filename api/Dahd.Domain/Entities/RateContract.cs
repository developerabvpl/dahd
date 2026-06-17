using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class RateContract : Entity
{
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RateContractCategory Category { get; set; }
    public string LeadBody { get; set; } = "AHD";
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidUntil { get; set; }
    public RateContractStatus Status { get; set; } = RateContractStatus.Active;
    public string? SourceUrl { get; set; }
    public string? Notes { get; set; }
    public List<RateContractItem> Items { get; set; } = new();
}

public class RateContractItem : Entity
{
    public Guid RateContractId { get; set; }
    public RateContract RateContract { get; set; } = default!;
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string? VendorName { get; set; }
    public decimal UnitRate { get; set; }
    public string? PackSize { get; set; }
    public int? MinOrderQuantity { get; set; }
    public string? Remarks { get; set; }
}
