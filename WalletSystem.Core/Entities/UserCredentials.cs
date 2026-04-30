
using System.ComponentModel.DataAnnotations;


namespace WalletSystem.Core.Entities
{
    public class UserCredentials
    {
        public Guid CredentialId { get; set; }

        public User? User { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; }

        [StringLength(256)]
        public string? PaymentPinHash { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } 

        [Timestamp]
        public byte[] RowVersion { get; set; }

    }
}
