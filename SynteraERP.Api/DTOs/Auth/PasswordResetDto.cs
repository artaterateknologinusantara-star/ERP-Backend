using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    // Dev-only: no email infrastructure exists yet, so the reset token is handed back directly
    // instead of being emailed. Null when the email doesn't match an active account.
    public string? ResetToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}
