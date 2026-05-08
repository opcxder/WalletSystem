

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        Task AddAsync(Wallet wallet, CancellationToken ct = default);

        Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct = default);

        Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task<Wallet?> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default);
        Task<Wallet?> GetByWalletIdForUpdateAsync(Guid walletId, CancellationToken ct = default);
    }
}
