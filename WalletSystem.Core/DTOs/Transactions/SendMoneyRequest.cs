

using System.ComponentModel.DataAnnotations;


namespace WalletSystem.Core.DTOs.Transactions
{
    public class SendMoneyRequest
    {
        [Required]
        [Range(0.01, 10000)]
        public decimal Amount { get; set; }

        [Required]
        public string IdempotencyKey { get; set; }

        [Required]
        public string ReceiverVpa { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 4)]
        public string PaymentPin { get; set; }


    }
}
