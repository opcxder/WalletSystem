
using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Bank;

namespace WalletSystem.Core.Interfaces.Services
{
    public interface ILinkedBankAccountService
    {

        Task<ServiceResult<LinkedBankReponse>> VerifyAndLinkBankAccountAsync(Guid userId, VerifyBankRequest request, CancellationToken ct = default);

        Task<ServiceResult<LinkedBankAccountResponse>> GetLinkedAccountAsync(Guid userId, CancellationToken ct = default);

        Task<ServiceResult<CheckBalanceResponse>> GetBankBalanceAsync(Guid userId, CancellationToken ct = default );

    }
}
