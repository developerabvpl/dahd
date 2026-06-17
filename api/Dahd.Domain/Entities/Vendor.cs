using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class Vendor : Entity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;

    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string? UdyamRegNumber { get; set; }
    public bool IsManufacturer { get; set; }
    public bool IsMsme { get; set; }
    public VendorCategory Categories { get; set; }

    public VendorStatus Status { get; set; } = VendorStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UnderReviewAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime? ScheduledInspectionAt { get; set; }
    public string? InspectionRemarks { get; set; }
    public string? BlacklistReason { get; set; }
    public DateOnly? EmpanelmentValidUntil { get; set; }

    public List<VendorDocument> Documents { get; set; } = new();
}

public class VendorDocument : Entity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = default!;
    public VendorDocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? StorageRef { get; set; }
    public string? IssuingAuthority { get; set; }
    public string? CertificateNumber { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? UploadedBy { get; set; }
}
