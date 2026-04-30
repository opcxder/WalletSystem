

using System.ComponentModel.DataAnnotations;
using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Auth
{
    public  class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }


        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        [StringLength(10, ErrorMessage = "Phone number must be 10 digits")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public GovernmentIdType GovernmentIdType { get; set; }

        [Required]
        [StringLength(20)]
        public string GovernmentIdNumber { get; set; }
    }
}
