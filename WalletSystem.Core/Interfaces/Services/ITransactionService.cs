

using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Transactions;
using WalletSystem.Core.Entities;

namespace WalletSystem.Core.Interfaces.Services
{
    public interface ITransactionService
    {
        Task<ServiceResult<TransactionResponse>> AddMoneyAsync(Guid userId, AddMoneyRequest request, CancellationToken ct = default);
        Task<ServiceResult<TransactionResponse>> SendMoneyAsync(Guid userId, SendMoneyRequest request, CancellationToken ct = default);
        Task<ServiceResult<TransactionResponse>> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken ct = default);
        Task<ServiceResult<List<TransactionResponse>>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
