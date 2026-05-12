using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Core.DTOs.Wallets
{
    public class CreateWalletRequest
    {

        [Required]
        [RegularExpression(@"^\d{4,6}$")]
        public required string PaymentPin { get; set; }

    }
}
