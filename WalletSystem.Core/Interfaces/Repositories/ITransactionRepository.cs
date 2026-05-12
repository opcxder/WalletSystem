

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
        Task<Transaction?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);

        Task<List<Transaction>> GetTransactionsForUserAsync(Guid userId, CancellationToken ct = default);

        Task AddAsync(Transaction transaction, CancellationToken ct = default);

        Task<Transaction?> GetByTransactionIdForUserAsync(Guid userId, Guid transactionId, CancellationToken ct = default);

        Task<Transaction?> GetAddMoneyByIdempotencyAsync(Guid destinationWalletId, Guid sourceBankAccountId, string idempotencyKey, CancellationToken ct = default);

    }
}
