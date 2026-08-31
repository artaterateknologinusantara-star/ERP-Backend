using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    // SHA-256 hash of the raw token — the raw token itself is never persisted, only handed to the
    // caller once at creation time, same principle as a password hash.
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public User User { get; set; } = null!;
}
