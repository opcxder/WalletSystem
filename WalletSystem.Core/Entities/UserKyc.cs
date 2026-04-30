

using System.ComponentModel.DataAnnotations;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.Entities
{
    public class UserKyc
    {
        public Guid KycId { get; set; }

        public User? User { get; set; }

        public Guid UserId { get; set; }

        public GovernmentIdType GovernmentIdType { get; set; } = GovernmentIdType.NotSelected;

        [Required]
        [StringLength(20)]
        
        public string GovernmentIdNumber { get; set; } = string.Empty;

        public KycStatus Status { get; set; } = KycStatus.Pending;

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } 


    }
}
