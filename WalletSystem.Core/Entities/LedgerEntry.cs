

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
    public class LedgerEntry
    {
        public Guid EntryId { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        public Wallet? Wallet { get; set; }


        [Required]
        public Guid TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        [Required]
        public EntryType EntryType { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   


    }
}
