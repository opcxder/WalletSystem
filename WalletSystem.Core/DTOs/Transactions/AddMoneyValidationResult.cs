

namespace WalletSystem.Core.DTOs.Transactions
{
    public sealed class AddMoneyValidationResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public AddMoneyContext? Context { get; set; }

        public TransactionResponse? IdempotentResponse { get; set; }
    }
}
