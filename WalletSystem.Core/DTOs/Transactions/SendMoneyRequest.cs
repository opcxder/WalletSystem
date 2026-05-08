

using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Transactions
{
    public class SendMoneyRequest
    {
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; }
        public string ReceiverVpa { get; set; }

    }
}
