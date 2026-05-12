

using WalletSystem.Core.Entities;

namespace WalletSystem.Core.DTOs.Transactions
{
    public sealed class AddMoneyContext
    {

        public User User { get; set; } = default!;

        public Wallet Wallet { get; set; } = default!;

        public LinkedBankAccount LinkedBankAccount { get; set; } = default!;

        public Vpa UserVpa { get; set; } = default!;

        public Transaction Transaction { get; set; } = default!;

        public Guid BankTransactionId { get; set; }
    }
}
