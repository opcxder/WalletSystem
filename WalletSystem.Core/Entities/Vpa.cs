

using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Core.Entities
{
    public class Vpa
    {
        public Guid VpaId { get; set; }

        public Guid WalletId { get; set; }
        public Wallet? Wallet { get; set; }

        [Required]
        [StringLength(100)]
        public string VpaAddress { get; set; }

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
