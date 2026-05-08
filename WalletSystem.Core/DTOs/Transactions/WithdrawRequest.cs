    
namespace WalletSystem.Core.DTOs.Transactions
{
    public class WithdrawRequest
    {
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }

    }
}
