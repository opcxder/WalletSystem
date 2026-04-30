

namespace WalletSystem.Core.DTOs.Auth
{
    public  class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool EmailSent { get; set; }

    
    }
}
