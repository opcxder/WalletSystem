

using WalletSystem.Core.Enums;

namespace WalletSystem.Core.DTOs.Bank
{
    public class LinkedBankReponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public Guid? ExternalReferenceId { get; set; }

        public string? MaskedAccountNumber { get; set; }
        public string? AccountHolderName { get; set; }

        public AccountType? AccountType { get; set; }
        public string? BankName { get; set; }

    }
}
