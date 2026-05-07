namespace WalletSystem.Core.DTOs.Bank
{
 public class BankOperationRequest
    {
        public Guid ExternalBankAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
