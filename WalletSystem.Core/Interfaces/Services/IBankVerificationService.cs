

using WalletSystem.Core.DTOs.Bank;

namespace WalletSystem.Core.Interfaces.Services
{
    public interface IBankVerificationService
    {
        Task<VerifyBankResponse> VerifyAccountAsync(VerifyBankRequest request);
        Task<BankOperationResponse> DebitAsync(Guid externalBankAccountId, decimal amount);

        Task<BankOperationResponse> CreditAsync(Guid externalBankAccountId, decimal amount);

        Task<CheckBalanceResponse> GetBalanceAsync(Guid externalBankAccountId);

        Task<LinkAccountResult> LinkAsync(string verificationToken);
    }
}
