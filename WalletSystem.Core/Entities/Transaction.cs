using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
    public class Transaction
    {

        public Guid TransactionId { get;  set; }

        public SourceType SourceType { get; set; }

        public Guid? SourceWalletId { get; set; }

        public Wallet? SourceWallet { get; set; }

        public Guid? SourceBankAccountId { get; set; }

        [StringLength(100)]
        public string? ReferenceId { get; set; }

        public Guid? DestinationWalletId { get; set; }
        public Wallet? DestinationWallet { get; set; }

        public Guid? DestinationBankAccountId { get; set; }

        [Required]
        [StringLength(100)]
        public string DestinationVPA { get; set; }


        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Initiated;

        [Required]
        [StringLength(100)]
        public string IdempotencyKey { get; set; }


        public Guid? BankTransactionId { get; set; }


        [StringLength(250)]
        public string? FailureReason { get; set; }

        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }

      
        [Timestamp]
        public byte[] RowVersion { get; set; }

        public int RetryCount { get; set; }

        public DateTime? NextRetryAt { get; set; }

        public DateTime? LastRetryAt { get; set; }


        [StringLength(500)]
        public string? CompensationFailureReason { get; set; }



        public DateTime? CompletedAt { get; set; }

        public DateTime? BankCompletedAt { get; set; }

        public void MarkBankDebitSuccess( Guid bankTransactionId, DateTime bankCompletedAt)
        {
            Status = TransactionStatus.BankDebitSuccess;

            BankTransactionId = bankTransactionId;

            BankCompletedAt = bankCompletedAt;

            UpdatedAt = DateTime.UtcNow;
        }


        public void MarkWalletCreditSuccess()
        {
            Status = TransactionStatus.WalletCreditSuccess;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkSuccess()
        {
            Status = TransactionStatus.Success;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            FailureReason = null;
            CompensationFailureReason = null;
        }

        public void MarkCompensationPending(string reason)
        {
            Status = TransactionStatus.CompensationPending;

            RetryCount = 0;

            CompensationFailureReason = reason;

            NextRetryAt = DateTime.UtcNow.AddMinutes(5);

            UpdatedAt = DateTime.UtcNow;
        }


        public void MarkCompensated()
        {
            Status = TransactionStatus.Compensated;

            RetryCount = 0;

            NextRetryAt = null;

            LastRetryAt = null;

            CompensationFailureReason = null;

            CompletedAt = DateTime.UtcNow;

            UpdatedAt = DateTime.UtcNow;
            FailureReason = null;
        }
        public void MarkCompensationRetryFailed(string reason)
        {
            RetryCount++;

            CompensationFailureReason = reason;

            LastRetryAt = DateTime.UtcNow;

            UpdatedAt = DateTime.UtcNow;

            if (RetryCount >= 3)
            {
                Status = TransactionStatus.ManualReviewRequired;

                NextRetryAt = null;
            }
            else
            {
                Status = TransactionStatus.CompensationRetrying;

                NextRetryAt = DateTime.UtcNow.AddMinutes(
                    Math.Min(RetryCount * 5, 60));
            }
        }

        public void MarkFailed(string reason)
        {
            Status = TransactionStatus.Failed;

            FailureReason = reason;

            UpdatedAt = DateTime.UtcNow;

            CompletedAt = DateTime.UtcNow;
        }

    }
}