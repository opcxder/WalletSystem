
using WalletSystem.Core.common;

using WalletSystem.Core.DTOs.Wallet;


namespace WalletSystem.Core.Interfaces.Services
{
    public interface IWalletService
    {
        Task<ServiceResult<WalletResponse>> CreateWalletAsync(Guid userId, CreateWalletRequest request, CancellationToken ct = default); 

        Task<ServiceResult<WalletBalanceResponse>> GetBalanceAsync(Guid userId, CancellationToken ct = default);

        Task<ServiceResult<WalletResponse>> GetWalletByUserIdAsync(Guid userId, CancellationToken ct = default);
    }
}
