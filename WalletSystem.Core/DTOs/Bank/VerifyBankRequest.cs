

using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Bank
{
    public class VerifyBankRequest
    {
        public string AccountHolderName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public string? BankName { get; set; } = string.Empty;
        public AccountType AccountType { get; set; } 
    }
}
