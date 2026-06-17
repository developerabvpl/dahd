using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class ProcurementCampaign : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SchemeBucket Scheme { get; set; }
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = default!;
    public DateOnly WindowStart { get; set; }
    public DateOnly WindowEnd { get; set; }
    public int LeadDays { get; set; } = 60;
    public decimal TargetDoseCount { get; set; }
    public string? TargetCohortDescription { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Planned;
    public string? Notes { get; set; }
    public DateTime? IndentsDraftedAt { get; set; }
    public int IndentsDraftedCount { get; set; }
}
