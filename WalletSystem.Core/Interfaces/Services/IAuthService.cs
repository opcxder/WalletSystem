

using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Auth;

namespace WalletSystem.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<RegisterResponse>>  RegisterAsync(RegisterRequest request);
        Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);

        Task<ServiceResult> VerifyEmailAsync(string token);

        Task<ServiceResult> ResendVerificationEmailAsync(string email);

    }
}
