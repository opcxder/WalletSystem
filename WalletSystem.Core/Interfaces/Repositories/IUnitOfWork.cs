

namespace WalletSystem.Core.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);


        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operations, CancellationToken ct =default);
    }
}
