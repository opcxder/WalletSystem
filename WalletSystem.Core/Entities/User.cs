
using System.ComponentModel.DataAnnotations;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
   
    public class User
    {
        public Guid UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
            
        [Required]
        [StringLength(15)] 
        public string PhoneNumber { get; set; }

        public UserStatus Status { get; set; } = UserStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } 

      

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationTokenHash { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }
    }
}