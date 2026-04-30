
using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id,  CancellationToken ct = default);
        Task<User?> GetActiveByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetActiveByPhoneAsync(string phone, CancellationToken ct = default);

       
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);



        Task AddAsync(User user, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);

        Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash , CancellationToken ct = default);

        Task<bool> ExistsActiveByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsActiveByPhoneAsync(string phone, CancellationToken ct = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);

    }
}