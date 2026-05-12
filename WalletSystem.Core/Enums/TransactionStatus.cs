
namespace WalletSystem.Core.Enums
{
    public enum TransactionStatus
    {
        Initiated,
        BankDebitSuccess,
        WalletCreditSuccess,
        Success,
        Failed,
        CompensationPending,
        CompensationRetrying,
        Compensated,
        ManualReviewRequired

    }
}
