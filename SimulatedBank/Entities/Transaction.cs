

using SimulatedBank.Enums;

namespace SimulatedBank.Entities
{

    public class Transaction
    {
        public Guid TransactionId { get; private set; }

        public Guid BankAccountId { get; private set; }

        public decimal Amount { get; private set; }

        public TransactionType Type { get; private set; }

        public string Description { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private Transaction() { }

        public static Transaction CreateCredit(Guid accountId, decimal amount, string description)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                BankAccountId = accountId,
                Amount = amount,
                Type = TransactionType.Credit,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Transaction CreateDebit(Guid accountId, decimal amount, string description)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                BankAccountId = accountId,
                Amount = amount,
                Type = TransactionType.Debit,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}