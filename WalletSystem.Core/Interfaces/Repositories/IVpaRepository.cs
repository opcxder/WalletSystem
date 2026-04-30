using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Repositories
{
    public  interface IVpaRepository
    {
        Task<Vpa?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Vpa?> GetByAddressAsync(string vpaAddress, CancellationToken ct = default);

        Task AddAsync(Vpa vpa, CancellationToken ct = default);

        Task<bool> ExistsAsync(string vpaAddress, CancellationToken ct = default);

    }
}
