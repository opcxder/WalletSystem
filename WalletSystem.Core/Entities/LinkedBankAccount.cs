using System;
using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Core.Entities
{
    // TODO: remove unique constraint when enabling multiple accounts
    public class LinkedBankAccount
    {
        public Guid LinkedBankAccountId { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [StringLength(20)]
        public string IFSCCode { get; set; }

        [Required]
        public Guid ExternalBankAccountId { get; set; }

        [Required]
        [StringLength(4)]
        public string MaskedAccountNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string BankName { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } 

        public bool IsDeleted { get; set; } = false;
    }
}