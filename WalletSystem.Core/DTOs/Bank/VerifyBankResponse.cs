

namespace WalletSystem.Core.DTOs.Bank
{
    public class VerifyBankResponse
    {
        public bool IsValid { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string? HashedToken { get; set; }
        public string? Message { get; set; }
    }
}
