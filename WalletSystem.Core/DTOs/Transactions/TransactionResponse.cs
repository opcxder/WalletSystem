

using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Transactions
{
    public class TransactionResponse
    {
        public Guid TransactionId { get; set; }
        public string ReferenceId { get; set; } = null!;
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsIdempotentReplay { get; set; }


        public string? FailureReason { get; set; }

    }
}
