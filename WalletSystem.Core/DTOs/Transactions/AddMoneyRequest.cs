

using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Core.DTOs.Transactions
{
    public class AddMoneyRequest
    {
        [Required]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between 0.01 and 10,000")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string IdempotencyKey { get; set; }

  
    }
}
