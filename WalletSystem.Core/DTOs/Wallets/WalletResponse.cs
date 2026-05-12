

using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Wallets
{
    public class WalletResponse
    {
        public Guid WalletId { get; set; }

        public decimal Balance { get; set; }

        public WalletStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? VpaAddress { get; set; }
    }
}
