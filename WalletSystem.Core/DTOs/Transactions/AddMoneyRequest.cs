

namespace WalletSystem.Core.DTOs.Transactions
{
    public class AddMoneyRequest
    {
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }

    }
}
