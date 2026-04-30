using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
    public class Wallet
    {     
        public Guid WalletId { get; set; }


        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; private set; } = 0m;

        
        public WalletStatus Status { get; set; } = WalletStatus.Active;

   
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

      


   
        [Timestamp]
        public byte[] RowVersion { get; set; }



        public void ApplyCredit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            Balance += amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ApplyDebit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (Balance < amount)
                throw new InvalidOperationException("Insufficient balance.");

            Balance -= amount;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}