
using System.ComponentModel.DataAnnotations;


namespace WalletSystem.Infrastructure.Config
{
    public class JwtSettings
    {
        [Required]
        public string PrivateKeyPath { get; set; }

        [Required]
        public string PublicKeyPath { get; set; }

        [Required]
        public string Issuer { get; set; }

        [Required]
        public string Audience { get; set; }

        [Range(1,1440)]
        public int ExpiryMinutes { get; set; }
    }
}
