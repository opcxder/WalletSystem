
using SimulatedBank.Enums;
using System.ComponentModel.DataAnnotations;

namespace SimulatedBank.Entities
{

    public class BankAccount
    {
        public Guid BankAccountId { get; private set; }

        public string AccountHolderName { get; private set; }

        public string AccountNumber { get; private set; }

        public AccountType AccountType { get; private set; }

        public decimal Balance { get; private set; }

        public bool IsActive { get; private set; }

        public Guid BankId { get; private set; }

        public string BankName { get; private set; }

        public Bank Bank { get; private set; }

        public List<Transaction> Transactions { get; private set; } = new();

        public DateTime CreatedAt { get; private set; }

        public Guid? ExternalBankAccountId { get; set; } 

        [Timestamp]
        public byte[] RowVersion { get; set; }

        private BankAccount() { }

        // Constructor with validation
        public BankAccount(string holderName, string accountNumber, Guid bankId, string bankName , AccountType accountType = AccountType.Savings )
        {
            if (string.IsNullOrWhiteSpace(holderName))
                throw new ArgumentException("Account holder name is required");

            if (string.IsNullOrWhiteSpace(bankName))
                throw new ArgumentException("Bank name is required");


            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length < 8)
                throw new ArgumentException("Invalid account number");

            BankAccountId = Guid.NewGuid();
            AccountHolderName = holderName;
            AccountNumber = accountNumber;
            BankId = bankId;
            Balance = 0;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            AccountType = accountType;
            BankName = bankName;
        }

        // Business Logic Methods
        public Transaction Credit(decimal amount, string description)
        {
            ValidateAmount(amount);

            Balance += amount;

            var transaction = Transaction.CreateCredit(BankAccountId, amount, description);
            Transactions.Add(transaction);
            return transaction;
        }

        public Transaction Debit(decimal amount, string description)
        {
            ValidateAmount(amount);

            if (Balance < amount)
                throw new InvalidOperationException("Insufficient balance");

            Balance -= amount;

            var transaction = Transaction.CreateDebit(BankAccountId, amount, description);
            Transactions.Add(transaction);
            return transaction;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");
        }
    }
}