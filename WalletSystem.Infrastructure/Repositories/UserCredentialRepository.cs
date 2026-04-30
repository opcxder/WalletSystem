
using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{
    public class UserCredentialRepository : IUserCredentialsRepository
    {
        private readonly WalletContext _walletContext;

        public UserCredentialRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }

        public async Task AddAsync(UserCredentials credentials, CancellationToken ct = default)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            await _walletContext.UserCredentials.AddAsync(credentials, ct);
        }

        public async Task<UserCredentials?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid userId", nameof(userId));

            return await _walletContext.UserCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        }

        public Task UpdateAsync(UserCredentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            _walletContext.UserCredentials.Update(credentials);
            return Task.CompletedTask;
        }
    }
}
