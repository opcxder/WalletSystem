
using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Core.DTOs.Auth
{
    public  class LoginRequest
    {

        // At least one of Email or PhoneNumber must be provided
        // Validated in AuthService, not here
        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        [StringLength(10, ErrorMessage = "Phone number must be 10 digits")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

    }
}
