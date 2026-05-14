
using WalletSystem.Core.Entities;

namespace WalletSystem.Core.DTOs.Transactions
{
    public sealed class SendMoneyContext
    {
        public User SenderUser { get; set; } = default!;
        public Wallet SenderWallet { get; set; } = default!;
        public Wallet ReceiverWallet { get; set; } = default!;
        public Vpa ReceiverVpa { get; set; } = default!;

        public User ReceiverUser { get; set; } = default!;
        public Transaction Transaction { get; set; } = default!;
    }
}
