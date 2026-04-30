using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
    public class Transaction
    {

        public Guid TransactionId { get; set; }

        public SourceType SourceType { get; set; }

        public Guid? SourceWalletId { get; set; }

        public Wallet? SourceWallet { get; set; }

        public Guid? SourceBankAccountId { get; set; }


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

        [StringLength(100)]
        public string? ReferenceId { get; set; }


        [StringLength(250)]
        public string? FailureReason { get; set; }

        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }

      
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}