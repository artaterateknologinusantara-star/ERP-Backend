using SynteraERP.Api.DTOs.Auth;

namespace SynteraERP.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
