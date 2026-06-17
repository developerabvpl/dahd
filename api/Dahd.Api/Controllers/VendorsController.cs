using Dahd.Application;
using Dahd.Application.Abstractions;
using Dahd.Domain.Entities;
using Dahd.Domain.Enums;
using Dahd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dahd.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController(
    DahdDbContext db,
    IPasswordHasher hasher,
    IAuditLogger audit,
    ICurrentUser current) : ControllerBase
{
    // --- Self-service (Public + Vendor) ---

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<VendorDto>> Register(
        [FromBody] VendorRegistrationRequest req,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username, ct))
            return Conflict("Username already taken.");
        if (await db.Vendors.AnyAsync(v => v.ContactEmail == req.ContactEmail, ct))
            return Conflict("A vendor with this contact email already exists.");
        if (req.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var user = new AppUser
        {
            Username = req.Username,
            DisplayName = req.LegalName,
            Email = req.ContactEmail,
            Role = AppRole.Vendor,
            PasswordHash = hasher.Hash(req.Password)
        };
        db.Users.Add(user);

        var vendor = new Vendor
        {
            User = user,
            UserId = user.Id,
            LegalName = req.LegalName,
            TradeName = req.TradeName,
            ContactPerson = req.ContactPerson,
            ContactEmail = req.ContactEmail,
            ContactPhone = req.ContactPhone,
            Address = req.Address,
            City = req.City,
            State = req.State,
            Pincode = req.Pincode,
            Gstin = req.Gstin,
            Pan = req.Pan,
            UdyamRegNumber = req.UdyamRegNumber,
            IsManufacturer = req.IsManufacturer,
            IsMsme = req.IsMsme,
            Categories = req.Categories,
            Status = VendorStatus.Draft
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), vendor.Id, "Register",
            after: new { vendor.LegalName, vendor.ContactEmail, Categories = vendor.Categories.ToString() },
            summary: $"Vendor {vendor.LegalName} registered (user {user.Username})", ct: ct);

        return CreatedAtAction(nameof(Me), null, ToDto(vendor, user.Username));
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Vendor)]
    public async Task<ActionResult<VendorDto>> Me(CancellationToken ct)
    {
        var v = await LoadMyVendor(ct);
        return v is null ? NotFound() : Ok(ToDto(v, current.Username));
    }

    [HttpPost("me/documents")]
    [Authorize(Roles = AppRoles.Vendor)]
    public async Task<ActionResult<VendorDocumentDto>> UploadMyDoc(
        [FromBody] UploadVendorDocumentRequest req,
        CancellationToken ct)
    {
        var v = await LoadMyVendor(ct);
        if (v is null) return NotFound();
        if (v.Status is VendorStatus.Approved or VendorStatus.Blacklisted)
            return Conflict("Cannot upload documents for an approved or blacklisted vendor.");

        var doc = new VendorDocument
        {
            VendorId = v.Id,
            DocumentType = req.DocumentType,
            FileName = req.FileName,
            StorageRef = req.StorageRef,
            IssuingAuthority = req.IssuingAuthority,
            CertificateNumber = req.CertificateNumber,
            IssuedDate = req.IssuedDate,
            ExpiryDate = req.ExpiryDate,
            Notes = req.Notes,
            UploadedBy = current.Username
        };
        db.VendorDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(VendorDocument), doc.Id, "Upload",
            after: new { doc.DocumentType, doc.FileName, doc.CertificateNumber },
            summary: $"Vendor {v.LegalName} uploaded {doc.DocumentType} ({doc.FileName})", ct: ct);
        return Ok(ToDocDto(doc));
    }

    [HttpPost("me/submit")]
    [Authorize(Roles = AppRoles.Vendor)]
    public async Task<ActionResult<VendorDto>> SubmitMine(CancellationToken ct)
    {
        var v = await LoadMyVendor(ct);
        if (v is null) return NotFound();
        if (v.Status != VendorStatus.Draft)
            return Conflict($"Cannot submit from status '{v.Status}'.");
        if (v.Documents.Count == 0)
            return BadRequest("Upload at least one supporting document before submitting.");

        v.Status = VendorStatus.Submitted;
        v.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), v.Id, "Transition:Draft->Submitted",
            summary: $"Vendor {v.LegalName} submitted for review", ct: ct);
        return Ok(ToDto(v, current.Username));
    }

    // --- Admin (Director / Admin) ---

    [HttpGet]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> List(
        [FromQuery] VendorStatus? status,
        CancellationToken ct)
    {
        var q = db.Vendors.AsNoTracking().Include(v => v.User).Include(v => v.Documents).AsQueryable();
        if (status.HasValue) q = q.Where(v => v.Status == status);
        var rows = await q.OrderByDescending(v => v.CreatedAt).ToListAsync(ct);
        return Ok(rows.Select(v => ToDto(v, v.User.Username)).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<VendorDto>> GetById(Guid id, CancellationToken ct)
    {
        var v = await db.Vendors.Include(x => x.User).Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null ? NotFound() : Ok(ToDto(v, v.User.Username));
    }

    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<VendorDto>> StartReview(
        Guid id,
        [FromBody] VendorReviewActionRequest? req,
        CancellationToken ct)
    {
        var v = await db.Vendors.Include(x => x.User).Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        if (v.Status != VendorStatus.Submitted) return Conflict($"Expected 'Submitted', got '{v.Status}'.");

        v.Status = VendorStatus.UnderReview;
        v.UnderReviewAt = DateTime.UtcNow;
        v.ReviewedBy = current.Username;
        v.ScheduledInspectionAt = req?.ScheduledInspectionAt;
        v.InspectionRemarks = req?.Remarks;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), v.Id, "Transition:Submitted->UnderReview",
            after: new { v.ReviewedBy, v.ScheduledInspectionAt },
            summary: $"Vendor {v.LegalName} moved to review by {current.Username}", ct: ct);
        return Ok(ToDto(v, v.User.Username));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<VendorDto>> Approve(
        Guid id,
        [FromBody] VendorReviewActionRequest? req,
        CancellationToken ct)
    {
        var v = await db.Vendors.Include(x => x.User).Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        if (v.Status is not (VendorStatus.Submitted or VendorStatus.UnderReview))
            return Conflict($"Expected 'Submitted' or 'UnderReview', got '{v.Status}'.");

        var from = v.Status;
        v.Status = VendorStatus.Approved;
        v.ApprovedAt = DateTime.UtcNow;
        v.ReviewedBy = current.Username;
        v.ReviewRemarks = req?.Remarks;
        v.EmpanelmentValidUntil = req?.EmpanelmentValidUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), v.Id, $"Transition:{from}->Approved",
            after: new { v.ApprovedAt, v.EmpanelmentValidUntil, v.ReviewRemarks },
            summary: $"Vendor {v.LegalName} approved by {current.Username} (valid until {v.EmpanelmentValidUntil})", ct: ct);
        return Ok(ToDto(v, v.User.Username));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<VendorDto>> Reject(
        Guid id,
        [FromBody] VendorReviewActionRequest req,
        CancellationToken ct)
    {
        var v = await db.Vendors.Include(x => x.User).Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        if (v.Status is not (VendorStatus.Submitted or VendorStatus.UnderReview))
            return Conflict($"Expected 'Submitted' or 'UnderReview', got '{v.Status}'.");
        if (string.IsNullOrWhiteSpace(req.Remarks))
            return BadRequest("Rejection remarks are required.");

        var from = v.Status;
        v.Status = VendorStatus.Rejected;
        v.RejectedAt = DateTime.UtcNow;
        v.ReviewedBy = current.Username;
        v.ReviewRemarks = req.Remarks;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), v.Id, $"Transition:{from}->Rejected",
            after: new { v.RejectedAt, v.ReviewRemarks },
            summary: $"Vendor {v.LegalName} rejected by {current.Username}: {v.ReviewRemarks}", ct: ct);
        return Ok(ToDto(v, v.User.Username));
    }

    [HttpPost("{id:guid}/blacklist")]
    [Authorize(Roles = AppRoles.EmpanelmentAdmin)]
    public async Task<ActionResult<VendorDto>> Blacklist(
        Guid id,
        [FromBody] VendorReviewActionRequest req,
        CancellationToken ct)
    {
        var v = await db.Vendors.Include(x => x.User).Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.BlacklistReason))
            return BadRequest("Blacklist reason is required.");

        var from = v.Status;
        v.Status = VendorStatus.Blacklisted;
        v.BlacklistedAt = DateTime.UtcNow;
        v.ReviewedBy = current.Username;
        v.BlacklistReason = req.BlacklistReason;
        v.User.IsActive = false;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(nameof(Vendor), v.Id, $"Transition:{from}->Blacklisted",
            after: new { v.BlacklistedAt, v.BlacklistReason },
            summary: $"Vendor {v.LegalName} blacklisted by {current.Username}: {v.BlacklistReason}", ct: ct);
        return Ok(ToDto(v, v.User.Username));
    }

    // --- Helpers ---

    private async Task<Vendor?> LoadMyVendor(CancellationToken ct)
    {
        if (current.UserId is null) return null;
        return await db.Vendors.Include(v => v.Documents).Include(v => v.User)
            .FirstOrDefaultAsync(v => v.UserId == current.UserId, ct);
    }

    private static VendorDto ToDto(Vendor v, string? username) => new(
        v.Id, v.UserId, username ?? v.User?.Username ?? string.Empty,
        v.LegalName, v.TradeName,
        v.ContactPerson, v.ContactEmail, v.ContactPhone,
        v.City, v.State, v.Pincode,
        v.Gstin, v.Pan, v.UdyamRegNumber,
        v.IsManufacturer, v.IsMsme, v.Categories,
        v.Status,
        v.SubmittedAt, v.UnderReviewAt,
        v.ApprovedAt, v.RejectedAt, v.BlacklistedAt,
        v.ReviewedBy, v.ReviewRemarks,
        v.ScheduledInspectionAt, v.InspectionRemarks,
        v.BlacklistReason, v.EmpanelmentValidUntil,
        v.Documents.Select(ToDocDto).ToList());

    private static VendorDocumentDto ToDocDto(VendorDocument d) => new(
        d.Id, d.DocumentType, d.FileName, d.StorageRef,
        d.IssuingAuthority, d.CertificateNumber,
        d.IssuedDate, d.ExpiryDate, d.Notes,
        d.UploadedAt, d.UploadedBy);
}
