

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface IUserCredentialsRepository
    {

        Task<UserCredentials?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task AddAsync(UserCredentials credentials , CancellationToken ct = default);
            
        Task UpdateAsync(UserCredentials credentials);
        Task<UserCredentials?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct = default);

    }
}
