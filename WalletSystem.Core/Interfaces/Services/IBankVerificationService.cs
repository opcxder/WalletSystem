

using WalletSystem.Core.DTOs.Bank;

namespace WalletSystem.Core.Interfaces.Services
{
    public interface IBankVerificationService
    {
        Task<VerifyBankResponse> VerifyAccountAsync(VerifyBankRequest request, CancellationToken ct = default);
        Task<BankOperationResponse> DebitAsync(Guid externalBankAccountId, decimal amount, Guid transactionId, CancellationToken ct = default);

        Task<BankOperationResponse> CreditAsync(Guid externalBankAccountId, decimal amount, Guid transactionId, CancellationToken ct = default);

        Task<CheckBalanceResponse> GetBalanceAsync(Guid externalBankAccountId, CancellationToken ct = default);

        Task<LinkAccountResult> LinkAsync(string verificationToken, CancellationToken ct = default);
    }
}
