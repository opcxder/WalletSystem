

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface IUserKycRepository
    {
        Task AddAsync(UserKyc userKyc, CancellationToken ct = default);
    }
}
