using Dahd.Domain.Common;
using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

public class AppUser : Entity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public AppRole Role { get; set; } = AppRole.Readonly;
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}

public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
