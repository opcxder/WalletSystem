

namespace WalletSystem.Core.DTOs.Transactions
{
    public class SendMoneyValidationResult
    {
        public bool Success { get; set; }

        public string? Message {get ;set;}

        public SendMoneyContext? SendMoneyContext { get; set; }

        public TransactionResponse? TransactionResponse { get; set; }
    }
}
