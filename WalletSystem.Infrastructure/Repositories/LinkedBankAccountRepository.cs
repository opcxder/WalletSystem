

using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{
    public class LinkedBankAccountRepository : ILinkedBankAccountRepository
    {
        private readonly WalletContext _walletContext;
        public LinkedBankAccountRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }
        public async Task AddAsync(LinkedBankAccount account , CancellationToken ct = default)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account), "Empty Input");
            }
            if (account.UserId == Guid.Empty || account.ExternalBankAccountId == Guid.Empty)
                throw new ArgumentException("Invalid account data");

            await _walletContext.LinkedBankAccounts.AddAsync(account, ct);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty) { throw new ArgumentException("UserId cannot be empty", nameof(userId)); }

            return await _walletContext.LinkedBankAccounts.AnyAsync(l => l.UserId == userId && !l.IsDeleted, ct);
        }

        public async Task<LinkedBankAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("UserId cannot be empty", nameof(userId));
            }
            return await _walletContext.LinkedBankAccounts.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId && !l.IsDeleted, ct);
        }
    }
}
