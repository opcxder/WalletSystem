

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface ILinkedBankAccountRepository
    {
        Task AddAsync(LinkedBankAccount account, CancellationToken ct = default);
        Task<LinkedBankAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task<LinkedBankAccount?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct = default);
    }
}
