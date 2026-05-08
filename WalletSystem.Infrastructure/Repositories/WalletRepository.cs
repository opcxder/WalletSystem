
using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{

    public class WalletRepository : IWalletRepository
    {
        private readonly WalletContext _walletContext;
        public WalletRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }
        public async Task AddAsync(Wallet wallet, CancellationToken ct = default)
        {

            //if (wallet == null)
            //{
            //    throw new ArgumentNullException(nameof(wallet));
            //}
            ArgumentNullException.ThrowIfNull(wallet);

            ValidateUserId(wallet.UserId);
            await _walletContext.Wallets.AddAsync(wallet, ct);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {

            ValidateUserId(userId);
            return await _walletContext.Wallets.AsNoTracking().AnyAsync(w => w.UserId == userId && w.Status == Core.Enums.WalletStatus.Active, ct);
        }

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {

            ValidateUserId(userId);
            return await _walletContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId && w.Status == Core.Enums.WalletStatus.Active, ct);

        }

        public async Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct = default)
        {

            ValidateUserId(userId);
            return await _walletContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.Status == Core.Enums.WalletStatus.Active, ct);

        }


        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        public async Task<Wallet?> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default)
        {
            if (walletId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(walletId));

            return await _walletContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w=> w.WalletId == walletId && w.Status == Core.Enums.WalletStatus.Active, ct);
        }

        public async Task<Wallet?> GetByWalletIdForUpdateAsync(Guid walletId, CancellationToken ct = default)
        {
            if (walletId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(walletId));

            return await _walletContext.Wallets.FirstOrDefaultAsync(w => w.WalletId == walletId && w.Status == Core.Enums.WalletStatus.Active, ct);

        }
    }
}
