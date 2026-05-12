
using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{

    public class TransactionRepository : ITransactionRepository
    {

        private readonly WalletContext _walletContext;

        public TransactionRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }

        public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction), "Invlaid Input");
            }
            await _walletContext.AddAsync(transaction, ct);
        }

        public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                throw new ArgumentNullException(nameof(idempotencyKey), "Invalid Input");
            }
            return await _walletContext.Transactions.AsNoTracking()
                             .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
        }

        public async Task<Transaction?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default)
        {
            ValidateId(transactionId);
            return await _walletContext.Transactions
                        .AsNoTracking().FirstOrDefaultAsync(t => t.TransactionId == transactionId, ct);
        }

        public async Task<List<Transaction>> GetTransactionsForUserAsync(Guid userId, CancellationToken ct = default)
        {
            ValidateId(userId);

            var walletId = await _walletContext.Wallets
                .AsNoTracking()
                .Where(w => w.UserId == userId)
                .Select(w => w.WalletId)
                .FirstOrDefaultAsync(ct);

            if (walletId == Guid.Empty)
                return [];

            return await _walletContext.Transactions
                .AsNoTracking()
                .Where(t => t.SourceWalletId == walletId || t.DestinationWalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }



        public async Task<Transaction?> GetByTransactionIdForUserAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
        {
            ValidateId(userId);
            ValidateId(transactionId);
            var walletId = await _walletContext.Wallets
                                   .AsNoTracking()
                                   .Where(w => w.UserId == userId)
                                   .Select(w => w.WalletId)
                                   .FirstOrDefaultAsync(ct);

            if (walletId == Guid.Empty)
                return null;

            return await _walletContext.Transactions
                         .AsNoTracking()
                         .FirstOrDefaultAsync(t =>
                              t.TransactionId == transactionId &&
                             (t.SourceWalletId == walletId || t.DestinationWalletId == walletId), ct);

        }



        public async Task<Transaction?> GetAddMoneyByIdempotencyAsync(Guid destinationWalletId, Guid sourceBankAccountId,
                     string idempotencyKey, CancellationToken ct = default)
        {
            ValidateId(destinationWalletId);
            ValidateId(sourceBankAccountId);

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentNullException(nameof(idempotencyKey));

            return await _walletContext.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.Type == Core.Enums.TransactionType.AddMoney &&
                    t.DestinationWalletId == destinationWalletId &&
                    t.SourceBankAccountId == sourceBankAccountId &&
                    t.IdempotencyKey == idempotencyKey,
                    ct);
        }



        private static void ValidateId(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));
        }
    }
}
