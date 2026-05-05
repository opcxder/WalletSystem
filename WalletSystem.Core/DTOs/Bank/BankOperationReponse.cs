

namespace WalletSystem.Core.DTOs.Bank
{
    public class BankOperationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? TransactionId { get; set; }
    }
}
