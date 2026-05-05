using SimulatedBank.Enums;

namespace SimulatedBank.Dtos
{
    public class VerifyAccountResponse
    {
        public bool IsValid { get; set; }

        public string AccountHolderName { get; set; } 
        public string MaskedAccountNumber { get; set; }

        public AccountType AccountType { get; set; }
        public string IFSCCode { get; set; }

        public string? Message { get; set; }

        public string? VerificationToken { get; set; }
    }
}
